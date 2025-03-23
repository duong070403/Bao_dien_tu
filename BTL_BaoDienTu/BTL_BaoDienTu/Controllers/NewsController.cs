using Microsoft.AspNetCore.Mvc;
using BTL_BaoDienTu.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System;

public class NewsController : Controller
{
    private readonly BaoDienTuContext _context;

    public NewsController(BaoDienTuContext context)
    {
        _context = context;
    }

    // Hiển thị form tạo tin tức
    [HttpGet]
    public IActionResult Create()
    {
        ViewBag.Categories = _context.Categories
            .Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
            {
                Value = c.CategoryId.ToString(),
                Text = c.CategoryName
            })
            .ToList();

        return View();
    }

    // Xử lý lưu dữ liệu khi submit form
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(News model, IFormFile ImageFile)
    {
        // Kiểm tra hợp lệ của dữ liệu
        if (!ModelState.IsValid)
        {
            foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
            {
                Console.WriteLine("Lỗi ModelState: " + error.ErrorMessage);
            }
            ViewBag.Categories = _context.Categories
                .Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = c.CategoryId.ToString(),
                    Text = c.CategoryName
                })
                .ToList();
            return View(model);
        }

        // Kiểm tra danh mục
        if (model.CategoryId == 0)
        {
            ModelState.AddModelError("CategoryId", "Vui lòng chọn danh mục.");
            ViewBag.Categories = _context.Categories
                .Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = c.CategoryId.ToString(),
                    Text = c.CategoryName
                })
                .ToList();
            return View(model);
        }

        // Lấy UserId từ Claims (nếu có)
        var userIdClaim = User.FindFirst("UserId")?.Value;
        if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
        {
            ModelState.AddModelError("", "Lỗi xác thực người dùng.");
            return View(model);
        }
        model.AuthorId = userId;

        // Xử lý ảnh nếu có tải lên
        if (ImageFile != null && ImageFile.Length > 0)
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var fileExtension = Path.GetExtension(ImageFile.FileName).ToLower();

            if (!allowedExtensions.Contains(fileExtension))
            {
                ModelState.AddModelError("ImageFile", "Chỉ được tải lên ảnh JPG, JPEG, PNG hoặc GIF.");
                return View(model);
            }

            string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            string uniqueFileName = Guid.NewGuid().ToString() + fileExtension;
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await ImageFile.CopyToAsync(fileStream);
            }

            model.ImageUrl = "/uploads/" + uniqueFileName;
        }

        // Thêm thông tin bài viết
        model.CreatedAt = DateTime.Now;
        model.IsApproved = false;

        try
        {
            _context.News.Add(model);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index", "Home");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", "Lỗi khi lưu tin tức: " + ex.Message);
            return View(model);
        }
    }
}
