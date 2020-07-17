using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LiteNetLib;
using LiteNetLib.Utils;
using NATShared;

namespace NATClient
{
    class Program
    {
        static ProgramExitListener _exitListener = new ProgramExitListener();

        static void Main(string[] args)
        {
            ThreadSafeLogger.WriteLine("NAT Traversal Test 클라이언트 입니다.");
            ThreadSafeLogger.WriteLine("테스트 조건 (1) : 해당 클라이언트는 공유기 환경에서 실행되어야합니다. (아닐 경우 제대로된 테스트 안됨)");
            ThreadSafeLogger.WriteLine("테스트 조건 (2) : 1 ~ 8번 옵션 모두 제대로 실행될 경우 Udp Nat Traversal 테스트는 성공입니다.");
            ThreadSafeLogger.WriteLine("테스트 목적 : Udp Nat Traversal 검증 및 패킷 손실 없는 Udp 통신입니다.");
            ThreadSafeLogger.WriteLine("작성자 : 윤정도\n");
            ThreadSafeLogger.WriteLine("<---------------------------------------------------------->\n");

            SetupConnectionInfo();
            _exitListener._handler += OnConsoleExit;
            _exitListener.RefreshHandler();

            while (true)
            {
                if (NATClientApp.GetInstance().IsTestCase8Running)
                    continue;

                ThreadSafeLogger.WriteLine("당신의 ID : {0} / Internal IP : {1}", NATClientApp.GetInstance().ID, NATClientApp.GetInstance().InternalIP);
                ThreadSafeLogger.WriteLine("1. 서버와 연결");
                ThreadSafeLogger.WriteLine("2. 서버와 연결 종료");
                ThreadSafeLogger.WriteLine("3. 서버에 에코 메시지 전달");
                ThreadSafeLogger.WriteLine("4. 서버에 접속한 클라이언트 목록 출력");
                ThreadSafeLogger.WriteLine("5. NAT 홀펀칭 P2P 연결시도");
                ThreadSafeLogger.WriteLine("6. P2P로 연결된 클라 목록 보기");
                ThreadSafeLogger.WriteLine("7. P2P로 연결된 클라에게 채팅 메시지 전송");
                ThreadSafeLogger.WriteLine("8. P2P로 연결된 클라에게 Udp 패킷 신뢰성 검사 4096바이트 에코 송수신 2천회 시작");
                ThreadSafeLogger.Write("> 입력 : ");
                string cmd = Console.ReadLine();

                if (Console.KeyAvailable && cmd.ToLower().Equals("exit") && NATClientApp.GetInstance().IsRunning())
                {
                    NATClientApp.GetInstance().Stop();
                    break;
                }

                int.TryParse(cmd, out int choose);

                if (choose >= 3 && choose <= 7 && NATClientApp.GetInstance().IsConnected() == false)
                {
                    ThreadSafeLogger.WriteLine("중앙서버에 우선 접속해주세요.");
                    continue;
                }

                switch (choose)
                {
                    //1. 서버와 연결
                    case 1:
                        Task.Run(() => NATClientApp.GetInstance().Run());
                        break;

                    //2. 서버와 연결 종료
                    case 2:
                        NATClientApp.GetInstance().Stop();
                        break;

                    //3. 서버에 에코 메시지 전달
                    case 3:
                        ThreadSafeLogger.Write("> 전송할 메시지 입력 : ");
                        string msg = Console.ReadLine();
                        if (msg.Length <= 0)
                        {
                            ThreadSafeLogger.WriteLine("메시지를 입력하지 않아서 메시지를 전송하지 못했습니다.");
                            continue;
                        }
                        else
                        {
                            ThreadSafeLogger.WriteLine("[서버에게 에코 메시지를 전송합니다.]");
                            NATClientApp.GetInstance().SendEchoMessageToMasterServer(msg);
                        }
                        break;

                    //4. 서버에 접속한 클라이언트 목록 출력
                    case 4:
                        ThreadSafeLogger.WriteLine("[중앙서버에 접속중인 유저목록을 출력합니다.]");
                        NATClientApp.GetInstance().PrintServerConnectedClients();
                        break;

                    //5. NAT 홀펀칭 P2P 연결시도
                    case 5:
                        if (NATClientApp.GetInstance().ServerConnectedClients.Count <= 1)
                        {
                            ThreadSafeLogger.WriteLine("당신을 제외하고 연결할 유저가 없습니다.");
                            continue;
                        }

                        ThreadSafeLogger.Write("> 연결할 클라이언트를 선택해주세요 : ");
                        string idxStr = Console.ReadLine();

                        int.TryParse(idxStr, out int selectedClientIdx);

                        if (idxStr.Length <= 0)
                            ThreadSafeLogger.WriteLine("메시지를 입력하지 않아서 연결하지 못했습니다");
                        else
                        {
                            if (selectedClientIdx >= 1 && selectedClientIdx <= NATClientApp.GetInstance().ServerConnectedClients.Count)
                            {
                                KeyValuePair<long, NATClientInfo> selectedClient = NATClientApp.GetInstance().ServerConnectedClients.ElementAt(selectedClientIdx - 1);
                                string recipientID = selectedClient.Key.ToString();
                                string natToken = RandomString(10);
                                selectedClient.Value.P2PNatToken = natToken;

                                if (long.Parse(recipientID) == NATClientApp.GetInstance().ID)
                                {
                                    ThreadSafeLogger.WriteLine("자기 자신에게 요청 불가능합니다");
                                    continue;
                                }

                                ThreadSafeLogger.WriteLine("중앙서버에게 {0}의 토큰값으로 NAT Traversal Introduction을 요청했습니다.", recipientID);

                                NATClientApp.GetInstance().SendPacketToMasterServer(new PtkNatTraversalRequest(NATClientApp.GetInstance().ID, long.Parse(recipientID), natToken));
                                NATClientApp.GetInstance().SendNatIntroductionRequest(natToken);
                            }
                            else
                                ThreadSafeLogger.WriteLine("인덱스 번호를 정확히 입력해주세요.");
                        }
                        break;

                    //6.P2P로 연결된 클라 목록 보기
                    case 6:
                        ThreadSafeLogger.WriteLine("[P2P 연결중인 유저목록을 출력합니다.]");
                        NATClientApp.GetInstance().PrintP2PConnectedClients();
                        break;
                    //7. P2P로 연결된 클라에게 채팅 메시지 전송
                    case 7:
                        if (NATClientApp.GetInstance().P2PConnectingPeers.Count() <= 0)
                        {
                            ThreadSafeLogger.WriteLine("연결된 P2P 클라이언트가 없습니다. 5번 항목을 먼저 진행해주세요.");
                            continue;
                        }

                        ThreadSafeLogger.Write("> 채팅을 전송할 P2P 클라이언트를 선택해주세요 : ");
                        string p2pClientIdxStr = Console.ReadLine();

                        int.TryParse(p2pClientIdxStr, out int selectedP2pClientIdx);

                        if (p2pClientIdxStr.Length <= 0)
                            ThreadSafeLogger.WriteLine("메시지를 입력하지 않아서 전송하지 못했습니다");
                        else
                        {
                            if (selectedP2pClientIdx >= 0 && selectedP2pClientIdx < NATClientApp.GetInstance().Manager.Count())
                            {
                                ThreadSafeLogger.Write("> 전송할 메시지 입력 : ");
                                string p2pMsg = Console.ReadLine();
                                if (p2pMsg.Length <= 0)
                                {
                                    ThreadSafeLogger.WriteLine("메시지를 입력하지 않아서 메시지를 전송하지 못했습니다.");
                                    continue;
                                }
                                else
                                {
                                    NATClientInfo clientInfo = NATClientApp.GetInstance().P2PConnectingPeers.ElementAt(selectedP2pClientIdx).Tag as NATClientInfo;
                                    if (clientInfo == null)
                                    {
                                        ThreadSafeLogger.WriteLine("당신의 서버에서 정보를 찾을 수 없습니다. 이상한 유저입니다.");
                                    }
                                    else
                                    {
                                        ThreadSafeLogger.WriteLine(clientInfo.ID + " [NAT 클라에게 채팅 메시지를 전송합니다.] " + clientInfo.Peer.EndPoint);
                                        NATClientApp.GetInstance().SendPacketToPeer(clientInfo.Peer, new PtkChatMessage(NATClientApp.GetInstance().ID, p2pMsg));
                                    }
                                }
                            }
                        }
                        break;

                    //8.P2P로 연결된 클라에게 Udp 패킷 신뢰성 검사 4096바이트 2천회 전송
                    case 8:
                        if (NATClientApp.GetInstance().P2PConnectingPeers.Count() <= 0)
                        {
                            ThreadSafeLogger.WriteLine("연결된 P2P 클라이언트가 없습니다. 5번 항목을 먼저 진행해주세요.");
                            continue;
                        }

                        ThreadSafeLogger.Write("> 테스트 통신을 시도할 P2P 클라이언트를 선택해주세요 : ");
                        string p2pClientIdxStr2 = Console.ReadLine();

                        int.TryParse(p2pClientIdxStr2, out int selectedP2pClientIdx2);
                        if (p2pClientIdxStr2.Length <= 0)
                            ThreadSafeLogger.WriteLine("선택을 해라 좀!");
                        else
                        {
                            NATClientInfo clientInfo = NATClientApp.GetInstance().P2PConnectingPeers.ElementAt(selectedP2pClientIdx2).Tag as NATClientInfo;
                            if (clientInfo == null)
                            {
                                ThreadSafeLogger.WriteLine("당신의 서버에서 정보를 찾을 수 없습니다. 이상한 유저입니다.");
                            }
                            else
                            {
                                Console.Clear();
                                Console.CursorVisible = false;
                                NATClientApp.GetInstance().TestPacketCount = 0;
                                NATClientApp.GetInstance().IsTestCase8Running = true;
                                ThreadSafeLogger.WriteLine(clientInfo.ID + " 와 패킷통신 무결성 검증 테스트 2천회를 시작합니다.\n이 작업을 하는동안 다른 작업은 하실 수 없습니다.");
                                NATClientApp.GetInstance().SendPacketToPeer(clientInfo.Peer, new PtkReliableTestStart(NATClientApp.GetInstance().ID));
                            }
                        }
                        break;
                    default:
                        break;
                }
                ThreadSafeLogger.WriteLine();
            }
        }

