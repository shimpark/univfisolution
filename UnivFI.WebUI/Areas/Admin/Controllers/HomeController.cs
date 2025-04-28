using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using UnivFI.WebUI.Models;
using UnivFI.WebUI.ViewModels;
using UnivFI.WebUI.Controllers;
using MapsterMapper;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace UnivFI.WebUI.Areas.Admin.Controllers
{
    [Area(AdminAreaConfiguration.AreaName)]
    [Authorize(Roles = "Administrators")]
    public class HomeController : BaseController
    {
        public HomeController(
            IMapper mapper,
            ITempDataDictionaryFactory tempDataFactory,
            ICompositeViewEngine viewEngine,
            ILogger<HomeController> logger)
            : base(mapper, tempDataFactory, viewEngine, logger)
        {
        }

        public IActionResult Index()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}