using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using UnivFI.WebUI.Models;

namespace UnivFI.WebUI.Extensions
{
    /// <summary>
    /// 컨트롤러 관련 확장 메서드 모음
    /// </summary>
    public static class ControllerExtensions
    {
        /// <summary>
        /// ServiceResult를 처리하고 적절한 ActionResult를 반환합니다.
        /// </summary>
        /// <typeparam name="T">결과 데이터 타입</typeparam>
        /// <param name="controller">컨트롤러 인스턴스</param>
        /// <param name="result">서비스 결과</param>
        /// <param name="successAction">성공 시 수행할 액션</param>
        /// <param name="failureAction">실패 시 수행할 액션(기본값: null)</param>
        /// <returns>적절한 ActionResult</returns>
        public static IActionResult HandleServiceResult<T>(
            this Controller controller,
            ServiceResult<T> result,
            Func<T, IActionResult> successAction,
            Func<string, IActionResult> failureAction = null)
        {
            if (result.Success)
            {
                return successAction(result.Data);
            }

            // 실패 처리
            if (failureAction != null)
            {
                return failureAction(result.ErrorMessage);
            }

            // 기본 실패 처리
            controller.ModelState.AddModelError("", result.ErrorMessage);
            return controller.View();
        }

        /// <summary>
        /// 데이터가 없는 ServiceResult를 처리하고 적절한 ActionResult를 반환합니다.
        /// </summary>
        /// <param name="controller">컨트롤러 인스턴스</param>
        /// <param name="result">서비스 결과</param>
        /// <param name="successAction">성공 시 수행할 액션</param>
        /// <param name="failureAction">실패 시 수행할 액션(기본값: null)</param>
        /// <returns>적절한 ActionResult</returns>
        public static IActionResult HandleServiceResult(
            this Controller controller,
            ServiceResult result,
            Func<IActionResult> successAction,
            Func<string, IActionResult> failureAction = null)
        {
            if (result.Success)
            {
                return successAction();
            }

            // 실패 처리
            if (failureAction != null)
            {
                return failureAction(result.ErrorMessage);
            }

            // 기본 실패 처리
            controller.ModelState.AddModelError("", result.ErrorMessage);
            return controller.View();
        }

        /// <summary>
        /// ModelState 오류를 TempData에 추가합니다.
        /// </summary>
        /// <param name="controller">컨트롤러 인스턴스</param>
        /// <param name="modelState">ModelState 객체</param>
        /// <param name="key">TempData에 저장할 키(기본값: "ErrorMessages")</param>
        public static void AddModelErrorsToTempData(
            this Controller controller,
            ModelStateDictionary modelState,
            string key = "ErrorMessages")
        {
            var errorMessages = modelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            controller.TempData[key] = errorMessages;
        }

        /// <summary>
        /// 경고 메시지를 TempData에 추가합니다.
        /// </summary>
        /// <param name="controller">컨트롤러 인스턴스</param>
        /// <param name="message">경고 메시지</param>
        /// <param name="key">TempData에 저장할 키(기본값: "WarningMessage")</param>
        public static void AddWarningMessage(
            this Controller controller,
            string message,
            string key = "WarningMessage")
        {
            controller.TempData[key] = message;
        }

        /// <summary>
        /// 성공 메시지를 TempData에 추가합니다.
        /// </summary>
        /// <param name="controller">컨트롤러 인스턴스</param>
        /// <param name="message">성공 메시지</param>
        /// <param name="key">TempData에 저장할 키(기본값: "SuccessMessage")</param>
        public static void AddSuccessMessage(
            this Controller controller,
            string message,
            string key = "SuccessMessage")
        {
            controller.TempData[key] = message;
        }
    }
}