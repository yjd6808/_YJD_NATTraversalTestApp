// ===============================
// @AUTHOR      : 윤정도
// @CREATE DATE : 2020-05-31 오전 12:06:38   
// @PURPOSE     : 패킷모음
// ===============================

using LiteNetLib;
using System;
using System.Collections.Generic;
using System.Net;

namespace NATShared
{
    [Serializable]
    public class NATClientInfo
    {
        public long ID;

        public IPEndPoint InternalEndpoint;
        public IPEndPoint ExternalEndpoint;
        

        [NonSerialized]
        public NetPeer Peer;

        [NonSerialized]
        public NetPeer P2PPeer; //클라에서만 씀

        [NonSerialized]
        public bool P2PConnected; //클라에서만 씀

        [NonSerialized]
        public string NatToken;


        public NATClientInfo(long Id, IPEndPoint internalEP, IPEndPoint externalEP)
        {
            this.ID = Id;
            this.InternalEndpoint = internalEP;
            this.ExternalEndpoint = externalEP;
        }

        public NATClientInfo(long Id, IPEndPoint internalEP, IPEndPoint externalEP, NetPeer peer)
        {
            this.ID = Id;
            this.InternalEndpoint = internalEP;
            this.ExternalEndpoint = externalEP;
            this.Peer = peer;
        }

        public void UpdatePeer(NetPeer peer) => Peer = peer;
        public void Update(NATClientInfo client)
        {
            this.Peer = client.Peer;
            this.ExternalEndpoint = client.ExternalEndpoint;
            this.InternalEndpoint = client.InternalEndpoint;
            this.ID = client.ID;
        }

        public override string ToString()
        {
            return string.Format("ID ({0}) / IEP ({1}) / EEP ({2})", ID, InternalEndpoint == null ? "N/A" : InternalEndpoint.ToString(), ExternalEndpoint == null ? "N/A" : ExternalEndpoint.ToString());
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
}