        private static void SetupConnectionInfo()
        {
            //while (true)
            //{
            //    Console.WriteLine("접속할 서버의 IP와 포트를 함께 입력해주세요 (ex : 192.168.0.1:12121) : ");
            //    string ip = Console.ReadLine();

            //    IPEndPoint endPoint = ip.MakeEndPoint();

            //    if (endPoint != null)
            //    {
            //        NATClientConfiguration.ServerIP = endPoint.Address.ToString();
            //        NATClientConfiguration.ServerPort = endPoint.Port;
            //        break;
            //    }

            //    Console.WriteLine("잘못된 IP 형식을 입력하셨습니다. 다시 입력해주세요.");
            //}
            
        }

        private static bool OnConsoleExit(CtrlType sig)
        {
            if (sig == CtrlType.CTRL_CLOSE_EVENT)
            {
                if (NATClientApp.GetInstance().IsRunning())
                    NATClientApp.GetInstance().Stop();
            }
            return true;
        }

        public static string RandomString(int length)
        {
            Random random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }

   


    static class StringExtension
    {
       
        public static IPEndPoint MakeEndPoint(this string ip)
        {
            if (ip.Contains(":") == false)
                return null;

            string[] splited = ip.Split(':');
            string parsedIpAddress = splited[0];
            int.TryParse(splited[1], out int port);

            if (port == 0)
                return null;

            try
            {
                return new IPEndPoint(IPAddress.Parse(parsedIpAddress), port);
            }
            catch (Exception e)
            {
                ThreadSafeLogger.WriteLine(e.Message);
                return null;
            }
        }
    }
}
