using UnivFI.WebUI.Models;

namespace UnivFI.WebUI.Services
{
    /// <summary>
    /// 기본 CRUD 작업을 위한 제네릭 서비스 인터페이스
    /// </summary>
    /// <typeparam name="TDto">DTO 타입</typeparam>
    /// <typeparam name="TCreateDto">생성 DTO 타입</typeparam>
    /// <typeparam name="TUpdateDto">업데이트 DTO 타입</typeparam>
    /// <typeparam name="TKey">엔티티 키 타입</typeparam>
    public interface IGenericCrudService<TDto, TCreateDto, TUpdateDto, TKey>
        where TDto : class
        where TCreateDto : class
        where TUpdateDto : class
        where TKey : IEquatable<TKey>
    {
        /// <summary>
        /// 모든 항목을 페이징하여 가져옵니다.
        /// </summary>
        /// <param name="page">페이지 번호</param>
        /// <param name="pageSize">페이지 크기</param>
        /// <param name="searchTerm">검색어</param>
        /// <param name="searchFields">검색 필드</param>
        /// <returns>항목 목록과 페이징 정보가 포함된 결과</returns>
        Task<ServiceResult<(IEnumerable<TDto> Items, int TotalCount)>> GetAllAsync(
            int page = 1,
            int pageSize = 10,
            string searchTerm = "",
            string searchFields = "");

        /// <summary>
        /// ID로 항목을 가져옵니다.
        /// </summary>
        /// <param name="id">항목 ID</param>
        /// <returns>항목 정보가 포함된 결과</returns>
        Task<ServiceResult<TDto>> GetByIdAsync(TKey id);

        /// <summary>
        /// 새로운 항목을 생성합니다.
        /// </summary>
        /// <param name="createDto">생성할 항목 정보</param>
        /// <returns>생성된 항목 정보가 포함된 결과</returns>
        Task<ServiceResult<TDto>> CreateAsync(TCreateDto createDto);

        /// <summary>
        /// 기존 항목을 업데이트합니다.
        /// </summary>
        /// <param name="id">업데이트할 항목 ID</param>
        /// <param name="updateDto">업데이트할 항목 정보</param>
        /// <returns>작업 결과</returns>
        Task<ServiceResult> UpdateAsync(TKey id, TUpdateDto updateDto);

        /// <summary>
        /// 항목을 삭제합니다.
        /// </summary>
        /// <param name="id">삭제할 항목 ID</param>
        /// <returns>작업 결과</returns>
        Task<ServiceResult> DeleteAsync(TKey id);
    }
}