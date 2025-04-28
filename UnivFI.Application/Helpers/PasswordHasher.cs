using System.Security.Cryptography;
using Microsoft.Extensions.Logging;

namespace UnivFI.Application.Helpers
{
    /// <summary>
    /// 비밀번호 해싱 및 검증을 위한 유틸리티 클래스
    /// PBKDF2(Password-Based Key Derivation Function 2) 알고리즘을 사용하여 
    /// 안전한 비밀번호 해싱을 구현합니다.
    /// SHA-512 해시 알고리즘을 사용하여 더 강력한 보안을 제공합니다.
    /// </summary>
    public static class PasswordHasher
    {
        // PBKDF2 알고리즘 설정값
        private const int SaltSize = 16;     // Salt 크기: 16바이트 (128비트)
        private const int KeySize = 64;      // 해시 출력 크기: 64바이트 (512비트)
        private const int Iterations = 100000; // 반복 횟수: 해시 함수를 10만번 반복 적용

        private static ILogger? _logger;

        public static void SetLogger(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 암호학적으로 안전한 난수를 사용하여 Salt를 생성합니다.
        /// Salt는 각 사용자마다 고유한 값을 가지며, 동일한 비밀번호도 
        /// 다른 해시값을 갖도록 합니다.
        /// </summary>
        /// <returns>Base64로 인코딩된 Salt 문자열</returns>
        public static string GenerateSalt()
        {
            // RandomNumberGenerator를 사용하여 암호학적으로 안전한 난수 생성
            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var saltBase64 = Convert.ToBase64String(salt);

            _logger?.LogDebug("[PasswordHasher] 생성된 Salt: {Salt}", saltBase64);

            return saltBase64;
        }

        /// <summary>
        /// 비밀번호와 Salt를 사용하여 해시를 생성합니다.
        /// PBKDF2 알고리즘과 SHA-512를 사용하여 비밀번호를 해싱하며, 
        /// 이는 무차별 대입 공격과 레인보우 테이블 공격에 대한 저항성을 제공합니다.
        /// </summary>
        /// <param name="password">해싱할 원본 비밀번호</param>
        /// <param name="salt">Base64로 인코딩된 Salt 문자열</param>
        /// <returns>Base64로 인코딩된 해시 문자열</returns>
        public static string HashPassword(string password, string salt)
        {
            // Base64 문자열을 바이트 배열로 디코딩
            var saltBytes = Convert.FromBase64String(salt);

            // PBKDF2 알고리즘을 사용하여 비밀번호 해싱
            // - password: 해싱할 원본 비밀번호
            // - saltBytes: Salt 바이트 배열
            // - Iterations: 해시 함수 반복 횟수
            // - HashAlgorithmName.SHA512: SHA-512 해시 알고리즘 사용
            // - KeySize: 출력할 해시의 크기 (SHA-512는 64바이트 출력)
            var hash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                saltBytes,
                Iterations,
                HashAlgorithmName.SHA512,
                KeySize);

            var hashBase64 = Convert.ToBase64String(hash);

            _logger?.LogDebug("[PasswordHasher] 해시 생성 결과 - Salt: {Salt}, Hash: {Hash}", salt, hashBase64);

            return hashBase64;
        }

        /// <summary>
        /// 사용자가 입력한 비밀번호가 저장된 해시와 일치하는지 검증합니다.
        /// 타이밍 공격을 방지하기 위해 고정 시간 비교를 사용합니다.
        /// </summary>
        /// <param name="password">검증할 비밀번호</param>
        /// <param name="passwordHash">저장된 비밀번호 해시</param>
        /// <param name="salt">저장된 Salt</param>
        /// <returns>비밀번호 일치 여부</returns>
        public static bool VerifyPassword(string password, string passwordHash, string salt)
        {
            // 입력된 비밀번호를 동일한 방식으로 해싱
            var hash = HashPassword(password, salt);

            // CryptographicOperations.FixedTimeEquals를 사용하여
            // 타이밍 공격에 대한 방어가 가능한 문자열 비교 수행
            var isValid = CryptographicOperations.FixedTimeEquals(
                Convert.FromBase64String(passwordHash),
                Convert.FromBase64String(hash));

            _logger?.LogDebug(
                "[PasswordHasher] 비밀번호 검증 - Salt: {Salt}, 저장된 해시: {StoredHash}, 생성된 해시: {GeneratedHash}, 일치여부: {IsValid}",
                salt,
                passwordHash,
                hash,
                isValid);

            return isValid;
        }
    }
}