// ===============================
// @AUTHOR      : 윤정도
// @CREATE DATE : 2020-05-30 오후 3:53:34   
// @PURPOSE     : NAT Puncher 메인 어플
// ===============================


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LiteNetLib;
using LiteNetLib.Utils;
using NATShared;

namespace NATPuncher
{
    class NATPuncherApp
    {
        private static NATPuncherApp s_Instance;
        //<---------------------------------------------->
        private Dictionary<string, WaitPeer> _waitingPeers;
        private Dictionary<long, NATClientInfo> _connectedClients;
        private NetManager _puncher;
        private INATPuncherCustomizedListener _customizedEventListener;
        private NATPuncherListener _eventListener;
        private NATPacketProcessor _packetProcessor;
        private InterlockedBoolean _running;


        //<---------------------------------------------->
        public Dictionary<string, WaitPeer> WaitingPeers { get => _waitingPeers; }
        public Dictionary<long, NATClientInfo> ConnectedPeers { get => _connectedClients; }
        public INATPuncherCustomizedListener CustomizedEventListener { get => _customizedEventListener; }
        public NetManager Manager { get => _puncher; }
        public NATPacketProcessor PacketProcessor { get => _packetProcessor; }

        public static NATPuncherApp GetInstance()
        {
            if (s_Instance == null)
            {
                // < 초기화 >
                s_Instance = new NATPuncherApp();
                s_Instance._eventListener = new NATPuncherListener(s_Instance);
                s_Instance._customizedEventListener = s_Instance._eventListener;
                s_Instance._waitingPeers = new Dictionary<string, WaitPeer>();
                s_Instance._connectedClients = new Dictionary<long, NATClientInfo>();
                s_Instance._puncher = new NetManager(s_Instance._eventListener);
                s_Instance._puncher.NatPunchEnabled = true;
                s_Instance._puncher.NatPunchModule.Init(s_Instance._eventListener);
                s_Instance._running = new InterlockedBoolean();
                s_Instance._running.Value = false;
                s_Instance._packetProcessor = new NATPacketProcessor(s_Instance);
                ThreadSafeLogger.WriteLine("NAT Puncher 초기화완료");
            }
            return s_Instance;
        }

        public void Run()
        {
            if (_running.Value)
                return;

            _puncher.Start(NATPuncherConfiguration.ListenPort);
            _customizedEventListener.OnServerOpened();
            _running.Value = true;
            

            while (_running.Value)
            {
                _puncher.PollEvents();
                _puncher.NatPunchModule.PollEvents();
            }
        }

        public void Stop()
        {
            //서버가 종료되었음을 다른 클라이언트 들에게 알려줌
            Broadcast(new PtkServerShutdown());

            _waitingPeers.Clear();
            _connectedClients.Clear();

            _running.Value = false;
            _puncher.Flush();
            _puncher.Stop();
            _customizedEventListener.OnServerClosed();
        }

        public bool IsRunning() => _running.Value;

        public void SendPacketToPeer(NetPeer peer, INetworkPacket packet)
        {
            if (peer.ConnectionState != ConnectionState.Connected)
            {
                ThreadSafeLogger.WriteLine("상대방과 연결중이지 않습니다. 메시지 전송에 실패했습니다.");
                return;
            }

            NetDataWriter writer = new NetDataWriter();
            writer.Put(packet.ToByteArray());
            peer.Send(writer, DeliveryMethod.ReliableOrdered);
            ThreadSafeLogger.WriteLine("데이터 전송완료 (전송타입 : {0})", DeliveryMethod.ReliableOrdered);
        }

        public void Broadcast(INetworkPacket packet)
        {
            foreach (NATClientInfo peer in _connectedClients.Values)
            {
                NetDataWriter writer = new NetDataWriter();
                writer.Put(packet.ToByteArray());
                peer.Peer.Send(writer, DeliveryMethod.ReliableOrdered);
            }
            ThreadSafeLogger.WriteLine("브로드 캐스팅 전송완료 (전송타입 : {0})", DeliveryMethod.ReliableOrdered);
        }
    }
}
