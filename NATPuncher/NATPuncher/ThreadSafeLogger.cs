// ===============================
// @AUTHOR      : 윤정도
// @CREATE DATE : 2020-05-30 오후 5:37:40   
// @PURPOSE     : 쓰레드 세이프 로거
// ===============================

using System;
using System.Diagnostics;

namespace NATPuncher
{
    class ThreadSafeLogger
    {
        private static object _locker = new object();

        public static void WriteLine()
        {
            lock (_locker)
                Console.WriteLine();
        }

        public static void WriteLine(string value)
        {
            lock (_locker)
                Console.WriteLine(value);
        }

        public static void WriteLine(string format, object arg0)
        {
            lock (_locker)
                Console.WriteLine(format, arg0);
        }

        public static void WriteLine(string format, object arg0, object arg1)
        {
            lock (_locker)
                Console.WriteLine(format, arg0, arg1);
        }

        public static void WriteLine(string format, params object[] arg)
        {
            lock (_locker)
                Console.WriteLine(format, arg);
        }

        public static void WriteLine(bool value)
        {
            lock (_locker)
                Console.WriteLine(value);
        }

        public static void WriteLine(char value)
        {
            lock (_locker)
                Console.WriteLine(value);
        }

        public static void WriteLine(char[] buffer)
        {
            lock (_locker)
                Console.WriteLine(buffer);
        }

        public static void WriteLine(decimal value)
        {
            lock (_locker)
                Console.WriteLine(value);
        }

        public static void WriteLine(double value)
        {
            lock (_locker)
                Console.WriteLine(value);
        }

        public static void WriteLine(float value)
        {
            lock (_locker)
                Console.WriteLine(value);
        }

        public static void WriteLine(int value)
        {
            lock (_locker)
                Console.WriteLine(value);
        }

        public static void WriteLine(long value)
        {
            lock (_locker)
                Console.WriteLine(value);
        }

        public static void WriteLine(object value)
        {
            lock (_locker)
                Console.WriteLine(value);
        }

        public static void WriteLine(uint value)
        {
            lock (_locker)
                Console.WriteLine(value);
        }

        public static void WriteLine(ulong value)
        {
            lock (_locker)
                Console.WriteLine(value);
        }


        public static void Write(string value)
        {
            lock (_locker)
                Console.Write(value);
        }

        public static void Write(string format, object arg0)
        {
            lock (_locker)
                Console.Write(format, arg0);
        }

        public static void Write(string format, object arg0, object arg1)
        {
            lock (_locker)
                Console.Write(format, arg0, arg1);
        }

        public static void Write(string format, params object[] arg)
        {
            lock (_locker)
                Console.Write(format, arg);
        }

        public static void Write(bool value)
        {
            lock (_locker)
                Console.Write(value);
        }

        public static void Write(char value)
        {
            lock (_locker)
                Console.Write(value);
        }

        public static void Write(char[] buffer)
        {
            lock (_locker)
                Console.Write(buffer);
        }

        public static void Write(decimal value)
        {
            lock (_locker)
                Console.Write(value);
        }

        public static void Write(double value)
        {
            lock (_locker)
                Console.Write(value);
        }

        public static void Write(float value)
        {
            lock (_locker)
                Console.Write(value);
        }

        public static void Write(int value)
        {
            lock (_locker)
                Console.Write(value);
        }

        public static void Write(long value)
        {
            lock (_locker)
                Console.Write(value);
        }

        public static void Write(object value)
        {
            lock (_locker)
                Console.Write(value);
        }

        public static void Write(uint value)
        {
            lock (_locker)
                Console.Write(value);
        }

        public static void Write(ulong value)
        {
            lock (_locker)
                Console.Write(value);
        }
    }
}
