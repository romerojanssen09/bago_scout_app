using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;

namespace BagoScoutApp.Services
{
    public class AuthStorageService
    {
        private const string FileName = "auth.txt";
        private const string KeyPref = "AuthCryptoKey";
        private const string IvPref = "AuthCryptoIv";

        private static readonly object FileLock = new object();

        public static string FilePath => Path.Combine(FileSystem.Current.AppDataDirectory, FileName);

        private static async Task<(byte[] Key, byte[] Iv)> GetOrCreateCryptoParamsAsync()
        {
            var keyBase64 = await SecureStorage.GetAsync(KeyPref);
            var ivBase64 = await SecureStorage.GetAsync(IvPref);

            if (string.IsNullOrEmpty(keyBase64) || string.IsNullOrEmpty(ivBase64))
            {
                using var aes = Aes.Create();
                aes.KeySize = 256;
                aes.BlockSize = 128;
                aes.GenerateKey();
                aes.GenerateIV();

                keyBase64 = Convert.ToBase64String(aes.Key);
                ivBase64 = Convert.ToBase64String(aes.IV);

                await SecureStorage.SetAsync(KeyPref, keyBase64);
                await SecureStorage.SetAsync(IvPref, ivBase64);

                return (aes.Key, aes.IV);
            }

            return (Convert.FromBase64String(keyBase64), Convert.FromBase64String(ivBase64));
        }

        public static async Task SaveCredentialsAsync(string email, string password, string token, int userId, string userType)
        {
            try
            {
                var credentials = new CredentialsPayload
                {
                    Email = email,
                    Password = password,
                    Token = token,
                    UserId = userId,
                    UserType = userType
                };

                var json = JsonSerializer.Serialize(credentials);
                var plainBytes = Encoding.UTF8.GetBytes(json);

                var (key, iv) = await GetOrCreateCryptoParamsAsync();

                byte[] encryptedBytes;
                using (var aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.IV = iv;
                    using var encryptor = aes.CreateEncryptor();
                    encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
                }

                var base64Encrypted = Convert.ToBase64String(encryptedBytes);

                lock (FileLock)
                {
                    File.WriteAllText(FilePath, base64Encrypted);
                }
                
                System.Diagnostics.Debug.WriteLine($"Credentials successfully encrypted and saved to {FilePath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save credentials: {ex.Message}");
            }
        }

        public static async Task<CredentialsPayload?> LoadCredentialsAsync()
        {
            try
            {
                if (!File.Exists(FilePath))
                {
                    return null;
                }

                string base64Encrypted;
                lock (FileLock)
                {
                    base64Encrypted = File.ReadAllText(FilePath);
                }

                if (string.IsNullOrEmpty(base64Encrypted))
                {
                    return null;
                }

                var encryptedBytes = Convert.FromBase64String(base64Encrypted);
                var (key, iv) = await GetOrCreateCryptoParamsAsync();

                byte[] plainBytes;
                using (var aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.IV = iv;
                    using var decryptor = aes.CreateDecryptor();
                    plainBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
                }

                var json = Encoding.UTF8.GetString(plainBytes);
                return JsonSerializer.Deserialize<CredentialsPayload>(json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load/decrypt credentials: {ex.Message}");
                return null;
            }
        }

        public static void ClearCredentials()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    lock (FileLock)
                    {
                        File.Delete(FilePath);
                    }
                }
                SecureStorage.Remove(KeyPref);
                SecureStorage.Remove(IvPref);
                System.Diagnostics.Debug.WriteLine("Encrypted credentials file and keys successfully cleared.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to clear credentials: {ex.Message}");
            }
        }
    }

    public class CredentialsPayload
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string UserType { get; set; } = string.Empty;
    }
}
