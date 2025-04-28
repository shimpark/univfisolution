using System.IO;
using System.Threading.Tasks;

namespace UnivFI.Application.Interfaces
{
    /// <summary>
    /// Amazon S3와 상호 작용하기 위한 서비스 인터페이스입니다.
    /// </summary>
    public interface IS3Service
    {
        /// <summary>
        /// 파일을 S3에 업로드합니다.
        /// </summary>
        /// <param name="bucketName">S3 버킷 이름</param>
        /// <param name="key">업로드할 파일의 키(경로)</param>
        /// <param name="filePath">업로드할 로컬 파일 경로</param>
        /// <returns>업로드된 파일의 URL</returns>
        Task<string> UploadFileAsync(string bucketName, string key, string filePath);

        /// <summary>
        /// 스트림을 S3에 업로드합니다.
        /// </summary>
        /// <param name="bucketName">S3 버킷 이름</param>
        /// <param name="key">업로드할 파일의 키(경로)</param>
        /// <param name="stream">업로드할 스트림</param>
        /// <returns>업로드된 파일의 URL</returns>
        Task<string> UploadStreamAsync(string bucketName, string key, Stream stream);

        /// <summary>
        /// 바이트 배열을 S3에 업로드합니다.
        /// </summary>
        /// <param name="bucketName">S3 버킷 이름</param>
        /// <param name="key">업로드할 파일의 키(경로)</param>
        /// <param name="data">업로드할 바이트 배열</param>
        /// <returns>업로드된 파일의 URL</returns>
        Task<string> UploadBytesAsync(string bucketName, string key, byte[] data);

        /// <summary>
        /// S3에서 파일을 스트림으로 다운로드합니다.
        /// </summary>
        /// <param name="bucketName">S3 버킷 이름</param>
        /// <param name="key">다운로드할 파일의 키(경로)</param>
        /// <returns>파일 스트림</returns>
        Task<Stream> DownloadStreamAsync(string bucketName, string key);

        /// <summary>
        /// S3에서 파일을 바이트 배열로 다운로드합니다.
        /// </summary>
        /// <param name="bucketName">S3 버킷 이름</param>
        /// <param name="key">다운로드할 파일의 키(경로)</param>
        /// <returns>파일 바이트 배열</returns>
        Task<byte[]> DownloadBytesAsync(string bucketName, string key);

        /// <summary>
        /// S3 파일을 특정 로컬 폴더에 다운로드합니다.
        /// </summary>
        /// <param name="bucketName">S3 버킷 이름</param>
        /// <param name="key">다운로드할 파일의 키(경로)</param>
        /// <param name="destinationPath">저장될 로컬 경로</param>
        /// <returns>다운로드된 파일의 로컬 경로</returns>
        Task<string> DownloadToFileAsync(string bucketName, string key, string destinationPath);

        /// <summary>
        /// S3 파일을 삭제합니다.
        /// </summary>
        /// <param name="bucketName">S3 버킷 이름</param>
        /// <param name="key">삭제할 파일의 키(경로)</param>
        /// <returns>성공 여부</returns>
        Task<bool> DeleteFileAsync(string bucketName, string key);

        /// <summary>
        /// S3 파일의 URL을 가져옵니다.
        /// </summary>
        /// <param name="bucketName">S3 버킷 이름</param>
        /// <param name="key">파일의 키(경로)</param>
        /// <returns>파일의 S3 URL</returns>
        string GetFileUrl(string bucketName, string key);

        /// <summary>
        /// S3 파일의 미리 서명된 URL을 가져옵니다.
        /// </summary>
        /// <param name="bucketName">S3 버킷 이름</param>
        /// <param name="key">파일의 키(경로)</param>
        /// <param name="expiryMinutes">URL 만료 시간(분)</param>
        /// <returns>미리 서명된 URL</returns>
        string GetPreSignedUrl(string bucketName, string key, int expiryMinutes = 60);
    }
}