using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnivFI.Domain.Entities;
using UnivFI.Domain.Models;

namespace UnivFI.Domain.Interfaces.Repositories
{
    public interface IFileAttachRepository
    {
        /// <summary>
        /// 파일 첨부 정보를 저장합니다.
        /// </summary>
        /// <param name="fileAttach">파일 첨부 모델</param>
        /// <returns>저장된 파일 첨부 ID</returns>
        Task<long> SaveFileAttachAsync(FileAttachEntity fileAttach);

        /// <summary>
        /// 파일 첨부 정보를 조회합니다.
        /// </summary>
        /// <param name="fileAttachId">파일 첨부 ID</param>
        /// <returns>파일 첨부 모델</returns>
        Task<FileAttachEntity> GetFileAttachAsync(long fileAttachId);

        /// <summary>
        /// GUID로 파일 첨부 정보를 조회합니다.
        /// </summary>
        /// <param name="fileGuid">파일 GUID</param>
        /// <returns>파일 첨부 모델</returns>
        Task<FileAttachEntity> GetFileAttachByGuidAsync(string fileGuid);

        /// <summary>
        /// 모든 파일 첨부 정보를 조회합니다.
        /// </summary>
        /// <returns>파일 첨부 모델 목록</returns>
        Task<IEnumerable<FileAttachEntity>> GetAllFileAttachesAsync();

        /// <summary>
        /// 파일 첨부 정보를 삭제합니다.
        /// </summary>
        /// <param name="fileAttachId">파일 첨부 ID</param>
        /// <returns>삭제 성공 여부</returns>
        Task<bool> DeleteFileAttachAsync(long fileAttachId);

        /// <summary>
        /// GUID로 파일 첨부 정보를 삭제합니다.
        /// </summary>
        /// <param name="fileGuid">파일 GUID</param>
        /// <returns>삭제 성공 여부</returns>
        Task<bool> DeleteFileAttachByGuidAsync(string fileGuid);
    }
}