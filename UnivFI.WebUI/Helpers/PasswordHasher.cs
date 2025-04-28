using System.Security.Cryptography;

namespace UnivFI.WebUI.Helpers
{
    public static class PasswordHasher
    {
        private const int SaltSizeInBytes = 16; // 솔트 크기 (바이트 단위)
        private const int KeySizeInBytes = 32; // 키 크기 (바이트 단위)
        private const int IterationsCount = 100000; // 반복 횟수
        private const char ElementDelimiter = ';'; // 요소 구분자

        public static string HashPassword(string userPassword)
        {
            var saltBytes = RandomNumberGenerator.GetBytes(SaltSizeInBytes); // 무작위 솔트 생성
            var hashBytes = Rfc2898DeriveBytes.Pbkdf2(
                userPassword,
                saltBytes,
                IterationsCount,
                HashAlgorithmName.SHA256,
                KeySizeInBytes); // 비밀번호 해싱

            return string.Join(ElementDelimiter,
                Convert.ToBase64String(saltBytes), // 솔트를 Base64 문자열로 변환
                Convert.ToBase64String(hashBytes)); // 해시된 비밀번호를 Base64 문자열로 변환
        }

        public static bool VerifyPassword(string passwordHash, string userPassword)
        {
            var elements = passwordHash.Split(ElementDelimiter); // 해싱된 비밀번호를 요소로 분리
            if (elements.Length != 2)
                return false;

            var saltBytes = Convert.FromBase64String(elements[0]); // Base64 문자열을 바이트 배열로 변환하여 솔트 추출
            var hashBytes = Convert.FromBase64String(elements[1]); // Base64 문자열을 바이트 배열로 변환하여 해시된 비밀번호 추출

            var hashToCompare = Rfc2898DeriveBytes.Pbkdf2(
                userPassword,
                saltBytes,
                IterationsCount,
                HashAlgorithmName.SHA256,
                KeySizeInBytes); // 비밀번호를 해싱하여 비교 대상 해시 생성

            return CryptographicOperations.FixedTimeEquals(hashBytes, hashToCompare); // 안전한 방식으로 해시 값 비교
        }




    }
}