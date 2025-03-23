using Microsoft.AspNetCore.Mvc;
using BTL_BaoDienTu.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

public class HomeController : Controller
{
    private readonly BaoDienTuContext _context;

    public HomeController(BaoDienTuContext context)
    {
        _context = context;
    }

    // Hi?n th? danh s�ch tin t?c
    public async Task<IActionResult> Index()
    {
        var newsList = await _context.News
            .Include(n => n.Category) // L?y th�ng tin danh m?c
            .Include(n => n.Author)   // L?y th�ng tin t�c gi?
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        return View(newsList);
    }
}
