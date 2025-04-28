using System;
using System.IO;

namespace UnivFI.Infrastructure.Security
{
    public static class EncryptionTool
    {
        public static void Main(string[] args)
        {
            // 콘솔에서 실행할 수 있는 암호화 도구
            if (args.Length < 2)
            {
                Console.WriteLine("사용법: EncryptionTool <암호화_키> <암호화할_텍스트>");
                return;
            }

            string encryptionKey = args[0];
            string textToEncrypt = args[1];

            try
            {
                var encryptionService = new EncryptionService(encryptionKey);
                string encryptedText = encryptionService.Encrypt(textToEncrypt);

                Console.WriteLine("암호화된 텍스트:");
                Console.WriteLine(encryptedText);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"오류 발생: {ex.Message}");
            }
        }
    }
}