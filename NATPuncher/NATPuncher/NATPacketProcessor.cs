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

        // ===============================

        public void OnPacketEchoMessage(NetPeer peer, PtkEchoMessage ptkEchoMessage)
        {
            ptkEchoMessage.ID = -1;
            _puncherApp.SendPacketToPeer(peer, ptkEchoMessage);
        }

        public void OnPacketClientConnect(NetPeer peer, PtkClientConnect ptkClientConnect)
        {
            if (peer.Tag == null)
                peer.Tag = new NATClientInfo(ptkClientConnect.ID, peer);
            else
                _puncherApp.Manager.GetPeerInfoByNetPeerID(peer.Id).Update(new NATClientInfo(ptkClientConnect.ID, peer));

            NATClientInfo clinetInfo = peer.Tag as NATClientInfo;
            clinetInfo.Peer = peer;

            //접속중인 유저들 중 접속한 유저만 제외하고 모두 에코 전송
            _puncherApp.Manager
                .Except(new NetPeer[1] { peer })
                .Where(x => x.Tag != null)
                .ForEach( x => _puncherApp.SendPacketToPeer(x, new PtkClientConnectAck(-1, clinetInfo)));

            //현재 접속중인 유저 정보들을 접속한 유저에게 전달
            _puncherApp.Manager
                .Where(x => x.Tag != null)
                .ForEach(x => _puncherApp.SendPacketToPeer(peer, new PtkClientConnectAck(-1, _puncherApp.Manager.GetPeerInfoByNetPeerID(x.Id))));
        }

        public void OnPacketClientDisconnect(NetPeer sender, PtkClientDisconnect ptkClientDisconnect)
        {
            ThreadSafeLogger.WriteLine(ptkClientDisconnect.ID + " 유저가 접속을 종료하였습니다.");

            //남아있는 유저들에게 모두 전송
            _puncherApp.Manager
                .Except(new NetPeer[1] { sender })
                .Where(x => x.Tag != null)
                .ForEach(x => _puncherApp.SendPacketToPeer(x, new PtkClientDisconnectAck(-1, ptkClientDisconnect.ID)));
        }

        public void OnPacketNatTraversalRequest(NetPeer sender, PtkNatTraversalRequest ptkNatTraversalRequest)
        {
            NATClientInfo recipientInfo = _puncherApp.Manager.GetPeerInfoByTickID(ptkNatTraversalRequest.RecipientID);


            //연결하고자하는 대상이 없을 경우
            if (recipientInfo == null)
                _puncherApp.SendPacketToPeer(sender, new PtkEchoMessage(-1, ptkNatTraversalRequest.RecipientID + "와 NAT 연결에 실패했습니다."));
            else
            {
                NetPeer recipientPeer = _puncherApp.Manager.GetPeerByTickID(recipientInfo.ID);
                _puncherApp.SendPacketToPeer(recipientPeer, new PtkNatTraversalRequestAck(-1, ptkNatTraversalRequest.ID, ptkNatTraversalRequest.NatToken));
            }
        }

        internal void OnPacketRequestP2PClientInfo(NetPeer sender, PtkRequestP2PClientInfo ptkRequestP2PClientInfo)
        {
            ThreadSafeLogger.WriteLine(ptkRequestP2PClientInfo.RequesterID + " 유저가 " + ptkRequestP2PClientInfo.ConnectedUserID + "의 정보를 요청하였습니다.");
            NATClientInfo connectedUserInfo = _puncherApp.Manager.GetPeerInfoByTickID(ptkRequestP2PClientInfo.ConnectedUserID);
            NetPeer requesterPeer = _puncherApp.Manager.GetPeerByTickID(ptkRequestP2PClientInfo.RequesterID);

            if (requesterPeer == null)
            {
                //요청 유저가 중앙 서버에 접속이 안되있는 경우
            }
            else if (connectedUserInfo == null)
            {
                //요청받은 유저가 중앙 서버에 접속이 안되있는 경우
            }
            else
            {
                ThreadSafeLogger.WriteLine(ptkRequestP2PClientInfo.RequesterID + " 유저에게 " + ptkRequestP2PClientInfo.ConnectedUserID + "의 정보를 전송하였습니다");
                _puncherApp.SendPacketToPeer(requesterPeer, new PtkRequestP2PClientInfoAck(-1, connectedUserInfo, ptkRequestP2PClientInfo.Key));
            }
        }
    }
}
