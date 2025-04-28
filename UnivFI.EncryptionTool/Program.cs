using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace UnivFI.EncryptionTool
{
    public class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("데이터베이스 자격 증명 암/복호화 도구");
            Console.WriteLine("===================================");

            if (args.Length < 1)
            {
                Console.WriteLine("사용법: dotnet run <암호화_키>");
                Console.WriteLine("암호화 키가 제공되지 않았습니다. 직접 입력해주세요:");
                var encryptionKey = Console.ReadLine();

                if (string.IsNullOrEmpty(encryptionKey))
                {
                    Console.WriteLine("암호화 키가 필요합니다. 프로그램을 종료합니다.");
                    return;
                }

                ProcessEncryptionDecryption(encryptionKey);
            }
            else
            {
                string encryptionKey = args[0];
                ProcessEncryptionDecryption(encryptionKey);
            }
        }

        static void ProcessEncryptionDecryption(string encryptionKey)
        {
            Console.WriteLine("\n작업을 선택하세요:");
            Console.WriteLine("1. 암호화");
            Console.WriteLine("2. 복호화");
            Console.Write("선택 (1 또는 2): ");

            var choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    PromptAndEncrypt(encryptionKey);
                    break;
                case "2":
                    PromptAndDecrypt(encryptionKey);
                    break;
                default:
                    Console.WriteLine("잘못된 선택입니다.");
                    break;
            }
        }

        static void PromptAndEncrypt(string encryptionKey)
        {
            Console.WriteLine("\n암호화할 서버 주소를 입력하세요:");
            string server = Console.ReadLine();

            Console.WriteLine("암호화할 데이터베이스명을 입력하세요:");
            string database = Console.ReadLine();

            Console.WriteLine("암호화할 사용자 ID를 입력하세요:");
            string userId = Console.ReadLine();

            Console.WriteLine("암호화할 비밀번호를 입력하세요:");
            string password = Console.ReadLine();

            if (string.IsNullOrEmpty(server) || string.IsNullOrEmpty(database) ||
                string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(password))
            {
                Console.WriteLine("모든 항목은 필수 입력 사항입니다.");
                return;
            }

            try
            {
                // 키 생성 (32바이트 = 256비트)
                byte[] key;
                byte[] iv;
                using (var deriveBytes = new Rfc2898DeriveBytes(encryptionKey, Encoding.UTF8.GetBytes("UnivFISalt"), 10000))
                {
                    key = deriveBytes.GetBytes(32); // AES-256
                    iv = deriveBytes.GetBytes(16);  // AES 블록 크기 (16바이트)
                }

                string encryptedServer = Encrypt(server, key, iv);
                string encryptedDatabase = Encrypt(database, key, iv);
                string encryptedUserId = Encrypt(userId, key, iv);
                string encryptedPassword = Encrypt(password, key, iv);

                Console.WriteLine("\n암호화 결과:");
                Console.WriteLine("=============");
                Console.WriteLine($"암호화된 서버 주소: {encryptedServer}");
                Console.WriteLine($"암호화된 데이터베이스명: {encryptedDatabase}");
                Console.WriteLine($"암호화된 사용자 ID: {encryptedUserId}");
                Console.WriteLine($"암호화된 비밀번호: {encryptedPassword}");

                Console.WriteLine("\nappsettings.json에 다음 구성을 추가하세요:");
                Console.WriteLine("\"ConnectionStrings\": {");
                Console.WriteLine($"  \"EncryptedServer\": \"{encryptedServer}\",");
                Console.WriteLine($"  \"EncryptedDatabase\": \"{encryptedDatabase}\",");
                Console.WriteLine("  \"IntegratedSecurity\": \"false\",");
                Console.WriteLine($"  \"EncryptedUserId\": \"{encryptedUserId}\",");
                Console.WriteLine($"  \"EncryptedPassword\": \"{encryptedPassword}\"");
                Console.WriteLine("}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"오류 발생: {ex.Message}");
            }
        }

        static void PromptAndDecrypt(string encryptionKey)
        {
            Console.WriteLine("\n복호화할 서버 주소를 입력하세요:");
            string encryptedServer = Console.ReadLine();

            Console.WriteLine("복호화할 데이터베이스명을 입력하세요:");
            string encryptedDatabase = Console.ReadLine();

            Console.WriteLine("복호화할 사용자 ID를 입력하세요:");
            string encryptedUserId = Console.ReadLine();

            Console.WriteLine("복호화할 비밀번호를 입력하세요:");
            string encryptedPassword = Console.ReadLine();

            try
            {
                // 키 생성 (32바이트 = 256비트)
                byte[] key;
                byte[] iv;
                using (var deriveBytes = new Rfc2898DeriveBytes(encryptionKey, Encoding.UTF8.GetBytes("UnivFISalt"), 10000))
                {
                    key = deriveBytes.GetBytes(32); // AES-256
                    iv = deriveBytes.GetBytes(16);  // AES 블록 크기 (16바이트)
                }

                string server = Decrypt(encryptedServer, key, iv);
                string database = Decrypt(encryptedDatabase, key, iv);
                string userId = Decrypt(encryptedUserId, key, iv);
                string password = Decrypt(encryptedPassword, key, iv);

                Console.WriteLine("\n복호화 결과:");
                Console.WriteLine("=============");
                Console.WriteLine($"서버 주소: {server}");
                Console.WriteLine($"데이터베이스명: {database}");
                Console.WriteLine($"사용자 ID: {userId}");
                Console.WriteLine($"비밀번호: {password}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"오류 발생: {ex.Message}");
            }
        }

        static string Encrypt(string plainText, byte[] key, byte[] iv)
        {
            if (string.IsNullOrEmpty(plainText))
                return plainText;

            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;

                var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                using (var msEncrypt = new MemoryStream())
                {
                    using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    using (var swEncrypt = new StreamWriter(csEncrypt))
                    {
                        swEncrypt.Write(plainText);
                    }

                    return Convert.ToBase64String(msEncrypt.ToArray());
                }
            }
        }

        static string Decrypt(string cipherText, byte[] key, byte[] iv)
        {
            if (string.IsNullOrEmpty(cipherText))
                return cipherText;

            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;

                var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                try
                {
                    byte[] cipherBytes = Convert.FromBase64String(cipherText);

                    using (var msDecrypt = new MemoryStream(cipherBytes))
                    using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    using (var srDecrypt = new StreamReader(csDecrypt))
                    {
                        return srDecrypt.ReadToEnd();
                    }
                }
                catch
                {
                    throw new Exception("복호화 실패: 잘못된 암호화 텍스트이거나 키가 일치하지 않습니다.");
                }
            }
        }
    }
}
