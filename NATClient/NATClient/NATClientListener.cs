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
            try
            {
                

                _clientApp.CustomizedEventListener.OnProcessPacket(peer, receivedBytes.ToP2PBase());
            }
            catch (Exception e)
            {
                ThreadSafeLogger.WriteLine("< 패킷 프로세싱 중 오류 발생 >\n" + e.Message);
            }
        }

        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {
        }
        public void OnPeerConnected(NetPeer peer)
        {
            if (peer == _clientApp.ServerPeer)
            {
                ThreadSafeLogger.WriteLine("서버와 연결되었습니다.");
                ThreadSafeLogger.WriteLine("상대방 정보 : IP({0}) / 연결상태 : {1}", peer.EndPoint, peer.ConnectionState);
                return;
            }
            else
            {
                ThreadSafeLogger.WriteLine("상대방이 당신의 서버에 접속하였습니다");
                ThreadSafeLogger.WriteLine("상대방 정보 : IP({0}) / 연결상태 : {1}", peer.EndPoint, peer.ConnectionState);

                NATClientInfo clientInfo = _clientApp.ServerConnectedClients.Values.FirstOrDefault(x => (x.P2PPeer != null && x.P2PPeer.EndPoint.Equals(peer.EndPoint)) || x.Peer.EndPoint.Equals(peer.EndPoint));

                ThreadSafeLogger.WriteLine("-------------- P2P 정보");
                foreach (var f in _clientApp.ServerConnectedClients.Values)
                    ThreadSafeLogger.WriteLine(f.ToString() + " / " +  (f.P2PPeer == null ? "p2p Peer = null" : (" / p2p PeerEP : " + f.P2PPeer.EndPoint + " / p2p LocalEP : " + f.P2PPeer.NetManager.LocalEndPointv4)));

                ThreadSafeLogger.WriteLine("-------------- 일반 정보");
                foreach (var f in _clientApp.ServerConnectedClients.Values)
                    ThreadSafeLogger.WriteLine(f.ToString() + " / EP : " + f.Peer.EndPoint + " / LocalEP : " + f.Peer.NetManager.LocalEndPointv4);

                if (clientInfo == null)
                {
                    ThreadSafeLogger.WriteLine(peer.EndPoint + "가 존재하지 않음");
                    return;
                }


                if (_clientApp.P2PConnectedClients.TryGetValue(clientInfo.ID, out NATTraversalPeer natTraversalClientInfo) == false)
                {
                    _clientApp.P2PConnectedClients.Add(clientInfo.ID, new NATTraversalPeer(clientInfo.NatToken, clientInfo));
                    _clientApp.P2PConnectedClients[clientInfo.ID].ClientInfo.P2PPeer = peer;
                }
                else
                {
                    _clientApp.P2PConnectedClients[clientInfo.ID].ClientInfo.P2PPeer = peer;
                }
                _clientApp.P2PConnectedClients[clientInfo.ID].ClientInfo.P2PConnected = true;

                ThreadSafeLogger.WriteLine("P2P 연결 성공!");
            }

            ThreadSafeLogger.WriteLine("접속한 P2P 클라의 연결된 클라수 : {0}", peer.NetManager.ConnectedPeersCount);

            if (peer.ConnectionState == ConnectionState.Connected)
            {
                foreach (NetPeer conn in peer.NetManager)
                {
                    ThreadSafeLogger.WriteLine("접속한 P2P 클라의 연결된 IP : {0}", peer.EndPoint);
                }
            }
        }

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            if (peer == _clientApp.ServerPeer)
                ThreadSafeLogger.WriteLine("당신과 연결된 유저(" + peer.EndPoint + ")와 연결이 끊어졌습니다. / 이유 : " + disconnectInfo.Reason);
            NATTraversalPeer clientInfo = _clientApp.P2PConnectedClients.Values.FirstOrDefault(x => x.ClientInfo.P2PPeer != null && x.ClientInfo.P2PPeer.EndPoint.Equals(peer.EndPoint));

            if (clientInfo == null)
            {
                ThreadSafeLogger.WriteLine(peer.EndPoint + "가 존재하지 않음");
                return;
            }


            if (_clientApp.P2PConnectedClients.TryGetValue(clientInfo.ClientInfo.ID, out NATTraversalPeer natTraversalClientInfo))
            {
                _clientApp.P2PConnectedClients.Remove(clientInfo.ClientInfo.ID);
                ThreadSafeLogger.WriteLine("당신과 연결된 유저(" + peer.EndPoint + ")와 P2P 연결이 끊어졌습니다. / 이유 : " + disconnectInfo.Reason);
            }

            //접속한 p2p 유저의 ID정보를 가져옴
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

            NATClientInfo clientInfo = _clientApp.ServerConnectedClients.Values.FirstOrDefault(x => x.NatToken == token);

            if (clientInfo == null)
            {
                ThreadSafeLogger.WriteLine(p2pPeer.EndPoint + " 에 대한 토큰값이 존재하지 않습니다. 연결을 거부합니다.");
                p2pPeer.Disconnect();
                return;
            }
            else
                ThreadSafeLogger.WriteLine(p2pPeer.EndPoint + " 과 토큰이 일치합니다.");

            //Console.WriteLine("Id : " + p2pPeer.Id + " remote ep : " + p2pPeer.EndPoint + "local ep : " + p2pPeer.NetManager.LocalEndPointv4.ToString());
            //foreach (var f in _clientApp.ServerConnectedClients)
            //Console.WriteLine(f.Value.InternalEndpoint + " / " + f.Value.ExternalEndpoint);

            //Console.WriteLine("==");
            //foreach (var f in _clientApp.ServerConnectedClients)
            //    Console.WriteLine("Id : " + f.Value.Peer.Id + "remote ep : " + f.Value.Peer.EndPoint + "local ep : " + f.Value.Peer.NetManager.LocalEndPointv4.ToString());



            //if (clientInfo.Value == null) 
            //{
            //    ThreadSafeLogger.WriteLine(p2pPeer.EndPoint + "는 중앙서버와 연결중이지 않습니다. P2P 연결에 실패합니다");
            //    return;
            //}

            clientInfo.P2PPeer = p2pPeer;
            clientInfo.P2PPeer.Tag = token;

            ThreadSafeLogger.WriteLine(p2pPeer.EndPoint + "의 토큰 태그 설정완료 " + token);

            if (_clientApp.P2PConnectedClients.TryGetValue(clientInfo.ID, out NATTraversalPeer natTraversalClientInfo) == false)
                _clientApp.P2PConnectedClients.Add(clientInfo.ID, new NATTraversalPeer(token, clientInfo));
            else
                _clientApp.P2PConnectedClients[clientInfo.ID] = new NATTraversalPeer(token, clientInfo);

            ThreadSafeLogger.WriteLine("축하드립니다!\nUdp Nat Traversal에 성공했습니다.\n상대 클라이언트와 연결되었습니다. (상태 : {0}) (NAT 주소 타입 : {1})", p2pPeer.ConnectionState.ToString(), natAddressType);
            ThreadSafeLogger.WriteLine("NAT 접속한 P2P 클라의 연결된 클라수 : {0}", p2pPeer.NetManager.ConnectedPeersCount);
            foreach (NetPeer conn in p2pPeer.NetManager)
            {
                ThreadSafeLogger.WriteLine("NAT 접속한 P2P 클라의 연결된 IP : {0} / 상태 : {1}", conn.EndPoint, conn.ConnectionState);
            }
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

            
        }
    }
}
