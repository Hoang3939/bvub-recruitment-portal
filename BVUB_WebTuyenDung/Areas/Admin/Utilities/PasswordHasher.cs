using System;
using System.Security.Cryptography;
using System.Text;

namespace BVUB_WebTuyenDung.Areas.Admin.Utilities
{
    public static class PasswordHasher
    {
        public static string Hash(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
#if NET6_0_OR_GREATER
            return Convert.ToHexString(bytes);
#else
            return BitConverter.ToString(bytes).Replace("-", string.Empty);
#endif
        }

        public static bool Verify(string plain, string storedHash) =>
            string.Equals(Hash(plain), storedHash, StringComparison.OrdinalIgnoreCase);
    }
}
