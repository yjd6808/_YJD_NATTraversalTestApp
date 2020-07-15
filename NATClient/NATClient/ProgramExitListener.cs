// ===============================
// @AUTHOR      : 윤정도
// @CREATE DATE : 2020-05-31 오후 7:19:35   
// @PURPOSE     : 프로그램 종료 감지
// ===============================


using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NATClient
{
    enum CtrlType
    {
        CTRL_C_EVENT = 0,
        CTRL_BREAK_EVENT = 1,
        CTRL_CLOSE_EVENT = 2,
        CTRL_LOGOFF_EVENT = 5,
        CTRL_SHUTDOWN_EVENT = 6
    }


    interface IExitListener
    {
        void OnConsoleExit(CtrlType sig);
    }

    class ProgramExitListener : IExitListener
    {
        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(EventHandler handler, bool add);

        public delegate bool EventHandler(CtrlType sig);
        public event EventHandler _handler;

        public void OnConsoleExit(CtrlType sig)
        {
            _handler?.Invoke(sig);
        }

        public void RefreshHandler()
        {
            SetConsoleCtrlHandler(_handler, true);
        }
    }
}
