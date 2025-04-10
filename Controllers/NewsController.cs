using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebBaoDienTu.Models;

namespace WebBaoDienTu.Controllers
{
    public class NewsController(BaoDienTuContext context, ILogger<NewsController> logger) : Controller
    {
        private readonly BaoDienTuContext _context = context;
        private readonly ILogger<NewsController> _logger = logger;

        #region Public Views
        // GET: News
        public async Task<IActionResult> Index(
            string title,
            string author,
            DateTime? date,
            int? categoryId, 
            string approvalStatus, 
            DateTime? startDate, 
            DateTime? endDate) 
        {
            try
            {
                // Convert Categories to SelectListItems for the dropdown
                var categories = await _context.Categories.ToListAsync();
                ViewBag.Categories = categories.Select(c => new SelectListItem
                {
                    Value = c.CategoryId.ToString(),
                    Text = c.CategoryName
                }).ToList();

                ViewBag.HideNavElements = true;

                var query = _context.News
                    .Include(n => n.Author)
                    .Include(n => n.Category)
                    .Where(n => !n.IsDeleted && (!n.IsArchived || n.IsApproved))
                    .AsQueryable();

                // Apply title filter
                if (!string.IsNullOrEmpty(title))
                    query = query.Where(n => n.Title.Contains(title));

                if (!string.IsNullOrEmpty(author))
                    query = query.Where(n => n.Author.FullName.Contains(author));

                if (date.HasValue)
                    query = query.Where(n => n.CreatedAt.Date == date.Value.Date);

                if (categoryId.HasValue && categoryId > 0)
                    query = query.Where(n => n.CategoryId == categoryId.Value);

                if (!string.IsNullOrEmpty(approvalStatus))
                {
                    if (approvalStatus == "approved")
                        query = query.Where(n => n.IsApproved);
                    else if (approvalStatus == "pending")
                        query = query.Where(n => !n.IsApproved);
                }

                if (startDate.HasValue && endDate.HasValue)
                {
                    var endDateAdjusted = endDate.Value.AddDays(1).AddSeconds(-1); 
                    query = query.Where(n => n.CreatedAt >= startDate.Value && n.CreatedAt <= endDateAdjusted);
                }

                var newsItems = await query
                    .OrderByDescending(n => !n.IsApproved) 
                    .ThenByDescending(n => n.CreatedAt)    
                    .ToListAsync();


                ViewData["TitleFilter"] = title;
                ViewData["AuthorFilter"] = author;
                ViewData["DateFilter"] = date?.ToString("yyyy-MM-dd");
                ViewData["CategoryFilter"] = categoryId?.ToString();
                ViewData["ApprovalStatusFilter"] = approvalStatus;
                ViewData["StartDateFilter"] = startDate?.ToString("yyyy-MM-dd");
                ViewData["EndDateFilter"] = endDate?.ToString("yyyy-MM-dd");

                return View("Index", newsItems);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Index action");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách tin tức.";
                return View("List", new List<News>());
            }
        }


        // GET: News/GetNewsDetails/5
        public async Task<IActionResult> GetNewsDetailsById(int id)
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

        // GET: News/Details/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Details(int? id)
        {
            await Task.CompletedTask;
            return RedirectToAction(nameof(Index), new { showDetails = id });
        }

        // GET: News/GetNewsDetailsForDelete/5
        [Authorize(Roles = "User,Admin")]
        public async Task<IActionResult> GetNewsDetailsForDelete(int id)
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
                    title = news.Title,
                    createdAt = news.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                    authorFullName = news.Author.FullName,
                    imageUrl = news.ImageUrl
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetNewsDetailsForDelete for ID: {Id}", id);
                return Json(new { error = "Không thể tải dữ liệu tin tức." });
            }
        }


        // GET: News/Create
        [Authorize(Roles = "User,Admin")]
        public IActionResult Create()
        {
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "CategoryName");
            ViewBag.HideNavElements = true;
            return View();
        }

        // POST: News/Create
        [Authorize(Roles = "User,Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Content,ImageUrl,CategoryId")] News news, IFormFile? ImageFile)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int authorId))
                {
                    ModelState.AddModelError("", "Không thể xác định người dùng hiện tại.");
                    ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "CategoryName");
                    return View(news);
                }

                news.AuthorId = authorId;
                news.CreatedAt = DateTime.Now;
                news.IsApproved = User.IsInRole("Admin");
                news.IsDeleted = false;
                news.IsArchived = false;

                if (string.IsNullOrEmpty(news.Title) || string.IsNullOrEmpty(news.Content) || news.CategoryId <= 0)
                {
                    if (string.IsNullOrEmpty(news.Title))
                        ModelState.AddModelError("Title", "Tiêu đề không được để trống.");

                    if (string.IsNullOrEmpty(news.Content))
                        ModelState.AddModelError("Content", "Nội dung không được để trống.");

                    if (news.CategoryId <= 0)
                        ModelState.AddModelError("CategoryId", "Vui lòng chọn danh mục.");

                    ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "CategoryName");
                    return View(news);
                }

                // Prioritize handling the uploaded image file
                if (ImageFile != null && ImageFile.Length > 0)
                {
                    string? imageUrl = await ProcessUploadedImage(ImageFile);
                    if (!string.IsNullOrEmpty(imageUrl))
                    {
                        news.ImageUrl = imageUrl;
                    }
                    else
                    {
                        ModelState.AddModelError("ImageFile", "Có lỗi khi xử lý hình ảnh.");
                        ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "CategoryName");
                        return View(news);
                    }
                }
                else if (!string.IsNullOrEmpty(news.ImageUrl)) // Handle image URL only if no file is uploaded
                {
                    string? downloadedImageUrl = await DownloadAndSaveImageFromUrl(news.ImageUrl);
                    if (string.IsNullOrEmpty(downloadedImageUrl))
                    {
                        return Json(new
                        {
                            success = false,
                            message = "Không thể tải ảnh từ URL này. Vui lòng kiểm tra lại URL hoặc tải lên ảnh trực tiếp."
                        });
                    }
                    news.ImageUrl = downloadedImageUrl;
                }

                _context.Add(news);
                await _context.SaveChangesAsync();
                return Json(new
                {
                    success = true,
                    message = "Tin tức đã được đăng thành công!",
                    redirectUrl = Url.Action(User.IsInRole("Admin") ? "Index" : "MyNews")
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating news");
                return Json(new { success = false, message = "Có lỗi xảy ra khi đăng tin." });
            }
        }

        private async Task<string?> ProcessUploadedImage(IFormFile imageFile)
        {
            try
            {
                if (imageFile.Length > 0)
                {
                    var filePath = Path.Combine("wwwroot/images", Path.GetRandomFileName());
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }
                    return filePath.Replace("wwwroot", string.Empty).Replace("\\", "/");
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing uploaded image");
                return null;
            }
        }

        private async Task<string?> DownloadAndSaveImageFromUrl(string imageUrl)
        {
            try
            {
                using var httpClient = new HttpClient();
                var response = await httpClient.GetAsync(imageUrl);

                if (!response.IsSuccessStatusCode)
                    return null;

                var fileName = Path.GetRandomFileName();
                var filePath = Path.Combine("wwwroot/images", fileName);

                await using var fileStream = new FileStream(filePath, FileMode.Create);
                await response.Content.CopyToAsync(fileStream);

                return filePath.Replace("wwwroot", string.Empty).Replace("\\", "/");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading and saving image from URL: {ImageUrl}", imageUrl);
                return null;
            }
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
        #endregion
    }
}
