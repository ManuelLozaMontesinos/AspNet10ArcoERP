using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace AspNet10ArcoERP.Services
{
    public class PasswordService : IPasswordService
    {
        private static readonly byte[] Salt = new byte[] { 0x26, 0x19, 0x81, 0x4E, 0x7F, 0x6D, 0x95, 0xFF, 0x26, 0x75, 0x64, 0x05, 0x26 };

        public bool VerifyPassword(string inputPassword, string dbPasswordBase64, byte[] dbPublicKey)
        {
            if (string.IsNullOrEmpty(inputPassword) || string.IsNullOrEmpty(dbPasswordBase64) || dbPublicKey == null)
                return false;

            try
            {
                string pubKeyStr = Encoding.UTF8.GetString(dbPublicKey);

                byte[] derived = Rfc2898DeriveBytes.Pbkdf2(pubKeyStr, Salt, 1000, HashAlgorithmName.SHA1, 48);
                byte[] key = derived[..32];
                byte[] iv = derived[32..48];

                using var aes = Aes.Create();
                aes.Key = key;
                aes.IV = iv;

                using var decryptor = aes.CreateDecryptor();
                byte[] cipherBytes = Convert.FromBase64String(dbPasswordBase64);

                using var ms = new MemoryStream();
                using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Write))
                {
                    cs.Write(cipherBytes, 0, cipherBytes.Length);
                    cs.FlushFinalBlock();
                }

                string decrypted = Encoding.UTF8.GetString(ms.ToArray());
                return string.Equals(decrypted, inputPassword, StringComparison.Ordinal);
            }
            catch
            {
                // Fallback for simple or legacy comparison if decryption fails
                return string.Equals(inputPassword, dbPasswordBase64, StringComparison.Ordinal);
            }
        }

        public string EncryptPassword(string plainPassword, out byte[] publicKey)
        {
            string pubKeyStr = Guid.NewGuid().ToString("N");
            publicKey = Encoding.UTF8.GetBytes(pubKeyStr);

            byte[] derived = Rfc2898DeriveBytes.Pbkdf2(pubKeyStr, Salt, 1000, HashAlgorithmName.SHA1, 48);
            byte[] key = derived[..32];
            byte[] iv = derived[32..48];

            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;

            using var encryptor = aes.CreateEncryptor();
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainPassword);

            using var ms = new MemoryStream();
            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            {
                cs.Write(plainBytes, 0, plainBytes.Length);
                cs.FlushFinalBlock();
            }

            return Convert.ToBase64String(ms.ToArray());
        }
    }
}
