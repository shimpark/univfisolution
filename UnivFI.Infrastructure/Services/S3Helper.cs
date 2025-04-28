using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;
using UnivFI.Application.Interfaces;

namespace UnivFI.Infrastructure.Services
{
    /// <summary>
    /// S3 서비스 사용을 간소화하는 헬퍼 클래스입니다.
    /// </summary>
    public class S3Helper
    {
        private readonly IS3Service _s3Service;
        private readonly ILogger<S3Helper> _logger;
        private readonly string _defaultBucket;
        private readonly string _filePrefix;

        public S3Helper(IS3Service s3Service, IConfiguration configuration, ILogger<S3Helper> logger)
        {
            _s3Service = s3Service;
            _logger = logger;
            _defaultBucket = configuration["AWS:S3:DefaultBucket"] ?? "your-bucket-name";
            _filePrefix = configuration["AWS:S3:FilePrefix"] ?? "uploads/";
        }

        /// <summary>
        /// 파일을 업로드하고 S3 URL을 반환합니다.
        /// </summary>
        /// <param name="filePath">업로드할 파일 경로</param>
        /// <param name="folder">S3 폴더 경로 (옵션)</param>
        /// <returns>업로드된 파일의 URL</returns>
        public async Task<string> UploadFileAsync(string filePath, string folder = "")
        {
            try
            {
                var fileName = Path.GetFileName(filePath);
                var key = BuildKey(fileName, folder);

                return await _s3Service.UploadFileAsync(_defaultBucket, key, filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "S3Helper 파일 업로드 중 오류 발생: {FilePath}", filePath);
                throw;
            }
        }

        /// <summary>
        /// 스트림을 S3에 업로드합니다.
        /// </summary>
        /// <param name="fileName">파일 이름</param>
        /// <param name="stream">업로드할 스트림</param>
        /// <param name="folder">S3 폴더 경로 (옵션)</param>
        /// <returns>업로드된 파일의 URL</returns>
        public async Task<string> UploadStreamAsync(string fileName, Stream stream, string folder = "")
        {
            try
            {
                var key = BuildKey(fileName, folder);

                return await _s3Service.UploadStreamAsync(_defaultBucket, key, stream);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "S3Helper 스트림 업로드 중 오류 발생: {FileName}", fileName);
                throw;
            }
        }

        /// <summary>
        /// 바이트 배열을 S3에 업로드합니다.
        /// </summary>
        /// <param name="fileName">파일 이름</param>
        /// <param name="data">업로드할 바이트 배열</param>
        /// <param name="folder">S3 폴더 경로 (옵션)</param>
        /// <returns>업로드된 파일의 URL</returns>
        public async Task<string> UploadBytesAsync(string fileName, byte[] data, string folder = "")
        {
            try
            {
                var key = BuildKey(fileName, folder);

                return await _s3Service.UploadBytesAsync(_defaultBucket, key, data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "S3Helper 바이트 데이터 업로드 중 오류 발생: {FileName}", fileName);
                throw;
            }
        }

        /// <summary>
        /// S3 파일을 스트림으로 다운로드합니다.
        /// </summary>
        /// <param name="fileName">파일 이름</param>
        /// <param name="folder">S3 폴더 경로 (옵션)</param>
        /// <returns>파일 스트림</returns>
        public async Task<Stream> DownloadStreamAsync(string fileName, string folder = "")
        {
            try
            {
                var key = BuildKey(fileName, folder);

                return await _s3Service.DownloadStreamAsync(_defaultBucket, key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "S3Helper 스트림 다운로드 중 오류 발생: {FileName}", fileName);
                throw;
            }
        }

        /// <summary>
        /// S3 파일을 바이트 배열로 다운로드합니다.
        /// </summary>
        /// <param name="fileName">파일 이름</param>
        /// <param name="folder">S3 폴더 경로 (옵션)</param>
        /// <returns>파일 바이트 배열</returns>
        public async Task<byte[]> DownloadBytesAsync(string fileName, string folder = "")
        {
            try
            {
                var key = BuildKey(fileName, folder);

                return await _s3Service.DownloadBytesAsync(_defaultBucket, key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "S3Helper 바이트 데이터 다운로드 중 오류 발생: {FileName}", fileName);
                throw;
            }
        }

        /// <summary>
        /// S3 파일을 특정 로컬 경로에 다운로드합니다.
        /// </summary>
        /// <param name="fileName">파일 이름</param>
        /// <param name="destinationPath">저장될 로컬 경로</param>
        /// <param name="folder">S3 폴더 경로 (옵션)</param>
        /// <returns>다운로드된 파일의 로컬 경로</returns>
        public async Task<string> DownloadToFileAsync(string fileName, string destinationPath, string folder = "")
        {
            try
            {
                var key = BuildKey(fileName, folder);

                return await _s3Service.DownloadToFileAsync(_defaultBucket, key, destinationPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "S3Helper 파일 다운로드 중 오류 발생: {FileName}", fileName);
                throw;
            }
        }

        /// <summary>
        /// S3 파일을 삭제합니다.
        /// </summary>
        /// <param name="fileName">파일 이름</param>
        /// <param name="folder">S3 폴더 경로 (옵션)</param>
        /// <returns>성공 여부</returns>
        public async Task<bool> DeleteFileAsync(string fileName, string folder = "")
        {
            try
            {
                var key = BuildKey(fileName, folder);

                return await _s3Service.DeleteFileAsync(_defaultBucket, key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "S3Helper 파일 삭제 중 오류 발생: {FileName}", fileName);
                throw;
            }
        }

        /// <summary>
        /// S3 파일의 URL을 가져옵니다.
        /// </summary>
        /// <param name="fileName">파일 이름</param>
        /// <param name="folder">S3 폴더 경로 (옵션)</param>
        /// <returns>파일의 S3 URL</returns>
        public string GetFileUrl(string fileName, string folder = "")
        {
            try
            {
                var key = BuildKey(fileName, folder);

                return _s3Service.GetFileUrl(_defaultBucket, key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "S3Helper URL 생성 중 오류 발생: {FileName}", fileName);
                throw;
            }
        }

        /// <summary>
        /// S3 파일의 미리 서명된 URL을 가져옵니다.
        /// </summary>
        /// <param name="fileName">파일 이름</param>
        /// <param name="expiryMinutes">URL 만료 시간(분)</param>
        /// <param name="folder">S3 폴더 경로 (옵션)</param>
        /// <returns>미리 서명된 URL</returns>
        public string GetPreSignedUrl(string fileName, int expiryMinutes = 60, string folder = "")
        {
            try
            {
                var key = BuildKey(fileName, folder);

                return _s3Service.GetPreSignedUrl(_defaultBucket, key, expiryMinutes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "S3Helper 미리 서명된 URL 생성 중 오류 발생: {FileName}", fileName);
                throw;
            }
        }

        /// <summary>
        /// S3 키를 생성합니다.
        /// </summary>
        /// <param name="fileName">파일 이름</param>
        /// <param name="folder">폴더 경로 (옵션)</param>
        /// <returns>S3 키</returns>
        private string BuildKey(string fileName, string folder = "")
        {
            // 파일명에 타임스탬프 추가하여 고유성 보장
            //var uniqueFileName = $"{DateTime.UtcNow.Ticks}_{fileName}";
            var uniqueFileName = $"{fileName}";

            // 폴더 경로를 정규화
            if (!string.IsNullOrEmpty(folder))
            {
                folder = folder.Trim('/') + "/";
            }

            return $"{_filePrefix}{folder}{uniqueFileName}";
        }
    }
}