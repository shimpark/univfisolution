using Dapper;
using Dapper.Contrib.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnivFI.Infrastructure.Helpers;

namespace UnivFI.Infrastructure.Repositories
{
    /// <summary>
    /// 모든 리포지토리에서 공통적으로 사용할 수 있는 기반 리포지토리 클래스입니다.
    /// SQL 쿼리 로깅 및 기본 데이터 접근 기능을 제공합니다.
    /// </summary>
    /// <typeparam name="TEntity">엔티티 타입</typeparam>
    /// <typeparam name="TKey">엔티티 키 타입</typeparam>
    /// <typeparam name="TLogger">로거 타입</typeparam>
    public abstract class BaseRepository<TEntity, TKey, TLogger>
        where TEntity : class
        where TLogger : class
    {
        protected readonly IConnectionFactory ConnectionFactory;
        protected readonly ILogger<TLogger> Logger;
        protected readonly string TableName;

        /// <summary>
        /// 기반 리포지토리 생성자
        /// </summary>
        /// <param name="connectionFactory">데이터베이스 연결 팩토리</param>
        /// <param name="logger">로거 인스턴스</param>
        /// <param name="tableName">테이블 이름</param>
        protected BaseRepository(IConnectionFactory connectionFactory, ILogger<TLogger> logger, string tableName)
        {
            ConnectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            TableName = !string.IsNullOrEmpty(tableName) ? tableName : throw new ArgumentNullException(nameof(tableName));
        }

        /// <summary>
        /// SQL 쿼리와 파라미터를 로깅합니다.
        /// </summary>
        /// <param name="sql">SQL 쿼리문</param>
        /// <param name="parameters">SQL 파라미터</param>
        protected void LogQuery(string sql, object? parameters)
        {
            SqlQueryLogger.LogQuery(Logger, sql, parameters);
        }

        /// <summary>
        /// ID 기반 쿼리를 로깅합니다.
        /// </summary>
        /// <param name="operationType">작업 타입 (SELECT, UPDATE, DELETE)</param>
        /// <param name="id">ID 값</param>
        protected void LogIdOperation(string operationType, object id)
        {
            SqlQueryLogger.LogIdOperation(Logger, operationType, TableName, id);
        }

        /// <summary>
        /// 엔티티 기반 쿼리를 로깅합니다.
        /// </summary>
        /// <param name="operationType">작업 타입 (INSERT INTO, UPDATE)</param>
        /// <param name="entity">엔티티 객체</param>
        /// <param name="sensitiveProperties">마스킹할 민감한 속성 이름 배열</param>
        protected void LogEntityOperation(string operationType, object entity, params string[] sensitiveProperties)
        {
            SqlQueryLogger.LogEntityOperation(Logger, operationType, TableName, entity, sensitiveProperties);
        }

        /// <summary>
        /// 데이터베이스 연결을 생성합니다.
        /// </summary>
        /// <returns>데이터베이스 연결</returns>
        protected IDbConnection CreateConnection()
        {
            return ConnectionFactory.CreateConnection();
        }

        /// <summary>
        /// 엔티티 ID로 단일 엔티티를 조회합니다.
        /// </summary>
        /// <param name="id">엔티티 ID</param>
        /// <returns>조회된 엔티티 또는 null</returns>
        public virtual async Task<TEntity> GetByIdAsync(TKey id)
        {
            try
            {
                using var connection = CreateConnection();

                // ID 기반 쿼리 로깅
                LogIdOperation("SELECT", id);

                return await connection.GetAsync<TEntity>(id);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "ID {Id}로 {Table} 엔티티 조회 중 오류 발생", id, TableName);
                throw;
            }
        }

