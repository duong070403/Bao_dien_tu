using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using WebBaoDienTu.Models;
using Microsoft.AspNetCore.Mvc.ViewFeatures; 
using System.ComponentModel.DataAnnotations;

namespace WebBaoDienTu.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly BaoDienTuContext _context;
        private readonly ITempDataDictionaryFactory _tempDataDictionaryFactory; 

        public AuthController(BaoDienTuContext context, ITempDataDictionaryFactory tempDataDictionaryFactory) 
        {
            _context = context;
            _tempDataDictionaryFactory = tempDataDictionaryFactory;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromForm] LoginViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ." });

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == model.Email);

                if (user == null || !VerifyPasswordHash(model.Password, user.PasswordHash))
                    return BadRequest(new { success = false, message = "Email hoặc mật khẩu không đúng." });

                await SignInUserAsync(user, model.RememberMe);

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromForm] RegisterViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ." });

                if (model.Password != model.ConfirmPassword)
                    return BadRequest(new { success = false, message = "Mật khẩu xác nhận không khớp." });

                // Kiểm tra độ dài và định dạng mật khẩu
                if (model.Password.Length < 6 || model.Password.Length > 8 ||
                    !model.Password.Any(char.IsUpper) || !model.Password.Any(char.IsDigit))
                    return BadRequest(new { success = false, message = "Mật khẩu phải từ 6-8 ký tự, chứa ít nhất một chữ cái in hoa và một số." });
                if (string.IsNullOrWhiteSpace(model.FullName))
                    return BadRequest(new { success = false, message = "Họ tên không được để trống." });
                if (!await EmailExistsInRealLife(model.Email))
                    return BadRequest(new { success = false, message = "Email không tồn tại." });

                var userExists = await _context.Users.AnyAsync(u => u.Email == model.Email);
                if (userExists)
                    return BadRequest(new { success = false, message = "Email đã tồn tại trong hệ thống." });

                var user = new User
                {
                    Email = model.Email,
                    FullName = model.FullName,
                    PasswordHash = HashPassword(model.Password),
                    Role = "User",
                    CreatedAt = DateTime.Now
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Chúc mừng bạn đã đăng ký thành công!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        private async Task<bool> EmailExistsInRealLife(string email)
        {
            string apiKey = "2b48740977c743faa37dd2794057637d";

            using (var handler = new HttpClientHandler())
            {
                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true; // Bỏ qua xác thực SSL (chỉ nên sử dụng trong môi trường phát triển)
                using (var client = new HttpClient(handler))
                {
                    var response = await client.GetAsync($"https://emailvalidation.abstractapi.com/v1/?api_key={apiKey}&email={email}");
                    if (response.IsSuccessStatusCode)
                    {
                        var result = await response.Content.ReadAsStringAsync();
                        if (!string.IsNullOrEmpty(result))
                        {
                            dynamic? json = Newtonsoft.Json.JsonConvert.DeserializeObject(result);
                            return json?.deliverability == "DELIVERABLE";
                        }
                    }
                }
            }
            return false;
        }




        [HttpPost("logout")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            try
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return Ok(new { success = true }); 
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Có lỗi xảy ra khi đăng xuất: " + ex.Message });
            }
        }


        private async Task SignInUserAsync(User user, bool isPersistent)
        {
            string displayName = GetFirstName(user.FullName) ?? user.Email.Split('@')[0];

            var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                    new Claim(ClaimTypes.Name, user.Email),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role),
                    new Claim("FullName", user.FullName ?? user.Email.Split('@')[0]),
                    new Claim("DisplayName", displayName)
                };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = isPersistent, 
                    ExpiresUtc = isPersistent ? DateTime.UtcNow.AddDays(30) : null 
                });
        }

        private string GetFirstName(string fullName)
        {
            if (string.IsNullOrEmpty(fullName))
                return string.Empty;

            var parts = fullName.Trim().Split(' ');
            return parts.Length > 0 ? parts[parts.Length - 1] : fullName;
        }

        private string HashPassword(string password)
        {
            // Sử dụng SHA256 để băm mật khẩu (nên dùng BCrypt trong môi trường thực tế)
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        private bool VerifyPasswordHash(string password, string storedHash)
        {
            var hash = HashPassword(password);
            return hash == storedHash;
        }
    }

    public class LoginViewModel
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool RememberMe { get; set; }
    }

    public class RegisterViewModel
    {
        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        [StringLength(8, MinimumLength = 6, ErrorMessage = "Mật khẩu phải từ 6-8 ký tự.")]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*\d).+$", ErrorMessage = "Mật khẩu phải chứa ít nhất một chữ cái in hoa và một số.")]
        public string Password { get; set; } = string.Empty;

        public string ConfirmPassword { get; set; } = string.Empty;
    }
}