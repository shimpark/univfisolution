using Microsoft.Data.SqlClient; // Updated namespace
using Microsoft.Extensions.Configuration; // Add this using directive
using System.Data;
using System.Security.Cryptography;
using UnivFI.Infrastructure.Security;

namespace UnivFI.Infrastructure.Repositories
{
    public interface IConnectionFactory
    {
        IDbConnection CreateConnection();
    }

    public class ConnectionFactory : IConnectionFactory
    {
        private readonly string _connectionString;
        private readonly IEncryptionService _encryptionService;

        public ConnectionFactory(IConfiguration configuration, IEncryptionService encryptionService)
        {
            _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));

            var dockerServer = configuration["ConnectionStrings:DockerServer"];
            var encryptedServer = configuration["ConnectionStrings:EncryptedServer"];
            var encryptedDatabase = configuration["ConnectionStrings:EncryptedDatabase"];
            var encryptedUserId = configuration["ConnectionStrings:EncryptedUserId"];
            var encryptedPassword = configuration["ConnectionStrings:EncryptedPassword"];
            var integratedSecurity = configuration["ConnectionStrings:IntegratedSecurity"];

            try
            {
                var server = dockerServer == "" ? _encryptionService.Decrypt(encryptedServer) : dockerServer;

                if (string.IsNullOrEmpty(server) || string.IsNullOrEmpty(encryptedDatabase))
                {
                    throw new ArgumentException("Invalid connection configuration. EncryptedServer and EncryptedDatabase are required.");
                }

                // 암호화된 값들을 복호화
                // var server = _encryptionService.Decrypt(encryptedServer);
                var database = _encryptionService.Decrypt(encryptedDatabase);

                // SqlConnectionStringBuilder를 사용하여 안전하게 연결 문자열 생성
                var builder = new SqlConnectionStringBuilder
                {
                    DataSource = server,
                    InitialCatalog = database
                };

                // 통합 인증 또는 사용자 이름/비밀번호 인증 선택
                if (integratedSecurity != null && bool.TryParse(integratedSecurity, out bool useIntegratedSecurity) && useIntegratedSecurity)
                {
                    builder.IntegratedSecurity = true;
                }
                else if (!string.IsNullOrEmpty(encryptedUserId) && !string.IsNullOrEmpty(encryptedPassword))
                {
                    try
                    {
                        // 암호화된 자격 증명 복호화
                        builder.UserID = _encryptionService.Decrypt(encryptedUserId);
                        builder.Password = _encryptionService.Decrypt(encryptedPassword);
                    }
                    catch (Exception ex)
                    {
                        throw new CryptographicException("DB 자격 증명 복호화 중 오류가 발생했습니다.", ex);
                    }
                }
                else
                {
                    throw new ArgumentException("Either IntegratedSecurity must be true or EncryptedUserId and EncryptedPassword must be provided.");
                }

                // 추가 연결 속성 설정
                builder.TrustServerCertificate = true;
                builder.ConnectTimeout = 30;

                //_connectionString = builder.ConnectionString;
                _connectionString =
                    "Data Source=host.docker.internal,1433;Initial Catalog=db_lucky;User ID=shimpark;Password=eogkrsodlf1!;Connect Timeout=30;Trust Server Certificate=True";
            }
            catch (Exception ex)
            {
                throw new CryptographicException("연결 문자열 구성 중 오류가 발생했습니다.", ex);
            }
        }

        public IDbConnection CreateConnection()
        {
            return new SqlConnection(_connectionString);
        }
    }
}