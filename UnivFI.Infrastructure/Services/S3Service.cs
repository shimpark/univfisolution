using Amazon;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;
using UnivFI.Application.Interfaces;

namespace UnivFI.Infrastructure.Services
{
    public class S3Service : IS3Service
    {
        private readonly IAmazonS3 _s3Client;
        private readonly ILogger<S3Service> _logger;

        public S3Service(IConfiguration configuration, ILogger<S3Service> logger)
        {
            _logger = logger;

            var region = Amazon.RegionEndpoint.APNortheast2;
            var s3Config = new AmazonS3Config
            {
                RegionEndpoint = region
            };

            // 프로필 위치와 이름 가져오기
            var profilesLocation = configuration["AWS:ProfilesLocation"];
            var profileName = configuration["AWS:ProfileName"];

            // AWS S3 클라이언트 초기화
            _s3Client = CreateS3Client(profilesLocation, profileName, s3Config);
        }

        private IAmazonS3 CreateS3Client(string profilesLocation, string profileName, AmazonS3Config s3Config)
        {
            // 개발자 PC에서 사용할 경우 (프로필 파일이 있는 경우)
            if (!string.IsNullOrEmpty(profilesLocation))
            {
                _logger.LogInformation("AWS 자격 증명 파일을 사용하여 S3 클라이언트 초기화: {ProfilesLocation}, 프로필: {ProfileName}",
                    profilesLocation, profileName);

                try
                {
                    var sharedFile = new SharedCredentialsFile(profilesLocation);
                    CredentialProfile basicProfile;
                    AWSCredentials awsCredentials;

                    if (sharedFile.TryGetProfile(profileName, out basicProfile) &&
                        AWSCredentialsFactory.TryGetAWSCredentials(basicProfile, sharedFile, out awsCredentials))
                    {
                        _logger.LogInformation("AWS 자격 증명 파일을 사용하여 S3 클라이언트 생성 성공");
                        // 개발서버 환경 사용 시
                        return new AmazonS3Client(awsCredentials, s3Config);
                    }
                    else
                    {
                        _logger.LogWarning("AWS 자격 증명 파일에서 프로필을 찾을 수 없음.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "AWS 자격 증명 파일 사용 중 오류 발생.");
                }
            }
            else
            {
                _logger.LogInformation("AWS 프로필 위치가 구성되지 않음.");
            }

            //EC2 운영서버 
            return new AmazonS3Client(s3Config);
        }

        /// <summary>
        /// S3에 파일을 업로드합니다.
        /// </summary>
        public async Task<string> UploadFileAsync(string bucketName, string key, string filePath)
        {
            try
            {
                _logger.LogInformation("S3에 파일 업로드 시작: {FilePath} -> {BucketName}/{Key}", filePath, bucketName, key);

                using var transferUtility = new TransferUtility(_s3Client);
                await transferUtility.UploadAsync(filePath, bucketName, key);

                var fileUrl = GetFileUrl(bucketName, key);
                _logger.LogInformation("S3 파일 업로드 완료: {Url}", fileUrl);
                return fileUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "S3 파일 업로드 중 오류 발생: {FilePath} -> {BucketName}/{Key}", filePath, bucketName, key);
                throw;
            }
        }

        /// <summary>
        /// 스트림을 S3에 업로드합니다.
        /// </summary>
        public async Task<string> UploadStreamAsync(string bucketName, string key, Stream stream)
        {
            try
            {
                _logger.LogInformation("S3에 스트림 업로드 시작: {BucketName}/{Key}", bucketName, key);

                var request = new PutObjectRequest
                {
                    BucketName = bucketName,
                    Key = key,
                    InputStream = stream
                };

                await _s3Client.PutObjectAsync(request);

                var fileUrl = GetFileUrl(bucketName, key);
                _logger.LogInformation("S3 스트림 업로드 완료: {Url}", fileUrl);
                return fileUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "S3 스트림 업로드 중 오류 발생: {BucketName}/{Key}", bucketName, key);
                throw;
            }
        }

        /// <summary>
        /// 바이트 배열을 S3에 업로드합니다.
        /// </summary>
        public async Task<string> UploadBytesAsync(string bucketName, string key, byte[] data)
        {
            try
            {
                _logger.LogInformation("S3에 바이트 데이터 업로드 시작: {BucketName}/{Key}, 크기: {Size} 바이트",
                    bucketName, key, data.Length);

                using var stream = new MemoryStream(data);
                var request = new PutObjectRequest
                {
                    BucketName = bucketName,
                    Key = key,
                    InputStream = stream
                };

                await _s3Client.PutObjectAsync(request);

                var fileUrl = GetFileUrl(bucketName, key);
                _logger.LogInformation("S3 바이트 데이터 업로드 완료: {Url}", fileUrl);
                return fileUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "S3 바이트 데이터 업로드 중 오류 발생: {BucketName}/{Key}", bucketName, key);
                throw;
            }
        }

