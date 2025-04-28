using Dapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using UnivFI.Domain.Entities;
using UnivFI.Domain.Interfaces.Repositories;

namespace UnivFI.Infrastructure.Repositories
{
    /// <summary>
    /// 파일 첨부 정보를 관리하는 리포지토리입니다.
    /// 파일 첨부 정보의 생성, 조회, 삭제 기능을 제공합니다.
    /// </summary>
    /// <remarks>
    /// 주요 기능:
    /// - 파일 첨부 정보 저장
    /// - ID 또는 GUID를 통한 파일 첨부 정보 조회
    /// - 모든 파일 첨부 정보 조회
    /// - 파일 첨부 정보 삭제
    /// </remarks>
    public class FileAttachRepository : BaseRepository<FileAttachEntity, int, FileAttachRepository>, IFileAttachRepository
    {
        private const string TABLE_NAME = "FileAttaches";

        public FileAttachRepository(IConnectionFactory connectionFactory, ILogger<FileAttachRepository> logger)
            : base(connectionFactory, logger, TABLE_NAME)
        {
        }

        /// <summary>
        /// 파일 첨부 정보를 저장합니다.
        /// </summary>
        /// <param name="fileAttach">파일 첨부 엔티티</param>
        /// <returns>저장된 파일 첨부 ID</returns>
        public async Task<long> SaveFileAttachAsync(FileAttachEntity fileAttach)
        {
            try
            {
                const string sql = @"
                    INSERT INTO FileAttaches (
                        FilePath,
                        FileName,
                        FileType,
                        FileLength,
                        FileGUID,
                        CreatedAt
                    ) VALUES (
                        @FilePath,
                        @FileName,
                        @FileType,
                        @FileLength,
                        @FileGUID,
                        GETDATE()
                    );
                    SELECT SCOPE_IDENTITY();";

                // SQL 쿼리 로깅
                LogEntityOperation("INSERT INTO", fileAttach);

                using var connection = CreateConnection();
                var id = await connection.ExecuteScalarAsync<long>(sql, fileAttach);
                Logger.LogInformation("파일 첨부 정보 저장됨: {@FileAttach}", fileAttach);
                return id;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "파일 첨부 정보 저장 중 오류 발생: {@FileAttach}", fileAttach);
                throw;
            }
        }

