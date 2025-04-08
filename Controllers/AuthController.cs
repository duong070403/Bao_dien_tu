using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using WebBaoDienTu.Models;
using Microsoft.AspNetCore.Mvc.ViewFeatures; 
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;

namespace WebBaoDienTu.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly BaoDienTuContext _context;
        private readonly ILogger<AuthController> _logger;
        private readonly ITempDataDictionaryFactory _tempDataDictionaryFactory; 

        public AuthController(BaoDienTuContext context, ILogger<AuthController> logger, ITempDataDictionaryFactory tempDataDictionaryFactory) 
        {
            _context = context;
            _logger = logger;
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
            // TEMPORARY MODIFICATION: Bypass real email verification with Google API
            // Only perform basic email format validation

            await Task.CompletedTask; // Temporary await to suppress warning
            // Check if the email has a valid basic format
            if (string.IsNullOrWhiteSpace(email) || !email.Contains("@") || !email.Contains("."))
            {
                return false;
            }

            // Split the email into parts
            var parts = email.Split('@');
            if (parts.Length != 2)
            {
                return false;
            }

            var domain = parts[1];

            // Check if domain has at least one dot and ends with a valid TLD
            if (!domain.Contains('.') || domain.EndsWith("."))
            {
                return false;
            }

            // Basic format is valid, consider it deliverable
            _logger.LogWarning("NOTICE: Real email verification is temporarily disabled. Using basic format validation only.");
            return true;

            /* ORIGINAL CODE - COMMENTED OUT
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
            */
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

        [HttpPost("changePassword")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromForm] ChangePasswordModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Thông tin không hợp lệ." });
            }

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int id))
                {
                    return Unauthorized(new { success = false, message = "Bạn cần đăng nhập lại." });
                }

                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return NotFound(new { success = false, message = "Người dùng không tồn tại." });
                }

                // Verify current password
                if (user.PasswordHash != HashPassword(model.CurrentPassword))
                {
                    return BadRequest(new { success = false, message = "Mật khẩu hiện tại không đúng." });
                }

                // Check if new password is the same as current password
                if (HashPassword(model.NewPassword) == user.PasswordHash)
                {
                    return BadRequest(new { success = false, message = "Mật khẩu mới không được trùng với mật khẩu hiện tại." });
                }

                // Validate new password
                var passwordRegex = new System.Text.RegularExpressions.Regex(@"^(?=.*[A-Z])(?=.*\d).{6,8}$");
                if (!passwordRegex.IsMatch(model.NewPassword))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Mật khẩu mới phải từ 6-8 ký tự, chứa ít nhất một chữ cái in hoa và một số."
                    });
                }

                // Confirm passwords match
                if (model.NewPassword != model.ConfirmPassword)
                {
                    return BadRequest(new { success = false, message = "Xác nhận mật khẩu không khớp." });
                }

                user.PasswordHash = HashPassword(model.NewPassword);
                _context.Update(user);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Mật khẩu đã được cập nhật thành công." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user");
                return StatusCode(500, new { success = false, message = "Có lỗi xảy ra khi thay đổi mật khẩu." });
            }
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
            return Convert.ToBase64String(
                System.Security.Cryptography.SHA256.Create()
                .ComputeHash(System.Text.Encoding.UTF8.GetBytes(password)));
        }


        private bool VerifyPasswordHash(string password, string storedHash)
        {
            var hash = HashPassword(password);
            return hash == storedHash;
        }
    }

    public class ChangePasswordModel
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
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