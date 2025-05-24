using CustomerManagementSystem.DB;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CustomerManagementSystem.Controllers
{
	public class AccessController : Controller
	{
		private readonly CustomerManagementSystemContext _dbContext;

		public AccessController(CustomerManagementSystemContext context)
		{
			_dbContext = context;
		}

		[HttpGet]
		public IActionResult Register()
		{
			return View();
		}

		[HttpGet]
		public IActionResult Login()
		{
			if (User.Identity != null && User.Identity.IsAuthenticated)
			{
				var userTypeId = User.FindFirst("UserTypeId")?.Value;

				if (userTypeId == "1")
					return RedirectToAction("Index", "Starting");
				else if (userTypeId == "2")
					return RedirectToAction("Index", "CustomerHome");
			}

			return View();
		}


		[HttpPost]
		public async Task<IActionResult> Login(DB.User user)
		{
			if (string.IsNullOrWhiteSpace(user.Email) || string.IsNullOrWhiteSpace(user.Password))
			{
				return Json(new { success = false, message = "Email and password are required." });
			}

			var dbUser = _dbContext.Users.FirstOrDefault(x => x.Email == user.Email && x.Password == user.Password);
			if (dbUser != null)
			{
				var claims = new List<Claim>
		{
			new Claim(ClaimTypes.Name, dbUser.Email),
			new Claim("UserId", dbUser.Id.ToString()),
			new Claim("UserTypeId", dbUser.UserTypeId.ToString())
		};

				var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
				var authProperties = new AuthenticationProperties
				{
					IsPersistent = true,
					AllowRefresh = true,
					ExpiresUtc = DateTime.UtcNow.AddMinutes(30)
				};

				await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

				// Kullanıcı başarılı giriş yaptıysa, success ve yönlendirme URL'sini JSON olarak döndür
				if (dbUser.UserTypeId == 1)
					return Json(new { success = true, redirectUrl = Url.Action("Index", "Starting") });
				if (dbUser.UserTypeId == 2)
					return Json(new { success = true, redirectUrl = Url.Action("Index", "CustomerHome") });
			}

			// Geçersiz kullanıcı bilgileri
			return Json(new { success = false, message = "Invalid email or password." });
		}


		[HttpPost]
		public async Task<IActionResult> SignUp(DB.User user)
		{
			if (string.IsNullOrWhiteSpace(user.Name) ||
				string.IsNullOrWhiteSpace(user.SurName) ||
				string.IsNullOrWhiteSpace(user.Email) ||
				string.IsNullOrWhiteSpace(user.Password) ||
				string.IsNullOrWhiteSpace(user.Adress) ||
				user.UserTypeId == 0)
			{
				return Json(new { success = false, message = "No field can be left blank." });
			}

			var existingUser = _dbContext.Users.FirstOrDefault(x => x.Email == user.Email);
			if (existingUser != null)
			{
				return Json(new { success = false, message = "Email is already registered." });
			}

			try
			{
				_dbContext.Users.Add(user);
				await _dbContext.SaveChangesAsync();

				return Json(new { success = true, message = "User registered successfully." });
			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = $"Unexpected error: {ex.Message}" });
			}
		}
	}
}