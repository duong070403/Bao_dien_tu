using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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

        #region Public Views
        // GET: News
        // GET: News
        public async Task<IActionResult> Index(
            string title,
            string author,
            DateTime? date,
            int? categoryId, // Add this parameter to accept category filtering
            string approvalStatus, // Add this parameter to handle approval status filtering
            DateTime? startDate, // Add this for date range filtering
            DateTime? endDate) // Add this for date range filtering
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

                // Apply author filter
                if (!string.IsNullOrEmpty(author))
                    query = query.Where(n => n.Author.FullName.Contains(author));

                // Apply simple date filter
                if (date.HasValue)
                    query = query.Where(n => n.CreatedAt.Date == date.Value.Date);

                // Apply category filter
                if (categoryId.HasValue && categoryId > 0)
                    query = query.Where(n => n.CategoryId == categoryId.Value);

                // Apply approval status filter
                if (!string.IsNullOrEmpty(approvalStatus))
                {
                    if (approvalStatus == "approved")
                        query = query.Where(n => n.IsApproved);
                    else if (approvalStatus == "pending")
                        query = query.Where(n => !n.IsApproved);
                }

                // Apply date range filter
                if (startDate.HasValue && endDate.HasValue)
                {
                    var endDateAdjusted = endDate.Value.AddDays(1).AddSeconds(-1); // End of the selected day
                    query = query.Where(n => n.CreatedAt >= startDate.Value && n.CreatedAt <= endDateAdjusted);
                }

                var newsItems = await query
                    .OrderByDescending(n => !n.IsApproved) // Pending items first
                    .ThenByDescending(n => n.CreatedAt)    // Then by date
                    .ToListAsync();

                // Store filter values in ViewData to maintain state in the view
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
                return View(new List<News>());
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

                // Handle image file upload
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
                // Handle image URL
                if (!string.IsNullOrEmpty(news.ImageUrl))
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

        // GET: News/MyNews
        [Authorize(Roles = "User")]
        public async Task<IActionResult> MyNews()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int authorId))
                    return RedirectToAction("Login", "Account");

                var myNews = await _context.News
                    .Where(n => n.AuthorId == authorId && !n.IsDeleted)
                    .Include(n => n.Category)
                    .OrderByDescending(n => n.CreatedAt)
                    .ToListAsync();

                ViewBag.HideNavElements = true;
                return View(myNews);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in MyNews action");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách tin tức của bạn.";
                return RedirectToAction("Index", "Home");
            }
        }

        // GET: News/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            try
            {
                if (id == null)
                    return NotFound();

                var news = await _context.News.FindAsync(id);
                if (news == null)
                    return NotFound();

                ViewData["IsEdit"] = true;
                ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "CategoryName", news.CategoryId);
                return View("Create", news);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Edit GET action for ID: {Id}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải trang chỉnh sửa tin tức.";
                return RedirectToAction(nameof(Index));
            }
        }

        #endregion
        #region Form Submissions

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("NewsId,Title,Content,ImageUrl,AuthorId,CategoryId,IsApproved,CreatedAt,IsDeleted,IsArchived")] News news, IFormFile? ImageFile)
        {
            try
            {
                if (id != news.NewsId)
                    return NotFound();

                _logger.LogInformation("Received data: NewsId={NewsId}, Title='{Title}', AuthorId={AuthorId}, CategoryId={CategoryId}",
                    news.NewsId, news.Title, news.AuthorId, news.CategoryId);

                ViewData["IsEdit"] = true;
                ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "CategoryName", news.CategoryId);

                if (!string.IsNullOrEmpty(news.Title) && !string.IsNullOrEmpty(news.Content) && news.CategoryId > 0)
                {
                    var existingNews = await _context.News.AsNoTracking().FirstOrDefaultAsync(n => n.NewsId == id);
                    if (existingNews == null)
                        return NotFound();

                    string? originalImageUrl = existingNews.ImageUrl;

                    // Handle image file upload
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
                            return View("Create", news);
                        }
                    }
                    // Handle image URL - only if it's different from the original
                    else if (!string.IsNullOrEmpty(news.ImageUrl) && news.ImageUrl != originalImageUrl)
                    {
                        string? downloadedImageUrl = await DownloadAndSaveImageFromUrl(news.ImageUrl);
                        if (!string.IsNullOrEmpty(downloadedImageUrl))
                        {
                            news.ImageUrl = downloadedImageUrl;
                        }
                        else
                        {
                            ModelState.AddModelError("ImageUrl", "Không thể tải ảnh từ URL này. Vui lòng kiểm tra lại URL hoặc tải lên ảnh trực tiếp.");
                            return View("Create", news);
                        }
                    }
                    else if (string.IsNullOrEmpty(news.ImageUrl))
                    {
                        // Keep the original image
                        news.ImageUrl = originalImageUrl;
                    }

                    try
                    {
                        _context.Update(news);
                        await _context.SaveChangesAsync();

                        return Json(new { success = true, message = "Tin tức đã được cập nhật thành công!", redirectUrl = Url.Action(nameof(Index)) });
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        if (!NewsExists(news.NewsId))
                            return NotFound();
                        else
                            throw;
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(news.Title))
                        ModelState.AddModelError("Title", "Tiêu đề không được để trống.");

                    if (string.IsNullOrEmpty(news.Content))
                        ModelState.AddModelError("Content", "Nội dung không được để trống.");

                    if (news.CategoryId <= 0)
                        ModelState.AddModelError("CategoryId", "Vui lòng chọn danh mục.");

                    TempData["ErrorMessage"] = "Vui lòng điền đầy đủ các trường bắt buộc.";
                    return View("Create", news);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Edit POST action for ID: {Id}", id);
                return Json(new { success = false, message = "Có lỗi xảy ra khi cập nhật tin tức." });
            }
        }

        // POST: News/Delete/5
        [Authorize(Roles = "User,Admin")]
        [HttpPost]
        [Route("News/Delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var newsItem = await _context.News.FindAsync(id);
                if (newsItem == null)
                    return Json(new { success = false, message = "Xóa thất bại: Tin tức không tồn tại" });

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!User.IsInRole("Admin") && (!int.TryParse(userId, out int authorId) || newsItem.AuthorId != authorId))
                    return Forbid();

                newsItem.IsDeleted = true;
                _context.Update(newsItem);
                await _context.SaveChangesAsync();

                _logger.LogInformation("News marked as deleted, ID: {NewsId}", newsItem.NewsId);
                return Json(new { success = true, message = "Xóa thành công" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Delete action for ID: {Id}", id);
                return Json(new { success = false, message = "Có lỗi xảy ra khi xóa tin tức." });
            }
        }

        // POST: News/DeleteConfirmed/5
        [Authorize(Roles = "User,Admin")]
        [HttpPost]
        [Route("News/DeleteConfirmed/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var news = await _context.News.FindAsync(id);
                if (news == null)
                    return Json(new { success = false, message = "Không tìm thấy tin tức" });

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!User.IsInRole("Admin") && (!int.TryParse(userId, out int authorId) || news.AuthorId != authorId))
                    return Forbid();

                if (User.IsInRole("Admin"))
                {
                    _context.News.Remove(news);
                    _logger.LogInformation("News permanently deleted by admin, ID: {NewsId}", news.NewsId);
                }
                else
                {
                    news.IsDeleted = true;
                    _context.Update(news);
                    _logger.LogInformation("News marked as deleted by user, ID: {NewsId}", news.NewsId);
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Xóa tin tức thành công" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteConfirmed action for ID: {Id}", id);
                return Json(new { success = false, message = "Có lỗi xảy ra khi xóa tin tức." });
            }
        }

        // POST: News/Approve/5
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Approve(int id)
        {
            try
            {
                var news = await _context.News.FindAsync(id);
                if (news == null)
                    return Json(new { success = false, message = "Không tìm thấy tin tức" });

                news.IsApproved = true;
                _context.Update(news);
                await _context.SaveChangesAsync();

                _logger.LogInformation("News approved, ID: {NewsId}", news.NewsId);
                return Json(new { success = true, message = "Tin tức đã được duyệt thành công." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Approve action for ID: {Id}", id);
                return Json(new { success = false, message = "Có lỗi xảy ra khi duyệt tin tức." });
            }
        }


        // POST: News/Archive/5
        [Authorize(Roles = "User")]
        [HttpPost]
        public async Task<IActionResult> Archive(int id)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int authorId))
                    return RedirectToAction("Login", "Account");

                var newsItem = await _context.News.FirstOrDefaultAsync(n => n.NewsId == id && n.AuthorId == authorId);
                if (newsItem != null && !newsItem.IsApproved)
                {
                    newsItem.IsArchived = true;
                    _context.Update(newsItem);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("News archived, ID: {NewsId}", newsItem.NewsId);
                    TempData["SuccessMessage"] = "Tin tức đã được lưu trữ.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể lưu trữ tin tức này.";
                }

                return RedirectToAction("MyNews");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Archive action for ID: {Id}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi lưu trữ tin tức.";
                return RedirectToAction("MyNews");
            }
        }

        // POST: News/Repost/5
        [Authorize(Roles = "User")]
        [HttpPost]
        public async Task<IActionResult> Repost(int id)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int authorId))
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập lại." });
                }

                var newsItem = await _context.News.FirstOrDefaultAsync(n => n.NewsId == id && n.AuthorId == authorId);
                if (newsItem != null)
                {
                    newsItem.IsApproved = false;
                    newsItem.IsDeleted = false;
                    newsItem.IsArchived = false;
                    newsItem.CreatedAt = DateTime.Now;

                    _context.Update(newsItem);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("News reposted, ID: {NewsId}", newsItem.NewsId);
                    return Json(new { success = true, message = "Đăng lại thành công" });
                }

                return Json(new { success = false, message = "Đăng lại thất bại: Tin tức không tồn tại hoặc bạn không có quyền." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Repost action for ID: {Id}", id);
                return Json(new { success = false, message = "Có lỗi xảy ra khi đăng lại tin tức." });
            }
        }

        #endregion
        #region Helper Methods

        private bool NewsExists(int id)
        {
            return _context.News.Any(e => e.NewsId == id);
        }

        private void ValidateNewsData(News news)
        {
            if (string.IsNullOrEmpty(news.Title))
            {
                ModelState.AddModelError("Title", "Tiêu đề không được để trống.");
            }

            if (string.IsNullOrEmpty(news.Content))
            {
                ModelState.AddModelError("Content", "Nội dung không được để trống.");
            }

            if (news.CategoryId <= 0)
            {
                ModelState.AddModelError("CategoryId", "Vui lòng chọn danh mục.");
            }
        }

        private async Task<string?> ProcessUploadedImage(IFormFile? imageFile)
        {
            if (imageFile == null || imageFile.Length == 0)
            {
                return null;
            }

            try
            {
                string fileExtension = Path.GetExtension(imageFile.FileName).ToLower();
                string[] allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif" };

                if (!allowedExtensions.Contains(fileExtension))
                {
                    ModelState.AddModelError("ImageFile", "Chỉ được tải lên ảnh JPG, JPEG, PNG hoặc GIF.");
                    return null;
                }

                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                string uniqueFileName = Guid.NewGuid().ToString() + fileExtension;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(fileStream);
                }

                return "/images/" + uniqueFileName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing image upload");
                return null;
            }
        }

        private async Task<string?> DownloadAndSaveImageFromUrl(string? imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl))
            {
                return null;
            }

            try
            {
                // Check if it's a base64 image
                if (imageUrl.StartsWith("data:image"))
                {
                    return SaveBase64Image(imageUrl);
                }

                // Check if URL is valid
                if (!Uri.TryCreate(imageUrl, UriKind.Absolute, out Uri? uri) ||
                    uri == null || (uri.Scheme != "http" && uri.Scheme != "https"))
                {
                    _logger.LogWarning("Invalid image URL format: {ImageUrl}", imageUrl);
                    return null;
                }

                // Rest of your existing HTTP download code
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(10);
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 WebBaoDienTuApp");

                // Try to get head first to check content type
                try
                {
                    var headResponse = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, uri));
                    if (!headResponse.IsSuccessStatusCode ||
                        headResponse.Content.Headers.ContentType == null ||
                        (headResponse.Content.Headers.ContentType != null &&
                         !headResponse.Content.Headers.ContentType.MediaType?.StartsWith("image/") == true))
                    {
                        _logger.LogWarning("URL does not point to a valid image: {ImageUrl}", imageUrl);
                        return null;
                    }
                }
                catch
                {
                    // Some servers don't support HEAD requests, continue anyway
                }



                // Download the image
                byte[] imageData = await httpClient.GetByteArrayAsync(uri);
                if (imageData.Length == 0)
                {
                    _logger.LogWarning("Downloaded image has zero length: {ImageUrl}", imageUrl);
                    return null;
                }

                // Determine file extension based on content or URL
                string fileExtension = ".jpg"; // Default

                // Try to get extension from URL
                string urlExtension = Path.GetExtension(uri.AbsolutePath).ToLower();
                if (!string.IsNullOrEmpty(urlExtension) &&
                    new[] { ".jpg", ".jpeg", ".png", ".gif" }.Contains(urlExtension))
                {
                    fileExtension = urlExtension;
                }

                return SaveImageData(imageData, fileExtension);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error downloading image from URL: {ImageUrl}, Status: {Status}",
                    imageUrl, ex.StatusCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading image from URL: {ImageUrl}", imageUrl);
                return null;
            }
        }

        private string? SaveBase64Image(string base64String)
        {
            try
            {
                // Extract the actual base64 data and content type
                string[] parts = base64String.Split(',');
                if (parts.Length != 2)
                {
                    _logger.LogWarning("Invalid base64 image format");
                    return null;
                }

                // Get the content type from the data URI
                string[] contentTypeParts = parts[0].Split(':');
                if (contentTypeParts.Length < 2)
                {
                    _logger.LogWarning("Invalid base64 content type format");
                    return null;
                }

                string contentTypeWithExtra = contentTypeParts[1];
                string[] contentTypeSplits = contentTypeWithExtra.Split(';');
                string contentType = contentTypeSplits[0].Trim();

                // Determine file extension from content type
                string fileExtension = ".jpg"; // Default
                switch (contentType.ToLower())
                {
                    case "image/png":
                        fileExtension = ".png";
                        break;
                    case "image/gif":
                        fileExtension = ".gif";
                        break;
                    case "image/jpeg":
                    case "image/jpg":
                        fileExtension = ".jpg";
                        break;
                    default:
                        if (!contentType.StartsWith("image/"))
                        {
                            _logger.LogWarning("Invalid image content type: {ContentType}", contentType);
                            return null;
                        }
                        break;
                }

                // Convert base64 to byte array
                byte[] imageData = Convert.FromBase64String(parts[1]);

                // Save the image data to a file
                return SaveImageData(imageData, fileExtension);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing base64 image");
                return null;
            }
        }


        private string? SaveImageData(byte[] imageData, string fileExtension)
        {
            try
            {
                // Generate a unique filename
                string fileName = Guid.NewGuid().ToString() + fileExtension;
                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");

                // Ensure the directory exists
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                string filePath = Path.Combine(uploadsFolder, fileName);

                // Save the image data
                System.IO.File.WriteAllBytes(filePath, imageData);

                // Return the relative path to be stored in the database
                return "/images/" + fileName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving image data");
                return null;
            }
        }
        #endregion
    }
}
