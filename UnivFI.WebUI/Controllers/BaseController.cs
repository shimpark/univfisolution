using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using UnivFI.WebUI.Helpers;
using UnivFI.WebUI.ViewModels;
using System.Security.Claims;
using UnivFI.WebUI.Extensions;

namespace UnivFI.WebUI.Controllers
{
    /// <summary>
    /// 모든 컨트롤러의 기본이 되는 추상 클래스
    /// </summary>
    public abstract class BaseController : Controller
    {
        /// <summary>
        /// 매퍼 인스턴스
        /// </summary>
        protected readonly IMapper Mapper;

        /// <summary>
        /// 로거 인스턴스
        /// </summary>
        protected readonly ILogger<BaseController> Logger;

        /// <summary>
        /// 뷰 엔진 인스턴스
        /// </summary>
        protected readonly ITempDataDictionaryFactory TempDataFactory;

        /// <summary>
        /// 뷰 렌더링을 위한 컴포지트 뷰 엔진
        /// </summary>
        protected readonly ICompositeViewEngine ViewEngine;

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="mapper">매퍼 인스턴스</param>
        /// <param name="tempDataFactory">TempData 팩토리 인스턴스</param>
        /// <param name="viewEngine">뷰 엔진</param>
        /// <param name="logger">로거 인스턴스</param>
        protected BaseController(
            IMapper mapper,
            ITempDataDictionaryFactory tempDataFactory,
            ICompositeViewEngine viewEngine,
            ILogger<BaseController>? logger = null)
        {
            Mapper = mapper;
            TempDataFactory = tempDataFactory;
            ViewEngine = viewEngine;
            Logger = logger ?? LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<BaseController>();
        }

        /// <summary>
        /// 페이지 번호를 검증하고 유효한 값으로 변환합니다.
        /// </summary>
        /// <param name="page">페이지 번호</param>
        /// <returns>유효한 페이지 번호</returns>
        protected int ValidatePageNumber(int page)
        {
            return Math.Max(1, page);
        }

        /// <summary>
        /// 총 페이지 수를 계산합니다.
        /// </summary>
        /// <param name="totalItems">총 아이템 수</param>
        /// <param name="pageSize">페이지 크기</param>
        /// <returns>총 페이지 수</returns>
        protected int CalculateTotalPages(int totalItems, int pageSize)
        {
            return (int)Math.Ceiling(totalItems / (double)pageSize);
        }

        /// <summary>
        /// 페이징 컴포넌트를 동적으로 렌더링하기 위한 공통 메서드
        /// </summary>
        /// <param name="currentPage">현재 페이지 번호</param>
        /// <param name="totalPages">전체 페이지 수</param>
        /// <param name="pageSize">페이지당 항목 수</param>
        /// <param name="totalItems">전체 항목 수</param>
        /// <param name="actionName">페이징 처리할 액션명</param>
        /// <param name="controllerName">페이징 처리할 컨트롤러명</param>
        /// <param name="searchTerm">검색어</param>
        /// <param name="searchFields">검색 필드</param>
        /// <returns>페이징 컴포넌트 HTML</returns>
        protected IActionResult RenderPaginationComponent(
            int currentPage,
            int totalPages,
            int pageSize,
            int totalItems,
            string actionName,
            string controllerName,
            string? searchTerm = null,
            string? searchFields = null)
        {
            try
            {
                var pagingModel = new PaginationViewModel
                {
                    CurrentPage = currentPage,
                    TotalPages = totalPages,
                    PageSize = pageSize,
                    TotalItems = totalItems,
                    ActionName = actionName,
                    ControllerName = controllerName
                };

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    pagingModel.RouteData["searchTerm"] = searchTerm;
                }

                if (!string.IsNullOrEmpty(searchFields))
                {
                    pagingModel.RouteData["searchFields"] = searchFields;
                }

                // 페이징 컴포넌트 렌더링
                string paginationHtml;

                try
                {
                    paginationHtml = ViewRenderHelper.RenderViewToString(this, ViewEngine, "Views/Shared/_Pagination.cshtml", pagingModel);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "페이징 컴포넌트 렌더링 중 오류 발생");
                    return Json(new { error = true, message = $"페이징 컴포넌트를 렌더링하는 중 오류가 발생했습니다: {ex.Message}" });
                }

                return Json(new { paginationHtml });
            }
            catch (Exception ex)
            {
                // 오류 발생 시 로그 기록 및 오류 응답 반환
                Logger.LogError(ex, "페이징 렌더링 중 오류 발생");
                return Json(new { error = true, message = $"페이징 컴포넌트를 렌더링하는 중 오류가 발생했습니다: {ex.Message}" });
            }
        }

        /// <summary>
        /// 뷰 컴포넌트를 문자열로 렌더링합니다.
        /// </summary>
        /// <param name="componentName">컴포넌트 이름</param>
        /// <param name="model">모델 데이터</param>
        /// <returns>렌더링된 HTML 문자열</returns>
        protected async Task<string> RenderViewComponentToStringAsync(string componentName, object model)
        {
            try
            {
                // 간단한 구현으로 ViewRenderHelper 사용
                string html = ViewRenderHelper.RenderViewToString(this, ViewEngine, $"Views/Shared/Components/{componentName}/Default.cshtml", model);
                return await Task.FromResult(html);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"컴포넌트 '{componentName}' 렌더링 중 오류 발생");
                return $"<!-- 렌더링 오류: {ex.Message} -->";
            }
        }
    }
}