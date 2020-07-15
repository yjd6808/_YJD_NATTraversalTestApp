using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using LiteNetLib;

namespace LibSample
{
    class WaitPeer
    {
        public IPEndPoint InternalAddr { get; }
        public IPEndPoint ExternalAddr { get; }
        public DateTime RefreshTime { get; private set; }

        public void Refresh()
        {
            RefreshTime = DateTime.UtcNow;
        }

        public WaitPeer(IPEndPoint internalAddr, IPEndPoint externalAddr)
        {
            Refresh();
            InternalAddr = internalAddr;
            ExternalAddr = externalAddr;
        }
    }

    class HolePunchServerTest : INatPunchListener
    {
        private const int ServerPort = 12345;
        private const string ConnectionKey = "test_key";
        private static readonly TimeSpan KickTime = new TimeSpan(0, 0, 6);

        private readonly Dictionary<string, WaitPeer> _waitingPeers = new Dictionary<string, WaitPeer>();
        private readonly List<string> _peersToRemove = new List<string>();
        private NetManager _puncher;
        private NetManager _c1;
        private NetManager _c2;
        private NetManager _c3;
        private NetManager _c4;

        void INatPunchListener.OnNatIntroductionRequest(IPEndPoint localEndPoint, IPEndPoint remoteEndPoint, string token)
        {
            if (_waitingPeers.TryGetValue(token, out var wpeer))
            {
                if (wpeer.InternalAddr.Equals(localEndPoint) &&
                    wpeer.ExternalAddr.Equals(remoteEndPoint))
                {
                    wpeer.Refresh();
                    return;
                }

                Console.WriteLine("Wait peer found, sending introduction...");

                //found in list - introduce client and host to eachother
                Console.WriteLine(
                    "host - i({0}) e({1})\nclient - i({2}) e({3})",
                    wpeer.InternalAddr,
                    wpeer.ExternalAddr,
                    localEndPoint,
                    remoteEndPoint);

                _puncher.NatPunchModule.NatIntroduce(
                    wpeer.InternalAddr, // host internal
                    wpeer.ExternalAddr, // host external
                    localEndPoint, // client internal
                    remoteEndPoint, // client external
                    token // request token
                    );

                //Clear dictionary
                _waitingPeers.Remove(token);
            }
            else
            {
                Console.WriteLine("Wait peer created. i({0}) e({1})", localEndPoint, remoteEndPoint);
                _waitingPeers[token] = new WaitPeer(localEndPoint, remoteEndPoint);
            }
        }

        void INatPunchListener.OnNatIntroductionSuccess(IPEndPoint targetEndPoint, NatAddressType type, string token)
        {
            //Ignore we are server
        }

