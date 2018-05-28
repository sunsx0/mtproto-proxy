using System;
using System.Linq;
using System.Security.Cryptography;

namespace MTProto.Proxy.Cryptography
{
    public class MTProtoCryptor
    {
        public ICryptoTransform CryptoTransform { get; }
        public byte[] Key { get; } = new byte[32];
        public byte[] Iv { get; } = new byte[16];
        public byte[] Xor { get; } = new byte[16];

        private int Number;
        public MTProtoCryptor(byte[] buffer, bool reversed, byte[] secret = null)
        {
            var keys = new byte[48];
            Buffer.BlockCopy(buffer, 8, keys, 0, 48);
            if (reversed)
            {
                Array.Reverse(keys);
            }

            Buffer.BlockCopy(keys, 0, Key, 0, 32);
            Buffer.BlockCopy(keys, 32, Iv, 0, 16);

            if (secret != null)
            {
                Key = MTProtoHelper.ComputeSHA256(Key.Concat(secret).ToArray());
            }
            CryptoTransform = MTProtoHelper.CreateEncryptorFromAes(Key);
        }

        public void Transform(byte[] data, int offset, int length, byte[] output, int outputOffset)
        {
            for (var i = 0; i < length; i++)
            {
                if (Number == 0)
                {
                    CryptoTransform.TransformBlock(Iv, 0, Iv.Length, Xor, 0);
                    for (var j = 15; j >= 0; j--)
                    {
                        Iv[j]++;
                        if (Iv[j] != 0)
                        {
                            break;
                        }
                    }
                }
                output[i + outputOffset] = (byte)(data[i + offset] ^ Xor[Number]);
                Number = (Number + 1) & 0xF;
            }
        }
    }
}
