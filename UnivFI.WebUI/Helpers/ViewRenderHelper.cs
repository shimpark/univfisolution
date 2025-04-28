using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System;
using System.IO;
using System.Threading.Tasks;

namespace UnivFI.WebUI.Helpers
{
    /// <summary>
    /// 뷰 렌더링을 위한 공통 헬퍼 클래스
    /// </summary>
    public static class ViewRenderHelper
    {
        /// <summary>
        /// 뷰를 문자열로 렌더링합니다.
        /// </summary>
        /// <param name="controller">현재 컨트롤러 인스턴스</param>
        /// <param name="viewEngine">뷰 엔진</param>
        /// <param name="viewName">렌더링할 뷰 이름</param>
        /// <param name="model">뷰에 전달할 모델</param>
        /// <returns>렌더링된 뷰의 HTML 문자열</returns>
        public static string RenderViewToString(Controller controller, ICompositeViewEngine viewEngine, string viewName, object model)
        {
            controller.ViewData.Model = model;
            using (var sw = new StringWriter())
            {
                try
                {
                    // 원래 뷰 이름으로 시도
                    var viewResult = FindView(controller, viewEngine, viewName);

                    if (viewResult.View == null)
                    {
                        throw new ArgumentNullException($"Could not find the view '{viewName}'. 경로를 확인하세요.");
                    }

                    var viewContext = new ViewContext(
                        controller.ControllerContext,
                        viewResult.View,
                        controller.ViewData,
                        controller.TempData,
                        sw,
                        new HtmlHelperOptions()
                    );

                    viewResult.View.RenderAsync(viewContext).GetAwaiter().GetResult();
                    return sw.ToString();
                }
                catch (Exception ex)
                {
                    // 오류 로그 기록
                    Console.WriteLine($"RenderViewToString 오류: {ex.Message}");
                    throw;
                }
            }
        }

        /// <summary>
        /// 비동기적으로 뷰를 문자열로 렌더링합니다.
        /// </summary>
        /// <param name="controller">현재 컨트롤러 인스턴스</param>
        /// <param name="viewEngine">뷰 엔진</param>
        /// <param name="viewName">렌더링할 뷰 이름</param>
        /// <param name="model">뷰에 전달할 모델</param>
        /// <returns>렌더링된 뷰의 HTML 문자열</returns>
        public static async Task<string> RenderViewToStringAsync(Controller controller, ICompositeViewEngine viewEngine, string viewName, object model)
        {
            controller.ViewData.Model = model;
            using (var sw = new StringWriter())
            {
                try
                {
                    // 원래 뷰 이름으로 시도
                    var viewResult = FindView(controller, viewEngine, viewName);

                    if (viewResult.View == null)
                    {
                        throw new ArgumentNullException($"Could not find the view '{viewName}'. 경로를 확인하세요.");
                    }

                    var viewContext = new ViewContext(
                        controller.ControllerContext,
                        viewResult.View,
                        controller.ViewData,
                        controller.TempData,
                        sw,
                        new HtmlHelperOptions()
                    );

                    await viewResult.View.RenderAsync(viewContext);
                    return sw.ToString();
                }
                catch (Exception ex)
                {
                    // 오류 로그 기록
                    Console.WriteLine($"RenderViewToStringAsync 오류: {ex.Message}");
                    throw;
                }
            }
        }

        /// <summary>
        /// 다양한 경로 형식으로 뷰를 찾습니다.
        /// </summary>
        /// <param name="controller">컨트롤러</param>
        /// <param name="viewEngine">뷰 엔진</param>
        /// <param name="viewName">뷰 이름</param>
        /// <returns>찾은 뷰 결과</returns>
        private static ViewEngineResult FindView(Controller controller, ICompositeViewEngine viewEngine, string viewName)
        {
            // 뷰 이름에서 '~/' 접두사 제거
            if (viewName.StartsWith("~/"))
            {
                viewName = viewName.Substring(2);
            }

            // 1. 원래 뷰 이름으로 시도
            var result = viewEngine.FindView(controller.ControllerContext, viewName, false);
            if (result.Success) return result;

            // 2. 파일 이름만 추출해서 시도
            var fileName = Path.GetFileName(viewName);
            result = viewEngine.FindView(controller.ControllerContext, fileName, false);
            if (result.Success) return result;

            // 3. 공유 뷰 폴더에서 시도
            result = viewEngine.FindView(controller.ControllerContext, $"~/Views/Shared/{fileName}", false);
            if (result.Success) return result;

            // 4. 공유 뷰 폴더에서 시도 (접두사 없이)
            result = viewEngine.FindView(controller.ControllerContext, $"Views/Shared/{fileName}", false);
            if (result.Success) return result;

            // 5. 컨트롤러 이름을 사용하여 뷰 폴더에서 시도
            var controllerName = controller.ControllerContext.ActionDescriptor.ControllerName;
            result = viewEngine.FindView(controller.ControllerContext, $"~/Views/{controllerName}/{fileName}", false);
            if (result.Success) return result;

            // 6. 컨트롤러 이름을 사용하여 뷰 폴더에서 시도 (접두사 없이)
            result = viewEngine.FindView(controller.ControllerContext, $"Views/{controllerName}/{fileName}", false);
            if (result.Success) return result;

            // 7. 접두사 없이 전체 경로 시도
            if (viewName.StartsWith("Views/"))
            {
                result = viewEngine.FindView(controller.ControllerContext, viewName, false);
                if (result.Success) return result;
            }

            // 8. 마지막 시도: 접두사 추가 시도
            if (!viewName.StartsWith("~/") && !viewName.StartsWith("/") && !viewName.StartsWith("Views/"))
            {
                result = viewEngine.FindView(controller.ControllerContext, $"~/Views/Shared/{viewName}", false);
                if (result.Success) return result;
            }

            // 9. 원래 결과 반환 (실패할 것임)
            return result;
        }
    }
}