// ===============================
// @AUTHOR      : 윤정도
// @CREATE DATE : 2020-05-30 오후 3:49:21   
// @PURPOSE     : NAT 클라이언트 어플리케이션
// ===============================


using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using LiteNetLib;
using LiteNetLib.Utils;
using NATShared;

namespace NATClient
{
    class NATTraversalPeer
    {
        public string Token { get; set; }
        public  NATClientInfo ClientInfo { get; set; }

        public NATTraversalPeer(string token, NATClientInfo peer)
        {
            Token = token;
            ClientInfo = peer;
        }
    }

    class NATClientApp
    {
        private static NATClientApp s_Instance = null;
        //<---------------------------------------------->

        private NetManager _manager;
        private long _Id;
        private NetPeer _serverPeer;
        private Dictionary<long, NATClientInfo> _serverConnectedClients;
        private Dictionary<string, NetPeer> _p2PConnectingWaitingPeers; //P2P로 연결되었으나 누군지 알지 못해 서버에 요청해 누군지 확인하기위해 대기중인 유저
        private INATClientCustomizedListener _customizedEventListener;
        private InterlockedBoolean _connected;
        private InterlockedBoolean _running;
        private NATClientListener _eventListener;
        private NATPacketProcessor _packetProcessor;

        public int MaxTestPacketCount { get => 2000; }
        public int TestPacketCount { get; set; }
        public bool IsTestCase8Running { get; set; }

        public NetManager Manager { get => _manager; }
        public NetPeer ServerPeer { get => _serverPeer; }
        public INATClientCustomizedListener CustomizedEventListener { get => _customizedEventListener; }
        public NATPacketProcessor PacketProcessor { get => _packetProcessor; }
        public Dictionary<long, NATClientInfo> ServerConnectedClients { get => _serverConnectedClients; }
        public Dictionary<string, NetPeer> P2PConnectingWaitingPeers { get => _p2PConnectingWaitingPeers; }
        public IEnumerable<NetPeer> P2PConnectingPeers { get => _manager.Where(x => x != _serverPeer); }
        public long ID { get => _Id;  }
        public bool IsRunning() => _running.Value;
        public bool IsConnected() => _connected.Value;

        public string InternalIP
        {
            get
            {
                if (IsConnected())
                    return _manager.LocalEndPointv4.ToString();
                else
                    return "N/A";
            }
        }


        public static NATClientApp GetInstance()
        {
            
            if (s_Instance == null)
            {
                s_Instance = new NATClientApp();
                s_Instance._eventListener = new NATClientListener(s_Instance);
                s_Instance._running = new InterlockedBoolean();
                s_Instance._running.Value = false;
                s_Instance._customizedEventListener = s_Instance._eventListener;
                s_Instance._manager = new NetManager(s_Instance._eventListener);
                s_Instance._manager.NatPunchEnabled = true;
                s_Instance._manager.NatPunchModule.Init(s_Instance._eventListener);
                s_Instance._connected = new InterlockedBoolean();
                s_Instance._connected.Value = false;
                s_Instance._packetProcessor = new NATPacketProcessor(s_Instance);
                s_Instance._serverConnectedClients = new Dictionary<long, NATClientInfo>();
                s_Instance._p2PConnectingWaitingPeers = new Dictionary<string, NetPeer>();
                s_Instance._Id = DateTime.Now.Ticks;
                NetDebug.Logger = ThreadSafeLogger.GetInstance(HostType.Client, s_Instance._Id.ToString());
                ThreadSafeLogger.WriteLine("NAT Client 초기화 완료");
            }
            return s_Instance;
        }

        public void Run()
        {
            if (_connected.Value && _running.Value)
            {
                ThreadSafeLogger.WriteLine("NAT Client 어플리케이션은 이미 실행중입니다.");
                return;
            }

            ThreadSafeLogger.WriteLine("NAT Client 시작");
            _manager.Start();
            _running.Value = true;
            ThreadSafeLogger.WriteLine("NAT Puncher 서버로 접속을 시도합니다");
            _serverPeer = _manager.Connect(NATClientConfiguration.ServerIP, NATClientConfiguration.ServerPort, NATClientConfiguration.ConnectionKey);
            while (_running.Value)
            {
                _manager.PollEvents();
                _manager.NatPunchModule.PollEvents();
                ConnectionStateCheck();
            }
        }
        public void ConnectionStateCheck()
        {
            if (_connected.Value)
                return;

            if (_serverPeer.ConnectionState == ConnectionState.Connected)
            {
                _connected.Value = true;
                _customizedEventListener.OnClientNetworkConnected();
            }
        }

        public void Stop(bool serverShutdown = false)
        {
            if (serverShutdown == false)
                SendPacketToMasterServer(new PtkClientDisconnect(ID));
            
            ThreadSafeLogger.WriteLine("NAT Client 중지");

            _serverConnectedClients.Clear();
            _customizedEventListener.OnClientNetworkClosed();
            _manager.Flush();
            _manager.Stop();
            _connected.Value = false;
            _running.Value = false;
        }

        

