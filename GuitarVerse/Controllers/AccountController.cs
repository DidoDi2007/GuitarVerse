using GuitarVerse.Data;
using GuitarVerse.Models;
using GuitarVerse.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;


namespace GuitarVerse.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;

        public AccountController(ApplicationDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }


        // GET: /Account/Login (contains both Login + Register tabs)
        public IActionResult Login()
        {
            return View();
        }

        //Reset password
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }


        // POST: /Account/Register
        [HttpPost]
        public async Task<IActionResult> Register(string email, string password, string confirmPassword)
        {
            if (_context.Users.Any(u => u.Email == email))
            {
                ViewBag.RegisterError = "An account with this email already exists.";
                ViewBag.ActiveTab = "register";
                return View("Login");
            }

            if (password != confirmPassword)
            {
                ViewBag.RegisterError = "Passwords do not match.";
                ViewBag.ActiveTab = "register";
                return View("Login");
            }

            if (!IsPasswordStrong(password))
            {
                ViewBag.RegisterError = "Password must be at least 8 characters long, include one uppercase letter and one number.";
                ViewBag.ActiveTab = "register";
                return View("Login");
            }

            var hashedPassword = HashPassword(password);

            var verificationToken = Guid.NewGuid().ToString();

            var user = new User
            {
                Email = email,
                PasswordHash = hashedPassword,
                Role = "Customer",
                IsEmailVerified = false,
                EmailVerificationToken = verificationToken
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            var customer = new Customer
            {
                UserID = user.UserID,
                FirstName = "",
                LastName = "",
                Address = "",
                Phone = ""
            };
            _context.Customers.Add(customer);
            _context.SaveChanges();

            var verificationLink = Url.Action(
                "VerifyEmail",
                "Account",
                new { token = verificationToken },
                protocol: HttpContext.Request.Scheme
            );

            await _emailService.SendEmailAsync(
             user.Email,
             "Verify your GuitarVerse account",
             $"<h3>Welcome to GuitarVerse!</h3><p>Click <a href='{verificationLink}'>here</a> to verify your email.</p>"
            );

            ViewBag.Success = "Account created. Check your email to verify your account.";
            ViewBag.ActiveTab = "login";

            return View("Login");
        }


        // POST: /Account/Login
        [HttpPost]
        public IActionResult Login(string email, string password)
        {
            var hashedPassword = HashPassword(password);
            var user = _context.Users.FirstOrDefault(u => u.Email == email && u.PasswordHash == hashedPassword);


            if (user == null)
            {
                ViewBag.LoginError = "Invalid email or password.";
                ViewBag.ActiveTab = "login";
                return View();
            }

            // Проверка за верифициран имейл
            if (!user.IsEmailVerified)
            {
                ViewBag.LoginError = "You need to verify your email before logging in.";
                ViewBag.ActiveTab = "login";
                return View();
            }

            HttpContext.Session.SetInt32("UserID", user.UserID);
            HttpContext.Session.SetString("UserRole", user.Role);

            // пренасочване спрямо роля
            if (user.Role.Equals("admin", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("Dashboard", "Admin"); // админ панел
            }
            else
            {
                return RedirectToAction("Index", "Home"); // клиентската част
            }


            return RedirectToAction("Index", "Home");
        }


        public IActionResult VerifyEmail(string token)
        {
            if (string.IsNullOrEmpty(token))
                return Content("Invalid verification token.");

            var user = _context.Users.FirstOrDefault(u => u.EmailVerificationToken == token);

            if (user == null)
                return Content("Invalid or expired verification link.");

            user.IsEmailVerified = true;
            user.EmailVerificationToken = null;
            _context.SaveChanges();

            return Content("Your email has been verified. You can now log in.");
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            var user = _context.Users.FirstOrDefault(u => u.Email == email);
            if (user == null)
            {
                ViewBag.Error = "No account found with that email.";
                return View();
            }

            if (user.Role == "admin") // блокиране на админи
            {
                ViewBag.Error = "Password reset is not allowed for admin accounts.";
                return View();
            }


            var token = Guid.NewGuid().ToString();

            var reset = new PasswordReset
            {
                UserId = user.UserID,
                Token = token,
                ExpiresAt = DateTime.Now.AddHours(1),
                Used = false
            };

            _context.PasswordResets.Add(reset);
            await _context.SaveChangesAsync();

            // генериране на линк с токен и email
            var resetLink = Url.Action(
                "ResetPassword",
                "Account",
                new { token = token, email = user.Email },
                Request.Scheme
            );

            await _emailService.SendEmailAsync(
                user.Email,
                "Password Reset",
                $"Click the link to reset your password: <a href='{resetLink}'>Reset Password</a>"
            );

            ViewBag.Message = "Password reset link has been sent to your email.";
            return View();
        }

        [HttpGet]
        public IActionResult ResetPassword(string token, string email)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
                return BadRequest("Invalid password reset link.");

            var resetEntry = _context.PasswordResets
                .Include(r => r.User) // зареждаме потребителя
                .FirstOrDefault(r => r.Token == token && r.User.Email == email && !r.Used && r.ExpiresAt > DateTime.Now);

            var model = new ResetPasswordViewModel
            {
                Token = token,
                Email = email
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = _context.Users.FirstOrDefault(u => u.Email == model.Email);
            if (user == null)
            {
                ViewBag.Error = "User not found.";
                return View(model);
            }


            var resetEntry = _context.PasswordResets
                .FirstOrDefault(r => r.Token == model.Token && r.UserId == user.UserID && !r.Used && r.ExpiresAt > DateTime.Now);

            if (resetEntry == null)
            {
                ViewBag.Error = "Invalid or expired password reset link.";
                return View(model);
            }

            if (model.NewPassword != model.ConfirmPassword)
            {
                ViewBag.Error = "Passwords do not match.";
                return View(model);
            }

            if (model.NewPassword.Length < 8 || !Regex.IsMatch(model.NewPassword, @"[A-Z]") || !Regex.IsMatch(model.NewPassword, @"[0-9]"))
            {
                ViewBag.Error = "Password must be at least 8 characters long, include one uppercase letter and one number.";
                return View(model);
            }

            user.PasswordHash = HashPassword(model.NewPassword);
            resetEntry.Used = true;

            await _context.SaveChangesAsync();

            return RedirectToAction("ResetPasswordConfirmation");
        }


        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return BitConverter.ToString(bytes).Replace("-", "").ToLower();
            }
        }

        private bool IsPasswordStrong(string password)
        {
            return password.Length >= 8 &&
                   Regex.IsMatch(password, @"[A-Z]+") &&
                   Regex.IsMatch(password, @"[0-9]+");
        }
    }
}