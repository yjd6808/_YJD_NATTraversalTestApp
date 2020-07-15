// ===============================
// @AUTHOR      : 윤정도
// @CREATE DATE : 2020-05-30 오후 8:54:27   
// @PURPOSE     : 쓰레드 세이프 Boolean 만들어봄
// ===============================


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NATClient
{
    class InterlockedBoolean
    {
        private long _value;
        public bool Value
        {
            get
            {
                return Convert.ToBoolean(Interlocked.Read(ref _value)); 
            }
            set
            {
                Interlocked.Exchange(ref _value, Convert.ToInt32(value));
            }
        }
    }
}
