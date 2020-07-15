// ===============================
// @AUTHOR      : 윤정도
// @CREATE DATE : 2020-05-31 오후 1:27:08   
// @PURPOSE     : 패킷 프로세서
// ===============================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiteNetLib;
using NATShared;

namespace NATPuncher
{
    class NATPacketProcessor
    {
        NATPuncherApp _puncherApp;
        public NATPacketProcessor(NATPuncherApp puncherApp)
        {
            _puncherApp = puncherApp;
        }

        public void OnPacketEchoMessage(NetPeer peer, PtkEchoMessage ptkEchoMessage)
        {
            ptkEchoMessage.ID = -1;
            _puncherApp.SendPacketToPeer(peer, ptkEchoMessage);
        }

        public void OnPacketClientConnect(NetPeer peer, PtkClientConnect ptkClientConnect)
        {
            Console.WriteLine(peer.NetManager.LocalEndPointv4.ToString());
            if (_puncherApp.ConnectedPeers.TryGetValue(ptkClientConnect.ID, out NATClientInfo client))
            {
                _puncherApp.ConnectedPeers[ptkClientConnect.ID].InternalEndpoint = ptkClientConnect.InternalEP;
                _puncherApp.ConnectedPeers[ptkClientConnect.ID].Peer = peer;
            }
            else
            {
                _puncherApp.ConnectedPeers.Add(ptkClientConnect.ID, new NATClientInfo(ptkClientConnect.ID, ptkClientConnect.InternalEP, peer.EndPoint, peer));
            }
            NATClientInfo connectedClientInfo = _puncherApp.ConnectedPeers[ptkClientConnect.ID];


            //접속중인 유저들 중 접속한 유저만 제외하고 모두 에코 전송
            foreach (NATClientInfo c in _puncherApp.ConnectedPeers.Values)
            {
                if (c.Peer == connectedClientInfo.Peer) continue;
                _puncherApp.SendPacketToPeer(c.Peer, new PtkClientConnectAck(-1, connectedClientInfo));
            }

            //현재 접속중인 유저 정보들을 접속한 유저에게 전달
            foreach (KeyValuePair<long, NATClientInfo> c in _puncherApp.ConnectedPeers)
                _puncherApp.SendPacketToPeer(peer, new PtkClientConnectAck(-1, c.Value));
        }

        public void OnPacketClientDisconnect(NetPeer sender, PtkClientDisconnect ptkClientDisconnect)
        {
            ThreadSafeLogger.WriteLine(ptkClientDisconnect.ID + " 유저가 접속을 종료하였습니다.");

            if (_puncherApp.ConnectedPeers.TryGetValue(ptkClientDisconnect.ID, out NATClientInfo client))
                _puncherApp.ConnectedPeers.Remove(ptkClientDisconnect.ID);

            //남아있는 유저에게 전송
            foreach (NATClientInfo c in _puncherApp.ConnectedPeers.Values)
                _puncherApp.SendPacketToPeer(c.Peer, new PtkClientDisconnectAck(-1, ptkClientDisconnect.ID));
        }

        public void OnPacketNatTraversalRequest(NetPeer sender, PtkNatTraversalRequest ptkNatTraversalRequest)
        {
            if (_puncherApp.ConnectedPeers.TryGetValue(ptkNatTraversalRequest.RecipientID, out NATClientInfo recipient))
                _puncherApp.SendPacketToPeer(recipient.Peer, new PtkNatTraversalRequestAck(-1, ptkNatTraversalRequest.ID, ptkNatTraversalRequest.NatToken));
            else
                ThreadSafeLogger.WriteLine(ptkNatTraversalRequest.RecipientID + " 클라이언트가 접속중이지 않습니다");
        }

      
    }
}
