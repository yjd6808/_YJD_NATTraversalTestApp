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
            ptkClientConnectAck.ConnectedClient.Peer = sender;

            Console.WriteLine(ptkClientConnectAck.ConnectedClient.ToString());
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

            if (sender == _clientApp.ServerPeer)
            {
                Console.WriteLine("서버 피어다");
            }
            else
            {
                NATClientInfo info = _clientApp.ServerConnectedClients.Values.FirstOrDefault(x => x.Peer == sender);
                if (info != null)
                {
                    Console.WriteLine(info.ToString() + "의 피어다");
                }
                else
                {
                    Console.WriteLine("누구의 피어도 아니다");
                }
            }
        }

        public void OnClientNatTraversalRequestAck(NetPeer sender, PtkNatTraversalRequestAck ptkNatTraversalRequestAck)
        {
            ThreadSafeLogger.WriteLine(ptkNatTraversalRequestAck.CallerID + "가 당신과 연결을 요청했습니다." + "토큰 설정완료 : " + ptkNatTraversalRequestAck.NatToken);
            _clientApp.SendNatIntroductionRequest(ptkNatTraversalRequestAck.NatToken);
            _clientApp.ServerConnectedClients[ptkNatTraversalRequestAck.CallerID].NatToken = ptkNatTraversalRequestAck.NatToken;
            ThreadSafeLogger.WriteLine(_clientApp.ServerConnectedClients[ptkNatTraversalRequestAck.CallerID].InternalEndpoint + "의 토큰을 설정했습니다");
        }

        public void OnClientChatMessage(NetPeer sender, PtkChatMessage ptkChatMessage)
        {
            ThreadSafeLogger.WriteLine(ptkChatMessage + " 으로부터 메시지 수신 : " + ptkChatMessage.Message);
        }


    }
}
