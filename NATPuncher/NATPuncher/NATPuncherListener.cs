// ===============================
// @AUTHOR      : 윤정도
// @CREATE DATE : 2020-05-30 오후 4:07:45   
// @PURPOSE     : 
// ===============================


using LiteNetLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using NATShared;

namespace NATPuncher
{
    class NATPuncherListener : INetEventListener, INatPunchListener, INATPuncherCustomizedListener
    {
        private readonly NATPuncherApp _puncherApp;

        public NATPuncherListener(NATPuncherApp puncherApp)
        {
            _puncherApp = puncherApp;
        }



        //<------------------------------------------------------------------------------------------------->
        //                                          < 넷 리스너 >
        //<------------------------------------------------------------------------------------------------->
        public void OnConnectionRequest(ConnectionRequest request)
        {
            ThreadSafeLogger.WriteLine("클라이언트로부터 접속 요청을 수신하였습니다. (요청 타입 : {0})", request.ToString());
            ThreadSafeLogger.WriteLine("연결 키값을 확인합니다.");

            string connectionKey = request.Data.GetString().Split('|')[0];
            if (connectionKey.Equals(NATPuncherConfiguration.ConnectionKey))
            {
                ThreadSafeLogger.WriteLine("키값 : {0}를 확인했습니다. 요청을 승인합니다.", NATPuncherConfiguration.ConnectionKey);
                request.Accept();
             
            }
            else
            {
                ThreadSafeLogger.WriteLine("키값 : {0}를 확인했습니다. 연결 키값이 틀립니다. (당신의 연결키값 : {1})", connectionKey, NATPuncherConfiguration.ConnectionKey);
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
                _puncherApp.CustomizedEventListener.OnProcessPacket(peer, receivedBytes.ToP2PBase());
            }
            catch (Exception e)
            {
                ThreadSafeLogger.WriteLine("< 패킷 프로세싱 중 오류 발생 >\n" + e.Message);
            }

        }

        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {
        }

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            ThreadSafeLogger.WriteLine("상대방이 당신의 서버를 떠났습니다");
            ThreadSafeLogger.WriteLine("상대방 정보 : IP({0})", peer.EndPoint);
        }

        void INetEventListener.OnPeerConnected(NetPeer peer)
        {
            ThreadSafeLogger.WriteLine("상대방이 당신의 서버에 접속하였습니다");
            ThreadSafeLogger.WriteLine("상대방 정보 : IP({0})", peer.EndPoint);
        }

        //<------------------------------------------------------------------------------------------------->
        //                                          < 펀쳐 서버 NAT Introduction 리스너 >
        //<------------------------------------------------------------------------------------------------->
        public void OnNatIntroductionRequest(IPEndPoint localEndPoint, IPEndPoint remoteEndPoint, string token)
        {
            WaitPeer wpeer;

            //주최측에서 일치하는 토큰을 찾아 연결을 해줌
            if (_puncherApp.WaitingPeers.TryGetValue(token, out wpeer))
            {
                if (wpeer.InternalAddr.Equals(localEndPoint) &&
                    wpeer.ExternalAddr.Equals(remoteEndPoint))
                {
                    wpeer.Refresh(); //매칭 시도한 시간을 업데이트 해줌
                    return;
                }

                Console.WriteLine("대기 중인 클라이언트를 찾았습니다. 클리이언트간 홀펀칭을 시작합니다.");

                
                Console.WriteLine(
                    "주최 클라이언트 - 내부 IP({0}) 외부 IP({1})\n접속 클라이언트 - 내부 IP({2}) 외부 IP({3})",
                    wpeer.InternalAddr,
                    wpeer.ExternalAddr,
                    localEndPoint,
                    remoteEndPoint);

                //서버에 먼저 접속하여 방을 만든 클라이언트와 접속시도한 클라이언트와 소개해줘서 연결시켜줌
                _puncherApp.Manager.NatPunchModule.NatIntroduce(
                    wpeer.InternalAddr, //호스트 클라 내부 IP
                    wpeer.ExternalAddr, //호스트 클라 외부 IP
                    localEndPoint, //접속 클라 내부 IP
                    remoteEndPoint, //접속 클라 외부 IP
                    token // 접속 토큰
                    );

                //매칭 잡아준 후 토큰 값에 해당하는 방을 제거해줌
                _puncherApp.WaitingPeers.Remove(token);
            }
            else
            {
                Console.WriteLine("클라이언트가 NAT Traversal을 진행하기 위해 {0}값의 토큰으로 본 서버에 대기방을 열었습니다. / 내부 IP({1}) 외부 IP({2})", token, localEndPoint, remoteEndPoint);
                _puncherApp.WaitingPeers[token] = new WaitPeer(localEndPoint, remoteEndPoint);
            }
        }

        public void OnNatIntroductionSuccess(IPEndPoint targetEndPoint, NatAddressType natAddressType, string token)
        {
            //클라측에서 사용하는 함수
        }

        //<------------------------------------------------------------------------------------------------->
        //                                          < 펀쳐 서버 커스텀 이벤트 리스너 >
        //<------------------------------------------------------------------------------------------------->

        public void OnServerOpened()
        {
            ThreadSafeLogger.WriteLine("NAT Puncher 시작");
        }

        public void OnServerClosed()
        {
            ThreadSafeLogger.WriteLine("NAT Puncher 중지");
        }

        public void OnProcessPacket(NetPeer sender, INetworkPacket networkPacket)
        {
            if (networkPacket.GetType() == typeof(PtkClientConnect))
                _puncherApp.PacketProcessor.OnPacketClientConnect(sender, (PtkClientConnect)networkPacket);
            else if (networkPacket.GetType() == typeof(PtkClientDisconnect))
                _puncherApp.PacketProcessor.OnPacketClientDisconnect(sender, (PtkClientDisconnect)networkPacket);
            else if (networkPacket.GetType() == typeof(PtkEchoMessage))
                _puncherApp.PacketProcessor.OnPacketEchoMessage(sender, (PtkEchoMessage)networkPacket);
            else if (networkPacket.GetType() == typeof(PtkNatTraversalRequest))
                _puncherApp.PacketProcessor.OnPacketNatTraversalRequest(sender, (PtkNatTraversalRequest)networkPacket);
        }
    }
}
