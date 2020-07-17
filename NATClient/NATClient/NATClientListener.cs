// ===============================
// @AUTHOR      : 윤정도
// @CREATE DATE : 2020-05-30 오후 10:04:36   
// @PURPOSE     : 클라이언트 리스너
// ===============================


using LiteNetLib;
using NATShared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NATClient
{
    class NATClientListener : INetEventListener, INatPunchListener, INATClientCustomizedListener
    {
        NATClientApp _clientApp;

        public NATClientListener(NATClientApp clientApp)
        {
            _clientApp = clientApp;
        }

        //<------------------------------------------------------------------------------------------------->
        //                                          < 넷 리스너 >
        //<------------------------------------------------------------------------------------------------->

        public void OnConnectionRequest(ConnectionRequest request)
        {
            ThreadSafeLogger.WriteLine("클라이언트로부터 접속 요청을 수신하였습니다. (요청 타입 : {0})", request.ToString());
            string connectionKey = request.Data.GetString();
            if (connectionKey.Equals(NATClientConfiguration.ConnectionKey))
            {
                ThreadSafeLogger.WriteLine("키값 : {0}를 확인했습니다. 요청을 승인합니다.", NATClientConfiguration.ConnectionKey);
                request.Accept();
            }
            else
            {
                ThreadSafeLogger.WriteLine("키값 : {0}를 확인했습니다. 연결 키값이 틀립니다. (당신의 연결키값 : {1})", connectionKey, NATClientConfiguration.ConnectionKey);
                ThreadSafeLogger.WriteLine("연결 요청을 거부합니다.");
                request.Reject();
            }
        }

        public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
        {
        }

        public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {
        }

        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            byte[] receivedBytes = new byte[4096];
            reader.GetBytes(receivedBytes, reader.AvailableBytes);
            //try
            //{
                _clientApp.CustomizedEventListener.OnProcessPacket(peer, receivedBytes.ToP2PBase());
            //}
            //catch (Exception e)
            //{
              //  ThreadSafeLogger.WriteLine("< 패킷 프로세싱 중 오류 발생 >\n" + e.Message);
            //}
        }

        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {
        }
        public void OnPeerConnected(NetPeer peer)
        {
            if (peer == _clientApp.ServerPeer)
            {
                ThreadSafeLogger.WriteLine("서버와 연결되었습니다.");
                ThreadSafeLogger.WriteLine("서버 정보 : IP({0}) / 연결상태 : {1}", peer.EndPoint, peer.ConnectionState);
            }
            else
            {
                //TODO : P2P로 들어오게되면 클라 태그정보가 지워지므로 누군지 물어봐야한다.
                //나 -> P2P 피어 : RequsetClientInfo(나의 ID) - 넌 누구냐?
                //P2P 피어 -> 나 : RequsetClientInfoAck(나의 ID) - 난 이런 사람이다. ( 자기 자신의 정보는 자기가 들고 있으므로 )

                //위의 방법은 안됨 P2P도 똑같이 정보가 지워졌기 때문에 서버에 물어봐야함
                //나 -> P2P피어 -> 서버 -> 나 순으로 이뤄줘야함

                //나 -> P2P : RequsetClientInfo(나의 ID)
                //P2P -> 서버 : RequsetClientInfo(P2P ID, 나의 ID)
                //서버 -> 나: RequsetClientInfoAck(-1, P2P의 클라 정보)

                string key = Program.RandomString(30);
                _clientApp.SendPacketToPeer(peer, new PtkRequestP2PClientInfo(_clientApp.ID, _clientApp.ID, key));
                _clientApp.P2PConnectingWaitingPeers.Add(key, peer);
                ThreadSafeLogger.WriteLine(peer.EndPoint + "가 접속하였습니다. 누군지 접속한 상대에게 정보를 요청하였습니다.\n상대는 서버측에 물어봅니다.\n그리고 중앙서버가 누군지 파악하여 회신해 줄 것입니다.");
            }
        }

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            //서버와 연결끊어지면 걍 노출력
            if (peer == _clientApp.ServerPeer)
                return;

            NATClientInfo clientInfo = peer.Tag as NATClientInfo;
            if (clientInfo == null)
                ThreadSafeLogger.WriteLine("당신과 연결되었던 " + peer.EndPoint + "와 연결이 끊어졌습니다. \n해당 유저의 데이터가 없습니다  / 이유 : " + disconnectInfo.Reason);
            else
                ThreadSafeLogger.WriteLine("당신과 연결된 유저(" + peer.EndPoint + ")와 P2P 연결이 끊어졌습니다. / 이유 : " + disconnectInfo.Reason);
        }

        //<------------------------------------------------------------------------------------------------->
        //                                          < 클라 NAT Introduction 리스너 >
        //<------------------------------------------------------------------------------------------------->

        public void OnNatIntroductionRequest(IPEndPoint localEndPoint, IPEndPoint remoteEndPoint, string token)
        {
            //펀쳐 서버에서 사용함
        }

        public void OnNatIntroductionSuccess(IPEndPoint targetEndPoint, NatAddressType natAddressType, string token)
        {
            NetPeer p2pPeer = _clientApp.Manager.Connect(targetEndPoint, NATClientConfiguration.ConnectionKey);
            ThreadSafeLogger.WriteLine("\n축하드립니다!\nUdp Nat Traversal에 성공했습니다.\n상대 클라이언트와 연결되었습니다. (상태 : {0}) (NAT 주소 타입 : {1})", p2pPeer.ConnectionState.ToString(), natAddressType);
        }

        //<------------------------------------------------------------------------------------------------->
        //                                          < 클라 자체제작 리스너 >
        //<------------------------------------------------------------------------------------------------->

        public void OnClientNetworkConnected()
        {
            ThreadSafeLogger.WriteLine("NAT Puncher 서버로 접속에 성공했습니다!");
            _clientApp.SendPacketToMasterServer(new PtkClientConnect(_clientApp.ID, _clientApp.Manager.LocalEndPointv4));
        }

        public void OnClientNetworkClosed()
        {
            ThreadSafeLogger.WriteLine("NAT Puncher 서버와 접속이 끊어졌습니다.");
        }

        public void OnProcessPacket(NetPeer sender,  INetworkPacket networkPacket)
        {
            if (networkPacket.GetType() == typeof(PtkClientConnectAck))
                _clientApp.PacketProcessor.OnClientConnectAck(sender, (PtkClientConnectAck)networkPacket);
            else if (networkPacket.GetType() == typeof(PtkClientDisconnectAck))
                _clientApp.PacketProcessor.OnClientDisconnectAck(sender, (PtkClientDisconnectAck)networkPacket);
            else if (networkPacket.GetType() == typeof(PtkServerShutdown))
                _clientApp.PacketProcessor.OnClientServerShutdown(sender, (PtkServerShutdown)networkPacket);
            else if (networkPacket.GetType() == typeof(PtkEchoMessage))
                _clientApp.PacketProcessor.OnClientEchoMessage(sender, (PtkEchoMessage)networkPacket);
            else if (networkPacket.GetType() == typeof(PtkNatTraversalRequestAck))
                _clientApp.PacketProcessor.OnClientNatTraversalRequestAck(sender, (PtkNatTraversalRequestAck)networkPacket);
            else if (networkPacket.GetType() == typeof(PtkChatMessage))
                _clientApp.PacketProcessor.OnClientChatMessage(sender, (PtkChatMessage)networkPacket);
            else if (networkPacket.GetType() == typeof(PtkRequestP2PClientInfo))
                _clientApp.PacketProcessor.OnClientRequestP2PClientInfo(sender, (PtkRequestP2PClientInfo)networkPacket);
            else if (networkPacket.GetType() == typeof(PtkRequestP2PClientInfoAck))
                _clientApp.PacketProcessor.OnClientRequestP2PClientInfoAck(sender, (PtkRequestP2PClientInfoAck)networkPacket);

            else if (networkPacket.GetType() == typeof(PtkReliableTestStart))
                _clientApp.PacketProcessor.OnClientReliableTestStart(sender, (PtkReliableTestStart)networkPacket);
            else if (networkPacket.GetType() == typeof(PtkReliableTestStartAck))
                _clientApp.PacketProcessor.OnClientReliableTestStartAck(sender, (PtkReliableTestStartAck)networkPacket);
            else if (networkPacket.GetType() == typeof(PtkReliableTest))
                _clientApp.PacketProcessor.OnClientReliableTest(sender, (PtkReliableTest)networkPacket);
            else if (networkPacket.GetType() == typeof(PtkReliableTestAck))
                _clientApp.PacketProcessor.OnClientReliableTestAck(sender, (PtkReliableTestAck)networkPacket);
            
        }
    }
}
