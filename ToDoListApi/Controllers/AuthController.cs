using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ToDoListApi.Data;
using ToDoListApi.Dtos;
using ToDoListApi.Models;
using ToDoListApi.Services;

namespace ToDoListApi.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly PasswordHasher<User> _passwordHasher;
        private readonly IConfiguration _configuration;

        private readonly EmailService _emailService;

        public AuthController(AppDbContext context, IConfiguration configuration, EmailService emailService)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<User>();
            _configuration = configuration;
            _emailService = emailService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            var normalizedEmail = dto.Email.Trim().ToLowerInvariant();

            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == normalizedEmail);

            if (existingUser != null)
            {
                return BadRequest(new { message = "Bu e-posta ile zaten bir hesap var." });
            }

            var tempUser = new User(normalizedEmail, string.Empty);
            var hashedPassword = _passwordHasher.HashPassword(tempUser, dto.Password);
            tempUser.SetPasswordHash(hashedPassword);

            var verificationToken = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32))
                .Replace("+", "-").Replace("/", "_").Replace("=", "");
            tempUser.SetEmailVerificationToken(verificationToken, DateTime.UtcNow.AddHours(24));

            _context.Users.Add(tempUser);
            await _context.SaveChangesAsync();

            var verificationLink = $"https://chamos.site/verify-email?token={verificationToken}";
            var emailBody = $@"
                <h1>ChaMos'a Hoş Geldin!</h1>
                <p>Hesabını doğrulamak için aşağıdaki linke tıkla:</p>
                <p><a href='{verificationLink}'>Hesabımı Doğrula</a></p>
                <p>Bu link 24 saat geçerlidir.</p>
            ";

            await _emailService.SendEmailAsync(normalizedEmail, "ChaMos - Hesabını Doğrula", emailBody);

            return Ok(new { message = "Kayıt başarılı. Lütfen email'ini kontrol edip hesabını doğrula." });
        }

        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromQuery] string token)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.EmailVerificationToken == token);

            if (user == null)
            {
                return BadRequest(new { message = "Geçersiz veya süresi dolmuş doğrulama linki." });
            }

            if (user.EmailVerificationTokenExpiresAt.HasValue && user.EmailVerificationTokenExpiresAt.Value < DateTime.UtcNow)
            {
                return BadRequest(new { message = "Geçersiz veya süresi dolmuş doğrulama linki." });
            }

            user.MarkEmailAsVerified();
            await _context.SaveChangesAsync();

            return Ok(new { message = "Email başarıyla doğrulandı!" });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto)
        {
            var normalizedEmail = dto.Email.Trim().ToLowerInvariant();

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == normalizedEmail);

            // Madde 2: kullanıcı var mı yok mu, hiçbir zaman belli etmiyoruz
            if (user != null)
            {
                var resetToken = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32))
                    .Replace("+", "-").Replace("/", "_").Replace("=", "");
                user.SetPasswordResetToken(resetToken, DateTime.UtcNow.AddHours(1));
                await _context.SaveChangesAsync();

                var resetLink = $"https://chamos.site/reset-password?token={resetToken}";
                var emailBody = $@"
                    <h1>Şifre Sıfırlama</h1>
                    <p>Şifreni sıfırlamak için aşağıdaki linke tıkla:</p>
                    <p><a href='{resetLink}'>Şifremi Sıfırla</a></p>
                    <p>Bu link 1 saat geçerlidir. Bu isteği sen yapmadıysan, bu emaili görmezden gelebilirsin.</p>
                ";

                await _emailService.SendEmailAsync(normalizedEmail, "ChaMos - Şifre Sıfırlama", emailBody);
            }

            return Ok(new { message = "Eğer bu email adresi kayıtlıysa, şifre sıfırlama linki gönderildi." });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.PasswordResetToken == dto.Token);

            if (user == null || user.PasswordResetTokenExpiresAt < DateTime.UtcNow)
            {
                return BadRequest(new { message = "Geçersiz veya süresi dolmuş sıfırlama linki." });
            }

            var hashedPassword = _passwordHasher.HashPassword(user, dto.NewPassword);
            user.ResetPassword(hashedPassword);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Şifren başarıyla güncellendi. Şimdi giriş yapabilirsin." });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var normalizedEmail = dto.Email.Trim().ToLowerInvariant();

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == normalizedEmail);

            if (user == null)
            {
                return Unauthorized(new { message = "E-posta veya parola hatalı." });
            }

            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);

            if (result == PasswordVerificationResult.Failed)
            {
                return Unauthorized(new { message = "E-posta veya parola hatalı." });
            }

            if (!user.IsEmailVerified)
            {
                return Unauthorized(new { message = "Lütfen giriş yapmadan önce email adresinizi doğrulayın." });
            }

            var accessToken = GenerateJwtToken(user);
            var refreshToken = await GenerateAndSaveRefreshTokenAsync(user.Id);

            return Ok(new { accessToken, refreshToken });
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh(RefreshTokenDto dto)
        {
            var storedToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == dto.RefreshToken);

            if (storedToken == null || !storedToken.IsActive)
            {
                return Unauthorized(new { message = "Refresh token geçersiz veya süresi dolmuş." });
            }

            var user = await _context.Users.FindAsync(storedToken.UserId);

            if (user == null)
            {
                return Unauthorized(new { message = "Refresh token geçersiz veya süresi dolmuş." });
            }

            // Eski refresh token'ı iptal edip yenisini üretiyoruz (rotation)
            storedToken.Revoke();
            var newRefreshToken = await GenerateAndSaveRefreshTokenAsync(user.Id);
            var newAccessToken = GenerateJwtToken(user);

            await _context.SaveChangesAsync();

            return Ok(new { accessToken = newAccessToken, refreshToken = newRefreshToken });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout(RefreshTokenDto dto)
        {
            var storedToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == dto.RefreshToken);

            if (storedToken != null)
            {
                storedToken.Revoke();
                await _context.SaveChangesAsync();
            }

            // Token bulunamasa bile aynı, muğlak cevabı dönüyoruz (bilgi sızdırmamak için)
            return Ok(new { message = "Çıkış yapıldı." });
        }

        private string GenerateJwtToken(User user)
        {
            var jwtKey = _configuration["Jwt:Key"]
                ?? throw new InvalidOperationException("Jwt:Key ayarı bulunamadı.");

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(30),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private async Task<string> GenerateAndSaveRefreshTokenAsync(int userId)
        {
            var randomBytes = RandomNumberGenerator.GetBytes(64);
            var tokenValue = Convert.ToBase64String(randomBytes);

            var refreshToken = new RefreshToken(
                tokenValue,
                userId,
                DateTime.UtcNow.AddDays(7)
            );

            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();

            return tokenValue;
        }
    }
}