using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using System.IO;
using System.Linq;

namespace Encryptor.Lib
{
    public class EncryptorEngine
    {
        public byte[] EncryptContent(byte[] content, byte[][] keys)
        {
            RandomNumberGenerator rnd = RandomNumberGenerator.Create();
            byte[] iv = new byte[12];
            byte[] tag = new byte[16];

            for (int i = 0; i < keys.GetLength(0); i++)
            {
                rnd.GetNonZeroBytes(iv);
                byte[] cipher = new byte[content.Length + tag.Length + iv.Length];

                AesGcm encr = new AesGcm(keys[i]);
                encr.Encrypt(iv, content, cipher, tag);

                Array.Copy(iv, 0, cipher, content.Length, iv.Length);
                Array.Copy(tag, 0, cipher, content.Length + iv.Length, tag.Length);

                content = cipher;
            }

            return content;
        }

        public byte[] DecryptContent(byte[] cipher, byte[][] keys)
        {
            byte[] iv = new byte[12];
            byte[] tag = new byte[16];

            for (int i = 0; i < keys.GetLength(0); i++)
            {
                byte[] content = new byte[cipher.Length - tag.Length - iv.Length];
                Array.Copy(cipher, content.Length, iv, 0, iv.Length);
                Array.Copy(cipher, content.Length + iv.Length, tag, 0, tag.Length);

                AesGcm encr = new AesGcm(keys[i]);
                encr.Decrypt(iv, cipher.Take(content.Length).ToArray(), content, tag);
                
                cipher = content;
            }

            return cipher;
        }

        public byte[] TransformKey(byte[] key, byte[] password, bool isEncryption)
        {
            SHA512 fHA = SHA512.Create();
            MD5 sHA = MD5.Create();

            BinaryWriter hashInput = new BinaryWriter(new MemoryStream());
            BinaryWriter backbone = new BinaryWriter(new MemoryStream());

            hashInput.Write(password);
            hashInput.Flush();

            for (int i = 0; i < 2048; i++)
            {
                if (i % 2 == 0)
                {
                    hashInput.BaseStream.Seek(0, SeekOrigin.Begin);
                    backbone.Write(fHA.ComputeHash(hashInput.BaseStream));
                    hashInput.BaseStream.Seek(0, SeekOrigin.Begin);
                    backbone.Write(sHA.ComputeHash(hashInput.BaseStream));
                }
                else
                {
                    hashInput.BaseStream.Seek(0, SeekOrigin.Begin);
                    backbone.Write(sHA.ComputeHash(hashInput.BaseStream));
                    hashInput.BaseStream.Seek(0, SeekOrigin.Begin);
                    backbone.Write(fHA.ComputeHash(hashInput.BaseStream));
                }

                backbone.Flush();
                hashInput.BaseStream.SetLength(0);
                {
                    var tmp = hashInput;
                    hashInput = backbone;
                    backbone = tmp;
                }
            }

            hashInput.BaseStream.Seek(0, SeekOrigin.Begin);
            byte[] hashedPassword = new byte[32];

            hashInput.BaseStream.Read(hashedPassword, 0, hashedPassword.Length);
            {
                byte[] tempPart = new byte[32];
                hashInput.BaseStream.Read(tempPart, 0, tempPart.Length);

                for (int i = 0; i < tempPart.Length; i++)
                    hashedPassword[i] = (byte)(hashedPassword[i] ^ tempPart[i]);
            }

            Aes eng = Aes.Create();
            eng.Mode = CipherMode.CBC;
            eng.Key = hashedPassword;
            eng.GenerateIV();

            ICryptoTransform aesTr;
            if (isEncryption)
                aesTr = eng.CreateEncryptor();
            else
                aesTr = eng.CreateDecryptor();

            return aesTr.TransformFinalBlock(key, 0, key.Length);
        }

        public byte[] GenerateKey(int size)
        {
            byte[] key = new byte[size];
            RandomNumberGenerator rnd = RandomNumberGenerator.Create();
            rnd.GetNonZeroBytes(key);

            return key;
        }
    }
}
