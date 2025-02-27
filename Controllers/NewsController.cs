using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BTL_Xay_dung_bao_dien_tu.Models;
using BTL_Xay_dung_bao_dien_tu.Models.ViewModels;
using System.Linq;
using System.Threading.Tasks;

namespace BTL_Xay_dung_bao_dien_tu.Controllers
{
    public class NewsController : Controller
    {
        private readonly Db2Context _context;

        public NewsController(Db2Context context)
        {
            _context = context;
        }

        // API lấy danh sách tin chưa duyệt
        [HttpGet]
        public async Task<IActionResult> PendingNews()
        {
            var newsList = await _context.News
                .Where(n => n.IsApproved == false || n.IsApproved == null)
                .Include(n => n.Author)
                .Include(n => n.Category)
                .Select(n => new NewsApprovalViewModel
                {
                    NewsId = n.NewsId,
                    Title = n.Title,
                    Content = n.Content,
                    AuthorName = n.Author.FullName,
                    CategoryName = n.Category.CategoryName,
                    IsApproved = n.IsApproved,
                    CreatedAt = n.CreatedAt
                })
                .ToListAsync();

            return Json(newsList);
        }

        // API Duyệt tin
        [HttpPost]
        public async Task<IActionResult> ApproveNews(int id)
        {
            var news = await _context.News.FindAsync(id);
            if (news == null)
            {
                return NotFound(new { success = false, message = "Không tìm thấy tin tức." });
            }

            news.IsApproved = true;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Tin đã được duyệt!" });
        }

        // API Xóa tin
        [HttpPost]
        public async Task<IActionResult> DeleteNews(int id)
        {
            var news = await _context.News.FindAsync(id);
            if (news == null)
            {
                return NotFound(new { success = false, message = "Không tìm thấy tin tức." });
            }

            _context.News.Remove(news);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Tin đã bị xóa!" });
        }
    }
}
