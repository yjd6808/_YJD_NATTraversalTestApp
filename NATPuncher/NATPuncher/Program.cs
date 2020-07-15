using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiteNetLib;
using NATShared;

namespace NATPuncher
{
    class Program
    {
        static ProgramExitListener _exitListener = new ProgramExitListener();

        class A
        {
            
            public int a;
            public A(int a) { this.a = a; }
        }
        static void Main(string[] args)
        {
            Task.Run(() => NATPuncherApp.GetInstance().Run());
            _exitListener.ExitEvent += OnConsoleExit;
            _exitListener.RefreshHandler();

            while (true)
            {
                if (Console.KeyAvailable && Console.ReadLine().ToLower().Equals("exit") && NATPuncherApp.GetInstance().IsRunning())
                {
                    NATPuncherApp.GetInstance().Stop();
                    break;
                }
            }
        }

        private static bool OnConsoleExit(CtrlType sig)
        {
            if (sig == CtrlType.CTRL_CLOSE_EVENT)
            {
                if (NATPuncherApp.GetInstance().IsRunning())
                    NATPuncherApp.GetInstance().Stop();
            }
            return true;
        }
    }
}
