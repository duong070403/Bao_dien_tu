using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebBaoDienTu.Models;

namespace WebBaoDienTu.Controllers
{
    public class NewsController : Controller
    {
        private readonly BaoDienTuContext _context;

        public NewsController(BaoDienTuContext context)
        {
            _context = context;
        }

        // GET: News
        public async Task<IActionResult> Index()
        {
            var baoDienTuContext = _context.News.Include(n => n.Author).Include(n => n.Category);
            return View(await baoDienTuContext.ToListAsync());
        }

        // GET: News/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var news = await _context.News
                .Include(n => n.Author)
                .Include(n => n.Category)
                .FirstOrDefaultAsync(m => m.NewsId == id);
            if (news == null)
            {
                return NotFound();
            }

            return View(news);
        }

        // GET: News/Create
        public IActionResult Create()
        {
            // Lấy ID của user hiện tại (VD: nếu dùng Identity)
            ViewBag.CurrentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier); 
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "CategoryName");
            
            return View();
        }

        // POST: News/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("NewsId,Title,Content,ImageUrl,AuthorId,CategoryId,IsApproved,CreatedAt")] News news, IFormFile ImageFile)
        {
    try
    {
            if (ModelState.IsValid)
            {
                // Xử lý ảnh tải lên từ file
                if (ImageFile != null && ImageFile.Length > 0)
                {
                    string fileExtension = Path.GetExtension(ImageFile.FileName).ToLower();
                    string[] allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif" };

                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        ModelState.AddModelError("ImageFile", "Chỉ được tải lên ảnh JPG, JPEG, PNG hoặc GIF.");
                        ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name");
                        return View(news);
                    }

                    string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    string uniqueFileName = Guid.NewGuid().ToString() + fileExtension;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await ImageFile.CopyToAsync(fileStream);
                    }

                    news.ImageUrl = "/uploads/" + uniqueFileName;
                }
            }
            
            // Set giá trị mặc định
            news.AuthorId = 1;
            news.CreatedAt = DateTime.Now;
            news.IsApproved = false;
                _context.Add(news);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Tin tức đã được đăng thành công!";
            return RedirectToAction(nameof(Index));
    }
    catch (Exception ex)
    {
        // Log lỗi
        ModelState.AddModelError("", "Không thể lưu dữ liệu: " + ex.Message);
    }
            ViewData["AuthorId"] = new SelectList(_context.Users, "UserId", "UserId", news.AuthorId);
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "CategoryName", news.CategoryId);
            return View(news);
        }

        // GET: News/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var news = await _context.News.FindAsync(id);
            if (news == null)
            {
                return NotFound();
            }
            ViewData["AuthorId"] = new SelectList(_context.Users, "UserId", "UserId", news.AuthorId);
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "CategoryName", news.CategoryId);
            return View(news);
        }

        // POST: News/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("NewsId,Title,Content,ImageUrl,AuthorId,CategoryId,IsApproved,CreatedAt")] News news)
        {
            ModelState.Clear();
            if (id != news.NewsId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(news);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!NewsExists(news.NewsId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["AuthorId"] = new SelectList(_context.Users, "UserId", "UserId", news.AuthorId);
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "CategoryName", news.CategoryId);
            return View(news);
        }

        // GET: News/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var news = await _context.News
                .Include(n => n.Author)
                .Include(n => n.Category)
                .FirstOrDefaultAsync(m => m.NewsId == id);
            if (news == null)
            {
                return NotFound();
            }

            return View(news);
        }

        // POST: News/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var news = await _context.News.FindAsync(id);
            if (news != null)
            {
                _context.News.Remove(news);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool NewsExists(int id)
        {
            return _context.News.Any(e => e.NewsId == id);
        }
    }
}
