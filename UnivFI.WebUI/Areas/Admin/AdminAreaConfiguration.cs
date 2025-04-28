using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace UnivFI.WebUI.Areas.Admin
{
    /// <summary>
    /// Admin 영역 구성 및 상수
    /// </summary>
    public static class AdminAreaConfiguration
    {
        /// <summary>
        /// 영역 이름
        /// </summary>
        public const string AreaName = "Admin";

        /// <summary>
        /// Admin Area의 기본 컨트롤러
        /// </summary>
        public const string DefaultController = "Home";

        /// <summary>
        /// Admin Area의 기본 액션
        /// </summary>
        public const string DefaultAction = "Index";

        /// <summary>
        /// Admin Area의 라우트 이름
        /// </summary>
        public const string AdminAreaRouteName = "Admin_default";
    }
}