using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using WebBaoDienTu.Models;

namespace WebBaoDienTu.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
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
}


//dotnet aspnet-codegenerator controller -name NewsController -m News -dc BaoDienTuContext -outDir Controllers -namespace WebBaoDienTu.Controllers