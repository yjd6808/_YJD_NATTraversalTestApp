// ===============================
// @AUTHOR      : 윤정도
// @CREATE DATE : 2020-07-15 오후 11:18:12   
// @PURPOSE     : 확장 메소드 추가해줌
// ===============================


using LiteNetLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NATShared
{
    public static class Extrensions
    {
        public static void ForEach<T>(this IEnumerable<T> enumeration, Action<T> action)
        {
            foreach (T item in enumeration)
            {
                action(item);
            }
        }

    }
}
