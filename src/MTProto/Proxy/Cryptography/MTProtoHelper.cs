using System;
using System.Security.Cryptography;

namespace MTProto.Proxy.Cryptography
{
    public static class MTProtoHelper
    {
        public static Random Random = new Random();
        public static byte[] ComputeSHA256(byte[] data)
        {
            using (var sha256 = new SHA256Managed())
            {
                return sha256.ComputeHash(data);
            }
        }

        public static ICryptoTransform CreateEncryptorFromAes(byte[] key)
        {
            using (var aesManaged = new AesManaged())
            {
                aesManaged.Key = key;
                aesManaged.Mode = CipherMode.ECB;
                aesManaged.Padding = PaddingMode.None;
                return aesManaged.CreateEncryptor();
            }
        }
        public static void GenerateNewBuffer(MTProtoProtocolType protocolType, byte[] buffer)
        {
            while (true)
            {
                Random.NextBytes(buffer);

                var val = (buffer[3] << 24) | (buffer[2] << 16) | (buffer[1] << 8) | (buffer[0]);
                var val2 = (buffer[7] << 24) | (buffer[6] << 16) | (buffer[5] << 8) | (buffer[4]);
                if (buffer[0] != 0xef
                    && val != 0x44414548
                    && val != 0x54534f50
                    && val != 0x20544547
                    && val != 0x4954504f
                    && val2 != 0x00000000)
                {
                    switch (protocolType)
                    {
                        case MTProtoProtocolType.AbridgedObfuscated2:
                            buffer[56] = buffer[57] = buffer[58] = buffer[59] = 0xef;
                            return;
                        case MTProtoProtocolType.IntermediateObfuscated2:
                            buffer[56] = buffer[57] = buffer[58] = buffer[59] = 0xee;
                            return;
                        default:
                            return;
                    }
                }
            }
        }
    }
}
