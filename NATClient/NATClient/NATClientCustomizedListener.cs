// ===============================
// @AUTHOR      : 윤정도
// @CREATE DATE : 2020-05-31 오후 1:16:11   
// @PURPOSE     : 
// ===============================


using LiteNetLib;
using NATShared;

namespace NATClient
{
    public interface INATClientCustomizedListener
    {
        //펀쳐 서버와 연결이 성공했을 때 호출
        void OnClientNetworkConnected();

        //펀쳐 서버와 연결이 끊어졌을 때 호출
        void OnClientNetworkClosed();
        void OnProcessPacket(NetPeer sender,  INetworkPacket networkPacket);
    }

    class NATPuncherCustomizedListener : INATClientCustomizedListener
    {
        public delegate void OnClientNetworkConnected();
        public delegate void OnClientNetworkClosed();
        public delegate void OnProcessPacket(NetPeer sender, INetworkPacket networkPacket);

        public event OnClientNetworkConnected OnClientNetworkConnectedEvent;
        public event OnClientNetworkClosed OnClientNetworkClosedEvent;
        public event OnProcessPacket OnProcessPacketEvent;

        void INATClientCustomizedListener.OnClientNetworkConnected()
        {
            OnClientNetworkConnectedEvent?.Invoke();
        }

        void INATClientCustomizedListener.OnClientNetworkClosed()
        {
            OnClientNetworkClosedEvent?.Invoke();
        }

        void INATClientCustomizedListener.OnProcessPacket(NetPeer sender, INetworkPacket networkPacket)
        {
            OnProcessPacketEvent?.Invoke(sender, networkPacket);
        }
    }
}