        /// <summary>
        /// S3에서 파일을 스트림으로 다운로드합니다.
        /// </summary>
        public async Task<Stream> DownloadStreamAsync(string bucketName, string key)
        {
            try
            {
                _logger.LogInformation("S3 스트림 다운로드 시작: {BucketName}/{Key}", bucketName, key);

                var request = new GetObjectRequest
                {
                    BucketName = bucketName,
                    Key = key
                };

                var response = await _s3Client.GetObjectAsync(request);

                // 메모리 스트림으로 복사하여 반환
                var memoryStream = new MemoryStream();
                await response.ResponseStream.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                _logger.LogInformation("S3 스트림 다운로드 완료: {BucketName}/{Key}", bucketName, key);
                return memoryStream;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "S3 스트림 다운로드 중 오류 발생: {BucketName}/{Key}", bucketName, key);
                throw;
            }
        }

        /// <summary>
        /// S3에서 파일을 바이트 배열로 다운로드합니다.
        /// </summary>
        public async Task<byte[]> DownloadBytesAsync(string bucketName, string key)
        {
            try
            {
                _logger.LogInformation("S3 바이트 데이터 다운로드 시작: {BucketName}/{Key}", bucketName, key);

                using var stream = await DownloadStreamAsync(bucketName, key);
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);

                var data = memoryStream.ToArray();
                _logger.LogInformation("S3 바이트 다운로드 완료: {BucketName}/{Key}, 크기: {Size} 바이트",
                    bucketName, key, data.Length);

                return data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "S3 바이트 데이터 다운로드 중 오류 발생: {BucketName}/{Key}", bucketName, key);
                throw;
            }
        }

        /// <summary>
        /// S3 파일을 특정 로컬 폴더에 다운로드합니다.
        /// </summary>
        public async Task<string> DownloadToFileAsync(string bucketName, string key, string destinationPath)
        {
            try
            {
                _logger.LogInformation("S3 파일 다운로드 시작: {BucketName}/{Key} -> {DestinationPath}",
                    bucketName, key, destinationPath);

                // 대상 디렉토리 생성
                var directoryPath = Path.GetDirectoryName(destinationPath);
                if (!Directory.Exists(directoryPath) && !string.IsNullOrEmpty(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                using var transferUtility = new TransferUtility(_s3Client);
                await transferUtility.DownloadAsync(destinationPath, bucketName, key);

                _logger.LogInformation("S3 파일 다운로드 완료: {BucketName}/{Key} -> {DestinationPath}",
                    bucketName, key, destinationPath);

                return destinationPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "S3 파일 다운로드 중 오류 발생: {BucketName}/{Key} -> {DestinationPath}",
                    bucketName, key, destinationPath);
                throw;
            }
        }

        /// <summary>
        /// S3에서 파일을 삭제합니다.
        /// </summary>
        public async Task<bool> DeleteFileAsync(string bucketName, string key)
        {
            try
            {
                _logger.LogInformation("S3 파일 삭제 시작: {BucketName}/{Key}", bucketName, key);

                var request = new DeleteObjectRequest
                {
                    BucketName = bucketName,
                    Key = key
                };

                await _s3Client.DeleteObjectAsync(request);

                _logger.LogInformation("S3 파일 삭제 완료: {BucketName}/{Key}", bucketName, key);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "S3 파일 삭제 중 오류 발생: {BucketName}/{Key}", bucketName, key);
                return false;
            }
        }

        /// <summary>
        /// S3 파일의 URL을 가져옵니다.
        /// </summary>
        public string GetFileUrl(string bucketName, string key)
        {
            try
            {
                var url = $"https://{bucketName}.s3.ap-northeast-2.amazonaws.com/{key}";
                return url;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "S3 파일 URL 생성 중 오류 발생: {BucketName}/{Key}", bucketName, key);
                throw;
            }
        }

        /// <summary>
        /// S3 파일의 미리 서명된 URL을 가져옵니다. (임시 접근 URL)
        /// </summary>
        public string GetPreSignedUrl(string bucketName, string key, int expiryMinutes = 60)
        {
            try
            {
                _logger.LogInformation("S3 파일 미리 서명된 URL 생성 시작: {BucketName}/{Key}, 만료: {ExpiryMinutes}분",
                    bucketName, key, expiryMinutes);

                var request = new GetPreSignedUrlRequest
                {
                    BucketName = bucketName,
                    Key = key,
                    Expires = DateTime.UtcNow.AddMinutes(expiryMinutes)
                };

                var url = _s3Client.GetPreSignedURL(request);

                _logger.LogInformation("S3 파일 미리 서명된 URL 생성 완료: {BucketName}/{Key}", bucketName, key);
                return url;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "S3 파일 미리 서명된 URL 생성 중 오류 발생: {BucketName}/{Key}", bucketName, key);
                throw;
            }
        }
    }
}