        public void SendNatIntroductionRequest(string token)
        {
            _manager.NatPunchModule.SendNatIntroduceRequest(
                NetUtils.MakeEndPoint(NATClientConfiguration.ServerIP, NATClientConfiguration.ServerPort), 
                token);
        }

        public void SendEchoMessageToMasterServer(string msg)
        {
            if (_serverPeer.ConnectionState != ConnectionState.Connected)
            {
                ThreadSafeLogger.WriteLine("상대방과 연결중이지 않습니다. 메시지 전송에 실패했습니다.");
                return;
            }

            NetDataWriter writer = new NetDataWriter();
            writer.Put(new PtkEchoMessage(_Id, msg).ToByteArray());
            _serverPeer.Send(writer, DeliveryMethod.ReliableOrdered);
            ThreadSafeLogger.WriteLine("메시지 전송완료");
        }

        public void SendPacketToMasterServer(INetworkPacket packet)
        {
            if (_serverPeer == null)
            {
                ThreadSafeLogger.WriteLine("중앙서버 피어의 데이터가 존재하지 않습니다. 연결을 확인해주세요.");
                return;
            }

            if (_serverPeer.ConnectionState != ConnectionState.Connected)
            {
                ThreadSafeLogger.WriteLine("상대방과 연결중이지 않습니다. 메시지 전송에 실패했습니다.");
                return;
            }

            NetDataWriter writer = new NetDataWriter();
            writer.Put(packet.ToByteArray());
            _serverPeer.Send(writer, DeliveryMethod.ReliableOrdered);
            ThreadSafeLogger.WriteLine("메시지 전송완료");
        }

        public void SendPacketToPeer(NetPeer peer, INetworkPacket packet)
        {
            //if (peer.ConnectionState != ConnectionState.Connected)
            //{
            //    ThreadSafeLogger.WriteLine("상대방과 연결중이지 않습니다. 메시지 전송에 실패했습니다.");
            //    return;
            //}

            NetDataWriter writer = new NetDataWriter();
            writer.Put(packet.ToByteArray());
            peer.Send(writer, DeliveryMethod.ReliableOrdered);

            if (IsTestCase8Running == false)
                ThreadSafeLogger.WriteLine(peer.EndPoint + "에게 데이터 전송완료 (전송타입 : {0})", DeliveryMethod.ReliableOrdered);
        }


        public void PrintServerConnectedClients()
        {
            int idx = 0;

            foreach (NATClientInfo peerInfo in ServerConnectedClients.Values)
            {
                if (peerInfo == null)
                    ThreadSafeLogger.WriteLine("{0}.\tID ({1})", idx++, "알 수 없는 ID");
                else
                    ThreadSafeLogger.WriteLine("{0}.\tID ({1})", idx++, peerInfo.ID);
            }

            if (_manager.Count() <= 1)
            {
                ThreadSafeLogger.WriteLine("중앙 서버에 접속중인 유저가 없습니다");
            }
        }

        public void PrintP2PConnectedClients()
        {
            int idx = 0;

            foreach (NetPeer peer in P2PConnectingPeers)
            {
                NATClientInfo clientInfo = peer.Tag as NATClientInfo;

                if (clientInfo == null)
                    ThreadSafeLogger.WriteLine(true, "{0}.\tID ({1}) / EP ({2})", idx++, "알 수 없는 ID",  peer.EndPoint);
                else
                    ThreadSafeLogger.WriteLine(true, "{0}.\tID ({1}) / EP ({2})", idx++, clientInfo.ID, peer.EndPoint);
            }

            if (P2PConnectingPeers.Count() <= 0)
            {
                ThreadSafeLogger.WriteLine("중앙 서버에 접속중인 유저가 없습니다");
            }
        }
    }

    public static class Ex
    {
        public static NetPeer GetPeerByTickID(this NetManager manager, long peerId)
        {
            return manager.FirstOrDefault(x =>
            {
                NATClientInfo clientInfo = x.Tag as NATClientInfo;

                if (clientInfo != null && clientInfo.ID == peerId)
                    return true;
                else if (clientInfo == null)
                    Console.WriteLine(x.EndPoint + "의 정보가 null 입니다.");
                return false;
            });
        }

        public static NATClientInfo GetPeerInfoByTickID(this NetManager manager, long peerId)
        {
            NetPeer peer = manager.GetPeerByTickID(peerId);

            if (peer != null && peer.Tag != null)
                return (NATClientInfo)peer.Tag;
            else
                return null;
        }

        public static NATClientInfo GetPeerInfoByNetPeerID(this NetManager manager, int peerId)
        {
            NetPeer peer = manager.GetPeerById(peerId);

            if (peer != null && peer.Tag != null)
                return (NATClientInfo)peer.Tag;
            else
                return null;
        }
    }
}