        /// <summary>
        /// 모든 엔티티를 조회합니다.
        /// </summary>
        /// <returns>엔티티 컬렉션</returns>
        public virtual async Task<IEnumerable<TEntity>> GetAllAsync()
        {
            try
            {
                using var connection = CreateConnection();

                // SQL 쿼리 로깅
                LogQuery($"SELECT * FROM {TableName}", new { });

                return await connection.GetAllAsync<TEntity>();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "{Table} 테이블의 모든 엔티티 조회 중 오류 발생", TableName);
                throw;
            }
        }

        /// <summary>
        /// 새 엔티티를 생성합니다.
        /// </summary>
        /// <param name="entity">생성할 엔티티</param>
        /// <returns>생성된 엔티티의 ID</returns>
        public virtual async Task<TKey> CreateAsync(TEntity entity)
        {
            try
            {
                using var connection = CreateConnection();

                // 엔티티 작업 로깅
                LogEntityOperation("INSERT INTO", entity);

                // 반환 값을 object로 받고 TKey로 변환
                var insertedId = await connection.InsertAsync(entity);

                // 타입에 따른 변환 처리
                if (typeof(TKey) == typeof(int))
                {
                    return (TKey)(object)insertedId;
                }
                else if (typeof(TKey) == typeof(long))
                {
                    return (TKey)(object)(long)insertedId;
                }
                else if (typeof(TKey) == typeof(string))
                {
                    return (TKey)(object)insertedId.ToString();
                }
                else if (typeof(TKey) == typeof(Guid))
                {
                    if (Guid.TryParse(insertedId.ToString(), out Guid guid))
                    {
                        return (TKey)(object)guid;
                    }
                }
                else if (typeof(TKey) == typeof(object))
                {
                    return (TKey)(object)insertedId;
                }

                // 기본 변환 시도
                return (TKey)Convert.ChangeType(insertedId, typeof(TKey));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "{Table} 테이블에 엔티티 추가 중 오류 발생", TableName);
                throw;
            }
        }

        /// <summary>
        /// 기존 엔티티를 업데이트합니다.
        /// </summary>
        /// <param name="entity">업데이트할 엔티티</param>
        /// <returns>업데이트 성공 여부</returns>
        public virtual async Task<bool> UpdateAsync(TEntity entity)
        {
            try
            {
                using var connection = CreateConnection();

                // 엔티티 작업 로깅
                LogEntityOperation("UPDATE", entity);

                return await connection.UpdateAsync(entity);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "{Table} 테이블의 엔티티 업데이트 중 오류 발생", TableName);
                throw;
            }
        }

        /// <summary>
        /// 엔티티 ID로 엔티티를 삭제합니다.
        /// </summary>
        /// <param name="id">삭제할 엔티티의 ID</param>
        /// <returns>삭제 성공 여부</returns>
        public virtual async Task<bool> DeleteAsync(TKey id)
        {
            try
            {
                using var connection = CreateConnection();

                // ID 기반 쿼리 로깅
                LogIdOperation("SELECT", id);

                var entity = await connection.GetAsync<TEntity>(id);

                if (entity == null)
                {
                    return false;
                }

                LogIdOperation("DELETE FROM", id);

                return await connection.DeleteAsync(entity);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "ID {Id}로 {Table} 테이블에서 엔티티 삭제 중 오류 발생", id, TableName);
                throw;
            }
        }

        /// <summary>
        /// 페이징 쿼리를 위한 SQL 파라미터를 생성합니다.
        /// </summary>
        /// <param name="page">페이지 번호 (1부터 시작)</param>
        /// <param name="pageSize">페이지 크기</param>
        /// <returns>페이징 파라미터</returns>
        protected DynamicParameters CreatePagingParameters(int page, int pageSize)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@Offset", (page - 1) * pageSize, DbType.Int32);
            parameters.Add("@PageSize", pageSize, DbType.Int32);
            return parameters;
        }

        /// <summary>
        /// 검색 조건 절을 생성합니다.
        /// </summary>
        /// <param name="searchTerm">검색어</param>
        /// <param name="searchFields">검색 필드 목록 (쉼표로 구분)</param>
        /// <param name="parameters">SQL 파라미터 객체 (검색어 파라미터가 추가됨)</param>
        /// <returns>WHERE 절 문자열</returns>
        protected string CreateSearchWhereClause(string? searchTerm, string? searchFields, DynamicParameters parameters)
        {
            if (parameters == null)
            {
                parameters = new DynamicParameters();
            }

            // 검색 필드 기본값 설정
            if (string.IsNullOrEmpty(searchFields))
            {
                searchFields = "Name";
            }

            // 검색 조건 생성
            var whereClause = "@SearchTerm IS NULL";
            parameters.Add("@SearchTerm", searchTerm, DbType.String);

            if (!string.IsNullOrEmpty(searchTerm))
            {
                var searchFieldsArray = searchFields.Split(',');
                var searchConditions = new List<string>();

                foreach (var field in searchFieldsArray)
                {
                    searchConditions.Add($"{field.Trim()} LIKE '%' + @SearchTerm + '%'");
                }

                whereClause = $"(@SearchTerm IS NULL OR {string.Join(" OR ", searchConditions)})";
            }

            return whereClause;
        }

        /// <summary>
        /// 예외를 로깅하고 false를 반환합니다.
        /// </summary>
        /// <param name="ex">발생한 예외</param>
        /// <param name="message">오류 메시지</param>
        /// <param name="args">메시지 형식 인자</param>
        /// <returns>항상 false</returns>
        protected bool LogErrorAndReturnFalse(Exception ex, string message, params object[] args)
        {
            Logger.LogError(ex, message, args);
            return false;
        }

        /// <summary>
        /// 정렬 가능한 컬럼 목록을 반환합니다.
        /// 하위 클래스에서 재정의하여 사용 가능합니다.
        /// </summary>
        /// <returns>정렬 가능한 컬럼 목록</returns>
        protected virtual IEnumerable<string> GetAllowedSortColumns()
        {
            return new[] { "Id", "CreatedAt", "UpdatedAt" };
        }

        /// <summary>
        /// 트랜잭션 내에서 여러 작업을 수행합니다.
        /// </summary>
        /// <param name="action">트랜잭션 내에서 실행할 작업</param>
        /// <returns>작업 성공 여부</returns>
        protected async Task<bool> ExecuteInTransactionAsync(Func<IDbConnection, IDbTransaction, Task<bool>> action)
        {
            using var connection = CreateConnection();
            connection.Open();

            using var transaction = connection.BeginTransaction();

            try
            {
                var result = await action(connection, transaction);

                if (result)
                {
                    transaction.Commit();
                }
                else
                {
                    transaction.Rollback();
                }

                return result;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "{Table} 테이블 트랜잭션 실행 중 오류 발생", TableName);
                transaction.Rollback();
                throw;
            }
        }
    }
}