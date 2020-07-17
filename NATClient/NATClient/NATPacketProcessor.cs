// ===============================
// @AUTHOR      : 윤정도
// @CREATE DATE : 2020-05-31 오후 2:17:49   
// @PURPOSE     : 패킷 프로세서
// ===============================


using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LiteNetLib;
using NATShared;

namespace NATClient
{
    class NATPacketProcessor
    {
        NATClientApp _clientApp;
        public NATPacketProcessor(NATClientApp clientApp)
        {
            _clientApp = clientApp;
        }

        public void OnClientConnectAck(NetPeer sender, PtkClientConnectAck ptkClientConnectAck)
        {
            if (_clientApp.ServerConnectedClients.TryGetValue(ptkClientConnectAck.ConnectedClient.ID, out NATClientInfo clientInfo) == false)
                _clientApp.ServerConnectedClients.Add(ptkClientConnectAck.ConnectedClient.ID, ptkClientConnectAck.ConnectedClient);
            else
                _clientApp.ServerConnectedClients[ptkClientConnectAck.ConnectedClient.ID] = ptkClientConnectAck.ConnectedClient;

            ThreadSafeLogger.WriteLine("신규 유저가 중앙서버에 접속하였습니다\n접속목록을 갱신합니다. 접속자 ID ({0})", ptkClientConnectAck.ConnectedClient.ID);
        }

        public void OnClientDisconnectAck(NetPeer sender, PtkClientDisconnectAck ptkClientDisconnectAck)
        {
            ThreadSafeLogger.WriteLine(ptkClientDisconnectAck.DisconnectClientID + " 유저가 중앙서버와 연결을 끊었습니다");

            if (_clientApp.ServerConnectedClients.TryGetValue(ptkClientDisconnectAck.DisconnectClientID, out NATClientInfo clientInfo))
                _clientApp.ServerConnectedClients.Remove(ptkClientDisconnectAck.DisconnectClientID);

            //만약 P2P연결 대기중인 유저가 끊긴 경우에도 제거 해줘야함
            KeyValuePair<string, NetPeer> p2pWaitingPeer = _clientApp.P2PConnectingWaitingPeers.FirstOrDefault(x => x.Value == sender);
            if (p2pWaitingPeer.Value != null)
                _clientApp.P2PConnectingWaitingPeers.Remove(p2pWaitingPeer.Key);
        }

        public void OnClientServerShutdown(NetPeer sender, PtkServerShutdown ptkServerShutdown)
        {
            ThreadSafeLogger.WriteLine("=====================================================");
            ThreadSafeLogger.WriteLine("중앙서버가 강제로 닫혔습니다. 모든 연결이 끊어집니다.");
            ThreadSafeLogger.WriteLine("=====================================================");

            _clientApp.Stop(true);
        }

        public void OnClientEchoMessage(NetPeer sender, PtkEchoMessage ptkEchoMessage)
        {
            ThreadSafeLogger.WriteLine("에코 메시지 수신 : " + ptkEchoMessage.Message);
        }

        public void OnClientNatTraversalRequestAck(NetPeer sender, PtkNatTraversalRequestAck ptkNatTraversalRequestAck)
        {
            ThreadSafeLogger.WriteLine(ptkNatTraversalRequestAck.CallerID + "가 당신과 연결을 요청했습니다." + " 토큰 설정완료 : " + ptkNatTraversalRequestAck.NatToken);
            _clientApp.SendNatIntroductionRequest(ptkNatTraversalRequestAck.NatToken);
            _clientApp.ServerConnectedClients[ptkNatTraversalRequestAck.CallerID].P2PNatToken = ptkNatTraversalRequestAck.NatToken;
            ThreadSafeLogger.WriteLine(_clientApp.ServerConnectedClients[ptkNatTraversalRequestAck.CallerID].ID + "의 토큰을 설정했습니다");
        }

        public void OnClientChatMessage(NetPeer sender, PtkChatMessage ptkChatMessage)
        {
            ThreadSafeLogger.WriteLine(ptkChatMessage + " 으로부터 메시지 수신 : " + ptkChatMessage.Message);
        }

