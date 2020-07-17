// ===============================
// @AUTHOR      : 윤정도
// @CREATE DATE : 2020-05-31 오전 12:06:38   
// @PURPOSE     : 패킷모음
// ===============================

using LiteNetLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace NATShared
{
    [Serializable]
    public class NATClientInfo
    {
        public long ID;
        public bool P2PConnected;
        public List<long> P2PConnectedPeers;
        public string P2PNatToken;

        [NonSerialized]
        public NetPeer Peer;      //피어 자신

        public NATClientInfo(long id, NetPeer peer)
        {
            this.ID = id;
            this.P2PConnected = false;
            this.P2PConnectedPeers = new List<long>();
            this.P2PNatToken = string.Empty;
            this.Peer = peer;
        }

        public void Update(NATClientInfo newClientInfo)
        {
            this.ID = newClientInfo.ID;
            this.P2PConnected = newClientInfo.P2PConnected;
            this.P2PConnectedPeers = newClientInfo.P2PConnectedPeers;
            this.P2PNatToken = newClientInfo.P2PNatToken;
            this.Peer = newClientInfo.Peer;
        }

        public override string ToString()
        {
            return string.Format("ID ({0}) / P2PConnected ({1})", ID, P2PConnected);
        }
    }

    [Serializable]
    public class PtkClientConnect : INetworkPacket
    {
        public long ID { get; set; }
        public IPEndPoint InternalEP { get; set; }
        public PtkClientConnect(long id, IPEndPoint iep)
        {
            this.ID = id;
            this.InternalEP = iep;
        }
    }

    [Serializable]
    public class PtkClientConnectAck : INetworkPacket
    {
        public long ID { get; set; }
        public NATClientInfo ConnectedClient { get; set; }
        public PtkClientConnectAck(long id, NATClientInfo client)
        {
            this.ID = id;
            this.ConnectedClient = client;
        }
    }

    [Serializable]
    public class PtkClientDisconnect : INetworkPacket
    {
        public long ID { get; set; }
        public PtkClientDisconnect(long id)
        {
            this.ID = id;
        }
    }

    [Serializable]
    public class PtkClientDisconnectAck : INetworkPacket
    {
        public long ID { get; set; }
        public long DisconnectClientID { get; set; }
        public PtkClientDisconnectAck(long id, long disconnectedClientid)
        {
            this.ID = id;
            this.DisconnectClientID = disconnectedClientid;
        }
    }

    [Serializable]
    public class PtkServerShutdown : INetworkPacket
    {
        public long ID { get; set; }
        public PtkServerShutdown()
        {
            this.ID = -1;
        }
    }

    [Serializable]
    public class PtkEchoMessage : INetworkPacket
    {
        public long ID { get; set; }
        public string Message { get; set; }
        public PtkEchoMessage(long id, string message)
        {
            this.ID = id;
            this.Message = message;
        }
    }

    [Serializable]
    public class PtkNatTraversalRequest : INetworkPacket
    {
        public long ID { get; set; }
        public long RecipientID { get; set; }
        public string NatToken { get; set; }

        public PtkNatTraversalRequest(long id, long recipientID, string natToken)
        {
            this.ID = id;
            this.RecipientID = recipientID;
            this.NatToken = natToken;
        }
    }

    [Serializable]
    public class PtkNatTraversalRequestAck : INetworkPacket
    {
        public long ID { get; set; }
        public long CallerID { get; set; }
        public string NatToken { get; set; }

        public PtkNatTraversalRequestAck(long id, long callerId, string natToken)
        {
            this.ID = id;
            this.CallerID = callerId;
            this.NatToken = natToken;
        }
    }

    [Serializable]
    public class PtkChatMessage : INetworkPacket
    {
        public long ID { get; set; }
        public string Message { get; set; }
        public PtkChatMessage(long id, string msg)
        {
            this.ID = id;
            this.Message = msg;
        }
    }

    [Serializable]
    public class PtkRequestP2PClientInfo : INetworkPacket
    {
        public long ID { get; set; }
        public long RequesterID { get; set; } //요청한 사람 ID
        public long ConnectedUserID { get; set; } //필요한 사람 ID
        public string Key { get; set; } //누군지 식별하기위한 키값


        //클라 -> P2P 상대에게 보낼 때
        public PtkRequestP2PClientInfo(long id, long requesterId, string key)
        {
            this.ID = id;
            this.RequesterID = requesterId;
            this.ConnectedUserID = -1;
            this.Key = key;
        }

        //P2P -> 서버에게 보낼 때
        public PtkRequestP2PClientInfo(long id, long requesterId, long connectedUserId, string key)
        {
            this.ID = id;
            this.RequesterID = requesterId;
            this.ConnectedUserID = connectedUserId;
            this.Key = key;
        }

    }

    [Serializable]
    public class PtkRequestP2PClientInfoAck : INetworkPacket
    {
        public long ID { get; set; }
        public string Key { get; set; }
        public NATClientInfo ClientInfo { get; set;}
        public PtkRequestP2PClientInfoAck(long id, NATClientInfo info, string key)
        {
            this.ID = id;
            this.ClientInfo = info;
            this.Key = key;
        }
    }

    [Serializable]
    public class PtkReliableTestStart : INetworkPacket
    {
        public long ID { get; set; }
        public PtkReliableTestStart(long id)
        {
            this.ID = id;
        }
    }

    [Serializable]
    public class PtkReliableTestStartAck : INetworkPacket
    {
        public long ID { get; set; }
        public PtkReliableTestStartAck(long id)
        {
            this.ID = id;
        }
    }

    [Serializable]
    public class PtkReliableTest : INetworkPacket
    {
        public long ID { get; set; }
        public byte[] ttt { get; }

        public PtkReliableTest(long id)
        {
            this.ID = id;
            this.ttt = Enumerable.Repeat<byte>(125, 3000).ToArray();
        }
    }

    [Serializable]
    public class PtkReliableTestAck : INetworkPacket
    {
        public long ID { get; set; }
        public bool Over { get; }
        public byte[] ttt { get; }

        public PtkReliableTestAck(long id, bool isOver)
        {
            this.ID = id;
            this.Over = isOver;
            this.ttt = Enumerable.Repeat<byte>(125, 3000).ToArray();

        }
    }
}
