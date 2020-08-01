// ===============================
// @AUTHOR      : 윤정도
// @CREATE DATE : 2020-07-18 오후 5:09:09   
// @PURPOSE     : 
// ===============================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiteNetLib;
using LiteNetLib.Utils;

namespace NATShared
{
    public enum HostType
    {
        Server,
        Client
    }

    public class ThreadSafeLogger : INetLogger
    {
        private static object _locker = new object();
        private static ThreadSafeLogger s_Instance;

        private FileStream _debugFileStream;
        private StreamWriter _debugStreamWriter;

        public static void WriteLine(bool saveLog = true)
        {
            lock (_locker)
            {
                Console.WriteLine();

                if (s_Instance != null && saveLog)
                    s_Instance._debugStreamWriter.WriteLine();
            }
        }

        public static void WriteLine(string value, bool saveLog = true)
        {
            lock (_locker)
            {
                Console.WriteLine(value);

                if (s_Instance != null && saveLog)
                    s_Instance._debugStreamWriter.WriteLine(value);
            }
        }

        public static void WriteLine(string format, object arg0, bool saveLog = true)
        {
            lock (_locker)
            {
                Console.WriteLine(format, arg0);

                if (s_Instance != null && saveLog)
                    s_Instance._debugStreamWriter.WriteLine(format, arg0);
            }
        }

        public static void WriteLine(string format, object arg0, object arg1, bool saveLog = true)
        {
            lock (_locker)
            {
                Console.WriteLine(format, arg0, arg1);

                if (s_Instance != null && saveLog)
                    s_Instance._debugStreamWriter.WriteLine(format, arg0, arg1);
            }
        }

        public static void WriteLine(bool saveLog, string format, params object[] arg)
        {
            lock (_locker)
            {
                Console.WriteLine(format, arg);

                if (s_Instance != null && saveLog)
                    s_Instance._debugStreamWriter.WriteLine(format, arg);
            }
        }

        public static void WriteLine(bool value, bool saveLog = true)
        {
            lock (_locker)
            {
                Console.WriteLine(value);

                if (s_Instance != null && saveLog)
                    s_Instance._debugStreamWriter.WriteLine(value);
            }
        }

        public static void WriteLine(char value, bool saveLog = true)
        {
            lock (_locker)
                Console.WriteLine(value);
        }

        public static void WriteLine(char[] buffer, bool saveLog = true)
        {
            lock (_locker)
                Console.WriteLine(buffer);
        }

        public static void WriteLine(decimal value, bool saveLog = true)
        {
            lock (_locker)
                Console.WriteLine(value);
        }

        public static void WriteLine(double value, bool saveLog = true)
        {
            lock (_locker)
                Console.WriteLine(value);
        }

        public static void WriteLine(float value, bool saveLog = true)
        {
            lock (_locker)
                Console.WriteLine(value);
        }

        public static void WriteLine(int value, bool saveLog = true)
        {
            lock (_locker)
                Console.WriteLine(value);
        }

        public static void WriteLine(long value, bool saveLog = true)
        {
            lock (_locker)
                Console.WriteLine(value);
        }

        public static void WriteLine(object value, bool saveLog = true)
        {
            lock (_locker)
                Console.WriteLine(value);
        }

        public static void WriteLine(uint value, bool saveLog = true)
        {
            lock (_locker)
                Console.WriteLine(value);
        }

        public static void WriteLine(ulong value, bool saveLog = true)
        {
            lock (_locker)
                Console.WriteLine(value);
        }


        public static void Write(string value, bool saveLog = true)
        {
            lock (_locker)
                Console.Write(value);
        }

        public static void Write(string format, object arg0, bool saveLog = true)
        {
            lock (_locker)
                Console.Write(format, arg0);
        }

        public static void Write(string format, object arg0, object arg1, bool saveLog = true)
        {
            lock (_locker)
                Console.Write(format, arg0, arg1);
        }

        public static void Write(string format, bool saveLog = true, params object[] arg)
        {
            lock (_locker)
                Console.Write(format, arg);
        }

        public static void Write(bool value, bool saveLog = true)
        {
            lock (_locker)
                Console.Write(value);
        }

        public static void Write(char value, bool saveLog = true)
        {
            lock (_locker)
                Console.Write(value);
        }

        public static void Write(char[] buffer, bool saveLog = true)
        {
            lock (_locker)
                Console.Write(buffer);
        }

        public static void Write(decimal value, bool saveLog = true)
        {
            lock (_locker)
                Console.Write(value);
        }

        public static void Write(double value, bool saveLog = true)
        {
            lock (_locker)
                Console.Write(value);
        }

        public static void Write(float value, bool saveLog = true)
        {
            lock (_locker)
                Console.Write(value);
        }

        public static void Write(int value, bool saveLog = true)
        {
            lock (_locker)
                Console.Write(value);
        }

        public static void Write(long value, bool saveLog = true)
        {
            lock (_locker)
                Console.Write(value);
        }

        public static void Write(object value, bool saveLog = true)
        {
            lock (_locker)
                Console.Write(value);
        }

        public static void Write(uint value, bool saveLog = true)
        {
            lock (_locker)
                Console.Write(value);
        }

        public static void Write(ulong value, bool saveLog = true)
        {
            lock (_locker)
                Console.Write(value);
        }

        

        private ThreadSafeLogger(HostType hostType, string ID)
        {
            _debugFileStream = new FileStream(DateTime.Now.ToString(hostType == HostType.Client ? "[" + ID + "]" : "[Server]" + "HH-ss") + ".txt", FileMode.OpenOrCreate);
            _debugStreamWriter = new StreamWriter(_debugFileStream);
        }

        public static ThreadSafeLogger GetInstance(HostType hostType, string ID)
        {
            if (s_Instance == null)
                s_Instance = new ThreadSafeLogger(hostType, ID);
            return s_Instance;
        }

        public void WriteNet(NetLogLevel level, string str, params object[] args)
        {
            lock (_locker)
            {
                Console.WriteLine(str, args);
                _debugStreamWriter.WriteLine(str, args);
            }
        }
    }
}
