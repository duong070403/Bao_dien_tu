using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WebBaoDienTu.Models;

namespace WebBaoDienTu.Controllers
{
    public class NewsController : Controller
    {
        private readonly BaoDienTuContext _context;
        private readonly ILogger<NewsController> _logger;

        public NewsController(BaoDienTuContext context, ILogger<NewsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: News/Read/5
        public async Task<IActionResult> Read(int? id)
        {
            try
            {
                ViewBag.Categories = await _context.Categories.ToListAsync();

                if (id == null)
                    return NotFound();

                var news = await _context.News
                    .Include(n => n.Author)
                    .Include(n => n.Category)
                    .FirstOrDefaultAsync(m => m.NewsId == id);

                if (news == null)
                    return NotFound();

                return View(news);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Read action for ID: {Id}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải nội dung tin tức.";
                return RedirectToAction("Index", "Home");
            }
        }

        // GET: News/Category/5
        public async Task<IActionResult> Category(int id)
        {
            try
            {
                ViewBag.Categories = await _context.Categories.ToListAsync();
                var news = await _context.News
                    .Include(n => n.Author)
                    .Include(n => n.Category)
                    .Where(n => n.CategoryId == id && n.IsApproved)
                    .ToListAsync();

                return View("Index", news);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Category action for ID: {Id}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách tin tức theo danh mục.";
                return RedirectToAction("Index", "Home");
            }
        }

        // GET: News/GetNewsDetails/5
        public async Task<IActionResult> GetNewsDetails(int id)
        {
            try
            {
                var news = await _context.News
                    .Include(n => n.Author)
                    .Include(n => n.Category)
                    .FirstOrDefaultAsync(m => m.NewsId == id);

                if (news == null)
                    return NotFound();

                return Json(new
                {
                    success = true,
                    newsId = news.NewsId,
                    title = news.Title,
                    content = news.Content,
                    createdAt = news.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                    authorFullName = news.Author.FullName,
                    imageUrl = news.ImageUrl,
                    categoryName = news.Category.CategoryName,
                    isApproved = news.IsApproved
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetNewsDetails for ID: {Id}", id);
                return Json(new { success = false, message = "Không thể tải chi tiết tin tức." });
            }
        }
    }
}
