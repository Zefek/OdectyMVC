using Microsoft.AspNetCore.Mvc;
using OdectyMVC.Application;
using OdectyMVC.Models;
using System.Diagnostics;

namespace OdectyMVC.Controllers
{
#if !DEBUG
    [Authorize]
#endif
    public class HomeController : Controller
    {
        private readonly IGaugeService service;

        public HomeController(IGaugeService service)
        {
            this.service = service;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            return View(await service.GetGaugeList(cancellationToken));
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpPost]
        [Route("Home/Index", Name = "AddNewValue")]
        public async Task<IActionResult> AddNewValue(decimal newValue, int id, CancellationToken cancellationToken)
        {
            await service.AddNewValue(id, newValue, cancellationToken);
            return RedirectToAction("Index");
        }
    }
}