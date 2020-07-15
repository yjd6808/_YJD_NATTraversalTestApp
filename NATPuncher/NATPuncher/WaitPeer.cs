// ===============================
// @AUTHOR      : 윤정도
// @CREATE DATE : 2020-05-30 오후 9:21:44   
// @PURPOSE     : NAT 연결 대기 Peer
// ===============================


using System;
using System.Net;

namespace NATPuncher
{
    class WaitPeer
    {
        public IPEndPoint InternalAddr { get; private set; }
        public IPEndPoint ExternalAddr { get; private set; }
        public DateTime RefreshTime { get; private set; }

        public void Refresh()
        {
            RefreshTime = DateTime.Now;
        }

        public WaitPeer(IPEndPoint internalAddr, IPEndPoint externalAddr)
        {
            Refresh();
            InternalAddr = internalAddr;
            ExternalAddr = externalAddr;
        }
    }
}
