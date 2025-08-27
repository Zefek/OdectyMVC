using Microsoft.AspNetCore.Mvc;
using OdectyMVC.Application;
using OdectyMVC.Models;
using System.Diagnostics;

namespace OdectyMVC.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IGaugeService service;

        public HomeController(ILogger<HomeController> logger, IGaugeService service)
        {
            _logger = logger;
            this.service=service;
        }

        public async Task<IActionResult> Index()
        {
            return View(await service.GetGaugeList());
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
        public async Task<IActionResult> AddNewValue(decimal newValue, int id)
        {
            await service.AddNewValue(id, newValue);
            return RedirectToAction("Index");
        }
    }
}