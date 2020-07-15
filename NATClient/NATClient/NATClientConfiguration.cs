// ===============================
// @AUTHOR      : 윤정도
// @CREATE DATE : 2020-05-30 오후 10:09:22   
// @PURPOSE     : 클라이언트 설정
// ===============================


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NATClient
{
    class NATClientConfiguration
    {
        public static string ServerIP = "221.162.129.150";
        public static int    ServerPort = 12345;
        public static string NatTraversalToken = "test_token";
        public static string ConnectionKey = "Puncher";
    }
}