        /// <summary>
        /// 파일 첨부 정보를 조회합니다.
        /// </summary>
        /// <param name="fileAttachId">파일 첨부 ID</param>
        /// <returns>파일 첨부 엔티티</returns>
        public async Task<FileAttachEntity> GetFileAttachAsync(long fileAttachId)
        {
            try
            {
                const string sql = @"
                    SELECT 
                        FileAttachId,
                        FilePath,
                        FileName,
                        FileType,
                        FileLength,
                        FileGUID,
                        CreatedAt
                    FROM FileAttaches
                    WHERE FileAttachId = @FileAttachId";

                // ID 기반 쿼리 로깅
                LogIdOperation("SELECT", fileAttachId);

                using var connection = CreateConnection();
                var fileAttach = await connection.QueryFirstOrDefaultAsync<FileAttachEntity>(sql, new { FileAttachId = fileAttachId });
                return fileAttach;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "파일 첨부 정보 조회 중 오류 발생, ID: {FileAttachId}", fileAttachId);
                throw;
            }
        }

        /// <summary>
        /// GUID로 파일 첨부 정보를 조회합니다.
        /// </summary>
        /// <param name="fileGuid">파일 GUID</param>
        /// <returns>파일 첨부 엔티티</returns>
        public async Task<FileAttachEntity> GetFileAttachByGuidAsync(string fileGuid)
        {
            try
            {
                const string sql = @"
                    SELECT 
                        FileAttachId,
                        FilePath,
                        FileName,
                        FileType,
                        FileLength,
                        FileGUID,
                        CreatedAt
                    FROM FileAttaches
                    WHERE FileGUID = @FileGUID";

                var parameters = new { FileGUID = fileGuid };

                // SQL 쿼리 로깅
                LogQuery(sql, parameters);

                using var connection = CreateConnection();
                var fileAttach = await connection.QueryFirstOrDefaultAsync<FileAttachEntity>(sql, parameters);
                return fileAttach;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GUID로 파일 첨부 정보 조회 중 오류 발생, GUID: {FileGUID}", fileGuid);
                throw;
            }
        }

        /// <summary>
        /// 모든 파일 첨부 정보를 조회합니다.
        /// </summary>
        /// <returns>파일 첨부 엔티티 목록</returns>
        public async Task<IEnumerable<FileAttachEntity>> GetAllFileAttachesAsync()
        {
            try
            {
                const string sql = @"
                    SELECT 
                        FileAttachId,
                        FilePath,
                        FileName,
                        FileType,
                        FileLength,
                        FileGUID,
                        CreatedAt
                    FROM FileAttaches
                    ORDER BY CreatedAt DESC";

                // SQL 쿼리 로깅
                LogQuery(sql, null);

                using var connection = CreateConnection();
                var fileAttaches = await connection.QueryAsync<FileAttachEntity>(sql);
                return fileAttaches;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "파일 첨부 정보 목록 조회 중 오류 발생");
                throw;
            }
        }

        /// <summary>
        /// 파일 첨부 정보를 삭제합니다.
        /// </summary>
        /// <param name="fileAttachId">파일 첨부 ID</param>
        /// <returns>삭제 성공 여부</returns>
        public async Task<bool> DeleteFileAttachAsync(long fileAttachId)
        {
            try
            {
                const string sql = @"
                    DELETE FROM FileAttaches
                    WHERE FileAttachId = @FileAttachId";

                // ID 기반 쿼리 로깅
                LogIdOperation("DELETE FROM", fileAttachId);

                using var connection = CreateConnection();
                var rowsAffected = await connection.ExecuteAsync(sql, new { FileAttachId = fileAttachId });
                var success = rowsAffected > 0;

                if (success)
                {
                    Logger.LogInformation("파일 첨부 정보 삭제됨, ID: {FileAttachId}", fileAttachId);
                }
                else
                {
                    Logger.LogWarning("파일 첨부 정보 삭제 실패, ID: {FileAttachId} - 해당 ID의 파일 첨부가 존재하지 않음", fileAttachId);
                }

                return success;
            }
            catch (Exception ex)
            {
                return LogErrorAndReturnFalse(ex, "파일 첨부 정보 삭제 중 오류 발생, ID: {FileAttachId}", fileAttachId);
            }
        }

        /// <summary>
        /// GUID로 파일 첨부 정보를 삭제합니다.
        /// </summary>
        /// <param name="fileGuid">파일 GUID</param>
        /// <returns>삭제 성공 여부</returns>
        public async Task<bool> DeleteFileAttachByGuidAsync(string fileGuid)
        {
            try
            {
                const string sql = @"
                    DELETE FROM FileAttaches
                    WHERE FileGUID = @FileGUID";

                var parameters = new { FileGUID = fileGuid };

                // SQL 쿼리 로깅
                LogQuery(sql, parameters);

                using var connection = CreateConnection();
                var rowsAffected = await connection.ExecuteAsync(sql, parameters);
                var success = rowsAffected > 0;

                if (success)
                {
                    Logger.LogInformation("GUID로 파일 첨부 정보 삭제됨, GUID: {FileGUID}", fileGuid);
                }
                else
                {
                    Logger.LogWarning("GUID로 파일 첨부 정보 삭제 실패, GUID: {FileGUID} - 해당 GUID의 파일 첨부가 존재하지 않음", fileGuid);
                }

                return success;
            }
            catch (Exception ex)
            {
                return LogErrorAndReturnFalse(ex, "GUID로 파일 첨부 정보 삭제 중 오류 발생, GUID: {FileGUID}", fileGuid);
            }
        }
    }
}