        public void OnClientRequestP2PClientInfo(NetPeer sender, PtkRequestP2PClientInfo ptkRequestP2PClientInfo)
        {
            _clientApp.SendPacketToMasterServer(new PtkRequestP2PClientInfo(_clientApp.ID, ptkRequestP2PClientInfo.ID, _clientApp.ID, ptkRequestP2PClientInfo.Key));
        }

        public void OnClientRequestP2PClientInfoAck(NetPeer sender, PtkRequestP2PClientInfoAck ptkRequestP2PClientInfoAck)
        {
            if (_clientApp.P2PConnectingWaitingPeers.TryGetValue(ptkRequestP2PClientInfoAck.Key, out NetPeer p2pConnectingWaitingPeer))
            {
                ptkRequestP2PClientInfoAck.ClientInfo.P2PConnected = true;
                ptkRequestP2PClientInfoAck.ClientInfo.Peer = p2pConnectingWaitingPeer;
                p2pConnectingWaitingPeer.Tag = ptkRequestP2PClientInfoAck.ClientInfo;
                ThreadSafeLogger.WriteLine(p2pConnectingWaitingPeer.EndPoint + "(" + ptkRequestP2PClientInfoAck.ClientInfo.ID + ")" + "와 P2P 연결 성공! / 연결상태 : {0}", sender.ConnectionState);
            }
            else
            {
                //대기 중인 유저를 못찾았을 경우
            }
        }


        public void OnClientReliableTestStart(NetPeer sender, PtkReliableTestStart networkPacket)
        {
            Console.Clear();
            Console.CursorVisible = false;
            _clientApp.IsTestCase8Running = true;

            ThreadSafeLogger.WriteLine(networkPacket.ID + "가 당신에게 패킷 통신 2천회 테스트 요청을 보냈습니다.\n이 작업을 하는동안 다른 작업은 하실 수 없습니다.");
            _clientApp.SendPacketToPeer(sender, new PtkReliableTestStartAck(_clientApp.ID));
        }

        public void OnClientReliableTestStartAck(NetPeer sender, PtkReliableTestStartAck networkPacket)
        {
            _clientApp.SendPacketToPeer(sender, new PtkReliableTest(_clientApp.ID));
        }

        public void OnClientReliableTest(NetPeer sender, PtkReliableTest networkPacket)
        {
            _clientApp.TestPacketCount += 1;
            Console.CursorTop = 2;
            ThreadSafeLogger.WriteLine("{0}회 수신완료", _clientApp.TestPacketCount);
            if (_clientApp.TestPacketCount >= _clientApp.MaxTestPacketCount)
            {
                _clientApp.SendPacketToPeer(sender, new PtkReliableTestAck(_clientApp.ID, true));

                Console.WriteLine("\n테스트 송신완료. --- 메뉴를 선택하시고 싶으시면 아무키나 입력해주세요");
                Console.ReadLine();

                Console.CursorVisible = true;
                _clientApp.TestPacketCount = 0;
                _clientApp.IsTestCase8Running = false;
            }
            else
                _clientApp.SendPacketToPeer(sender, new PtkReliableTestAck(_clientApp.ID, false));
        }

        public void OnClientReliableTestAck(NetPeer sender, PtkReliableTestAck networkPacket)
        {
            _clientApp.TestPacketCount += 1;
            Console.CursorTop = 2;
            ThreadSafeLogger.WriteLine("통신 {0}회 완료", _clientApp.TestPacketCount);
            if (networkPacket.Over)
            {
                if (_clientApp.TestPacketCount == _clientApp.MaxTestPacketCount)
                    ThreadSafeLogger.WriteLine("패킷 소실 없음 / 완벽한 UDP Reliable 통신입니다.");
                else
                    ThreadSafeLogger.WriteLine("{0} 회 패킷 소실 발생 / 실패한 UDP Reliable 통신입니다.", _clientApp.MaxTestPacketCount - _clientApp.TestPacketCount);

                Console.WriteLine("메뉴를 선택하시고 싶으시면 아무키나 입력해주세요");
                Console.ReadLine();
                Console.CursorVisible = true;
                _clientApp.IsTestCase8Running = false;
                _clientApp.TestPacketCount = 0;
            }
            else
                _clientApp.SendPacketToPeer(sender, new PtkReliableTest(_clientApp.ID));
        }
    }
}
