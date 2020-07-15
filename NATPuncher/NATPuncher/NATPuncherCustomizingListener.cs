// ===============================
// @AUTHOR      : 윤정도
// @CREATE DATE : 2020-05-31 오후 12:44:11   
// @PURPOSE     : 자체제작 이벤트 리스너 / 패킷 프로세싱 목적
// ===============================


using LiteNetLib;
using NATShared;

namespace NATPuncher
{
    public interface INATPuncherCustomizedListener
    {
        void OnServerOpened();
        void OnServerClosed();
        void OnProcessPacket(NetPeer sender, INetworkPacket networkPacket);
    }

    class NATPuncherCustomizedListener : INATPuncherCustomizedListener
    {
        public delegate void OnServerOpened();
        public delegate void OnServerClosed();
        public delegate void OnProcessPacket(NetPeer sender, INetworkPacket networkPacket);

        public event OnServerOpened OnServerOpenedEvent;
        public event OnServerOpened OnServerClosedEvent;
        public event OnProcessPacket OnProcessPacketEvent;

        void INATPuncherCustomizedListener.OnServerOpened()
        {
            OnServerOpenedEvent?.Invoke();
        }

        void INATPuncherCustomizedListener.OnServerClosed()
        {
            OnServerClosedEvent?.Invoke();
        }

        void INATPuncherCustomizedListener.OnProcessPacket(NetPeer sender, INetworkPacket networkPacket)
        {
            OnProcessPacketEvent?.Invoke(sender, networkPacket);
        }
    }
}
