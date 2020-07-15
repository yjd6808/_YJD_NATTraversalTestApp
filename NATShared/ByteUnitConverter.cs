// ===============================
// @AUTHOR      : 윤정도
// @CREATE DATE : 2020-05-31 오전 12:15:59   
// @PURPOSE     : 바이트 단위 변경 확장 메소드
// ===============================


namespace NATShared
{
    public static class ByteUnitConverter
    {
        public static double BytesToKilobytes(this byte[] bytes)
        {
            return bytes.Length / 1024d;
        }

        public static double BytesToMegabytes(this byte[] bytes)
        {
            return BytesToKilobytes(bytes) / 1024d;
        }

        public static double BytesToGigabytes(this byte[] bytes)
        {
            return BytesToKilobytes(bytes) / 1048576D;
        }

        public static double BytesToKilobytes(this uint bytesSize)
        {
            return bytesSize / 1024d;
        }

        public static double BytesToMegabytes(this uint bytesSize)
        {
            return BytesToKilobytes(bytesSize) / 1024d;
        }

        public static double BytesToGigabytes(this uint bytesSize)
        {
            return BytesToKilobytes(bytesSize) / 1048576D;
        }
    }
}
