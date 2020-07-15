// ===============================
// @AUTHOR      : 윤정도
// @CREATE DATE : 2020-05-31 오전 12:07:02   
// @PURPOSE     : 네트워크 패킷 직렬화, 역직렬화 확장 메소드
// ===============================

using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace NATShared
{
    public static class NetworkPacketConverter
    {
        public static byte[] ToByteArray(this INetworkPacket packet)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            MemoryStream Stream = new MemoryStream();

            formatter.Serialize(Stream, packet);
            return Stream.ToArray();
        }

        public static INetworkPacket ToP2PBase(this byte[] bytes)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            MemoryStream Stream = new MemoryStream();

            Stream.Write(bytes, 0, bytes.Length);
            Stream.Seek(0, SeekOrigin.Begin);

            INetworkPacket clientInfo = (INetworkPacket)formatter.Deserialize(Stream);

            return clientInfo;
        }


        
    }
}
