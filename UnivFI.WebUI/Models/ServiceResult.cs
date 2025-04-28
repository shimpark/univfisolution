namespace UnivFI.WebUI.Models
{
    /// <summary>
    /// 서비스 작업의 결과를 래핑하는 제네릭 클래스
    /// </summary>
    /// <typeparam name="T">결과 데이터의 타입</typeparam>
    public class ServiceResult<T>
    {
        /// <summary>
        /// 작업 성공 여부
        /// </summary>
        public bool Success { get; private set; }

        /// <summary>
        /// 작업 결과 데이터
        /// </summary>
        public T? Data { get; private set; }

        /// <summary>
        /// 오류 메시지 (실패 시 설정)
        /// </summary>
        public string ErrorMessage { get; private set; } = string.Empty;

        /// <summary>
        /// 성공 결과를 생성합니다.
        /// </summary>
        /// <param name="data">결과 데이터</param>
        /// <returns>성공 상태의 서비스 결과</returns>
        public static ServiceResult<T> CreateSuccess(T data)
        {
            return new ServiceResult<T>
            {
                Success = true,
                Data = data,
                ErrorMessage = string.Empty
            };
        }

        /// <summary>
        /// 실패 결과를 생성합니다.
        /// </summary>
        /// <param name="errorMessage">오류 메시지</param>
        /// <returns>실패 상태의 서비스 결과</returns>
        public static ServiceResult<T> CreateFailure(string errorMessage)
        {
            return new ServiceResult<T>
            {
                Success = false,
                Data = default,
                ErrorMessage = errorMessage
            };
        }
    }

    /// <summary>
    /// 반환 데이터가 없는 서비스 작업의 결과를 래핑하는 클래스
    /// </summary>
    public class ServiceResult
    {
        /// <summary>
        /// 작업 성공 여부
        /// </summary>
        public bool Success { get; private set; }

        /// <summary>
        /// 오류 메시지 (실패 시 설정)
        /// </summary>
        public string ErrorMessage { get; private set; } = string.Empty;

        /// <summary>
        /// 성공 결과를 생성합니다.
        /// </summary>
        /// <returns>성공 상태의 서비스 결과</returns>
        public static ServiceResult CreateSuccess()
        {
            return new ServiceResult
            {
                Success = true,
                ErrorMessage = string.Empty
            };
        }

        /// <summary>
        /// 실패 결과를 생성합니다.
        /// </summary>
        /// <param name="errorMessage">오류 메시지</param>
        /// <returns>실패 상태의 서비스 결과</returns>
        public static ServiceResult CreateFailure(string errorMessage)
        {
            return new ServiceResult
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }
    }
}