        public void Run()
        {
            Console.WriteLine("=== HolePunch Test ===");

            EventBasedNetListener client1Listener = new EventBasedNetListener();
            EventBasedNetListener client2Listener = new EventBasedNetListener();
            EventBasedNetListener client3Listener = new EventBasedNetListener();
            EventBasedNetListener client4Listener = new EventBasedNetListener();
            EventBasedNetListener serverListener = new EventBasedNetListener();
            EventBasedNatPunchListener natPunchListener1 = new EventBasedNatPunchListener();
            EventBasedNatPunchListener natPunchListener2 = new EventBasedNatPunchListener();
            EventBasedNatPunchListener natPunchListener3 = new EventBasedNatPunchListener();
            EventBasedNatPunchListener natPunchListener4 = new EventBasedNatPunchListener();

            client1Listener.PeerConnectedEvent += peer =>
            {
                Console.WriteLine("PeerConnected: " + peer.EndPoint + " / " + peer.GetHashCode());

                foreach (NetPeer p in _c1)
                    Console.WriteLine("C1 : " + p.Id + " / " + p.EndPoint + " / " + p.NetManager.LocalEndPointv4 + " / " + p.GetHashCode());
            };

            client1Listener.ConnectionRequestEvent += request =>
            {
                request.AcceptIfKey(ConnectionKey);
            };

            client1Listener.PeerDisconnectedEvent += (peer, disconnectInfo) =>
            {
                Console.WriteLine("C1 : PeerDisconnected: " + disconnectInfo.Reason);
                if (disconnectInfo.AdditionalData.AvailableBytes > 0)
                {
                    Console.WriteLine("C1 : Disconnect data: " + disconnectInfo.AdditionalData.GetInt());
                }
            };

            client2Listener.PeerConnectedEvent += peer =>
            {
                Console.WriteLine("C2 : PeerConnected: " + peer.EndPoint + " / " + peer.GetHashCode());

                foreach (NetPeer p in _c2)
                    Console.WriteLine("C2 : " + p.Id + " / " + p.EndPoint + " / " + p.NetManager.LocalEndPointv4 + " / " + p.GetHashCode());
            };

            client2Listener.ConnectionRequestEvent += request =>
            {
                request.AcceptIfKey(ConnectionKey);
            };

            client2Listener.PeerDisconnectedEvent += (peer, disconnectInfo) =>
            {
                Console.WriteLine("C2 : PeerDisconnected: " + disconnectInfo.Reason);
                if (disconnectInfo.AdditionalData.AvailableBytes > 0)
                {
                    Console.WriteLine("C2 : Disconnect data: " + disconnectInfo.AdditionalData.GetInt());
                }
            };

            natPunchListener1.NatIntroductionSuccess += (point, addrType, token) =>
            {
                var peer = _c1.Connect(point, ConnectionKey);
                Console.WriteLine($"NatIntroductionSuccess C1. Connecting to C2: {point}, type: {addrType}, connection created: {peer != null}");
            };

            natPunchListener2.NatIntroductionSuccess += (point, addrType, token) =>
            {
                var peer = _c2.Connect(point, ConnectionKey);
                Console.WriteLine($"NatIntroductionSuccess C2. Connecting to C1: {point}, type: {addrType}, connection created: {peer != null}");
            };

            natPunchListener3.NatIntroductionSuccess += (point, addrType, token) =>
            {
                var peer = _c3.Connect(point, ConnectionKey);
                Console.WriteLine($"NatIntroductionSuccess C3. Connecting to C2: {point}, type: {addrType}, connection created: {peer != null}");
            };

            natPunchListener4.NatIntroductionSuccess += (point, addrType, token) =>
            {
                var peer = _c4.Connect(point, ConnectionKey);
                Console.WriteLine($"NatIntroductionSuccess C4. Connecting to C2: {point}, type: {addrType}, connection created: {peer != null}");
            };

            serverListener.PeerConnectedEvent += peer =>
            {
                Console.WriteLine("Server : PeerConnected: " + peer.EndPoint + " / " + peer.GetHashCode());

                foreach (NetPeer p in _c1)
                    Console.WriteLine("Server : " + p.Id + " / " + p.EndPoint + " / " + p.NetManager.LocalEndPointv4 + " / " + p.GetHashCode());
            };

            serverListener.ConnectionRequestEvent += request =>
            {
                request.AcceptIfKey(ConnectionKey);
            };

            serverListener.PeerDisconnectedEvent += (peer, disconnectInfo) =>
            {
                Console.WriteLine("Server : PeerDisconnected: " + disconnectInfo.Reason);
                if (disconnectInfo.AdditionalData.AvailableBytes > 0)
                {
                    Console.WriteLine("Server : Disconnect data: " + disconnectInfo.AdditionalData.GetInt());
                }
            };

            _c1 = new NetManager(client1Listener)
            {
                IPv6Enabled = IPv6Mode.DualMode,
                NatPunchEnabled = true
            };
            _c1.NatPunchModule.Init(natPunchListener1);
            _c1.Start();

            _c2 = new NetManager(client2Listener)
            {
                IPv6Enabled = IPv6Mode.DualMode,
                NatPunchEnabled = true
            };
            _c2.NatPunchModule.Init(natPunchListener2);
            _c2.Start();

            _c3 = new NetManager(client3Listener)
            {
                IPv6Enabled = IPv6Mode.DualMode,
                NatPunchEnabled = true
            };
            _c3.NatPunchModule.Init(natPunchListener3);
            _c3.Start();

            _c4 = new NetManager(client4Listener)
            {
                IPv6Enabled = IPv6Mode.DualMode,
                NatPunchEnabled = true
            };
            _c4.NatPunchModule.Init(natPunchListener4);
            _c4.Start();

            _puncher = new NetManager(serverListener)
            {
                IPv6Enabled = IPv6Mode.SeparateSocket,
                NatPunchEnabled = true
            };
            _puncher.Start(ServerPort);
            _puncher.NatPunchModule.Init(this);

            _c1.NatPunchModule.SendNatIntroduceRequest(new IPEndPoint(IPAddress.Parse("221.162.129.150"), ServerPort), "token1");
            _c2.NatPunchModule.SendNatIntroduceRequest(new IPEndPoint(IPAddress.Parse("221.162.129.150"), ServerPort), "token1");

            _c2.NatPunchModule.SendNatIntroduceRequest(new IPEndPoint(IPAddress.Parse("221.162.129.150"), ServerPort), "token2");
            _c3.NatPunchModule.SendNatIntroduceRequest(new IPEndPoint(IPAddress.Parse("221.162.129.150"), ServerPort), "token2");

            _c2.NatPunchModule.SendNatIntroduceRequest(new IPEndPoint(IPAddress.Parse("221.162.129.150"), ServerPort), "token3");
            _c4.NatPunchModule.SendNatIntroduceRequest(new IPEndPoint(IPAddress.Parse("221.162.129.150"), ServerPort), "token3");

            // keep going until ESCAPE is pressed
            Console.WriteLine("Press ESC to quit");

            while (true)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true).Key;
                    if (key == ConsoleKey.Escape)
                    {
                        break;
                    }
                    if (key == ConsoleKey.A)
                    {
                        Console.WriteLine("C1 stopped");
                        _c1.DisconnectPeer(_c1.FirstPeer, new byte[] { 1, 2, 3, 4 });
                        _c1.Stop();
                    }
                }

                DateTime nowTime = DateTime.UtcNow;

                _c1.NatPunchModule.PollEvents();
                _c2.NatPunchModule.PollEvents();
                _puncher.NatPunchModule.PollEvents();
                _c1.PollEvents();
                _c2.PollEvents();

                //check old peers
                foreach (var waitPeer in _waitingPeers)
                {
                    if (nowTime - waitPeer.Value.RefreshTime > KickTime)
                    {
                        _peersToRemove.Add(waitPeer.Key);
                    }
                }

                //remove
                for (int i = 0; i < _peersToRemove.Count; i++)
                {
                    Console.WriteLine("Kicking peer: " + _peersToRemove[i]);
                    _waitingPeers.Remove(_peersToRemove[i]);
                }
                _peersToRemove.Clear();

                Thread.Sleep(10);
            }

            _c1.Stop();
            _c2.Stop();
            _puncher.Stop();
        }

        public static void Main(String[] args)
        {
            new HolePunchServerTest().Run();
        }
    }
}