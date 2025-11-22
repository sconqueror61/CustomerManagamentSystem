using CustomerManagementSystem.DB;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CustomerManagementSystem.Controllers
{
	public class StartingController : Controller
	{
		private readonly CustomerManagementSystemContext _db;
		public StartingController(CustomerManagementSystemContext db)
		{
			_db = db;
		}

		public IActionResult Index()
		{
			return View();
		}

		private (int? CustomerId, bool IsCustomer) GetCurrentCustomer()
		{
			var type = User.FindFirst("UserTypeId")?.Value;
			var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("UserId")?.Value;
			if (int.TryParse(idStr, out var cid))
				return (cid, type == "2");
			return (null, false);
		}

		private async Task<Session?> GetLatestSessionAsync(int customerId)
		{
			return await _db.Sessions
				.Where(s => s.CustomerId == customerId)
				.OrderByDescending(s => s.EnterTime)
				.FirstOrDefaultAsync();
		}

		private async Task FlushCookiePathsAsync()
		{

			var raw = Request.Cookies["cust_paths"];
			if (string.IsNullOrWhiteSpace(raw)) return;

			var (cid, isCust) = GetCurrentCustomer();
			if (!isCust || cid is null) return;

			var session = await GetLatestSessionAsync(cid.Value);

			var paths = raw
				.Split('|', StringSplitOptions.RemoveEmptyEntries)
				.Select(p => p.Trim())
				.Where(p => p.Length > 0)
				.Take(10)
				.ToArray();

			if (paths.Length == 0) return;

			var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
			var ua = Request.Headers["User-Agent"].ToString();
			var now = DateTime.UtcNow;

			foreach (var p in paths)
			{
				_db.SessionDetails.Add(new SessionDetail
				{
					SessionId = session?.Id ?? 0,
					Action = "PathVisit",
					Path = p,
				});
			}

			await _db.SaveChangesAsync();

			Response.Cookies.Delete("cust_paths");
		}

		public async Task<IActionResult> LogOut(int? userId)
		{
			if (userId is null)
			{
				var claimVal = User.FindFirstValue(ClaimTypes.NameIdentifier);
				if (int.TryParse(claimVal, out var parsed))
					userId = parsed;
			}

			// 2) userId varsa TempData'ya yaz + Session tablosunu güncelle
			if (userId is not null)
			{
				TempData["LastLoggedOutUserId"] = userId.Value;

				// 2.a) Bu kullanıcıya ait, ExitTime'ı henüz dolmamış en son session'ı bul
				var latestSession = await _db.Sessions
					.Where(s => s.CustomerId == userId.Value && s.ExitTime == null)
					.OrderByDescending(s => s.EnterTime)
					.FirstOrDefaultAsync();

				// 2.b) Varsa ExitTime set et
				if (latestSession is not null)
				{
					latestSession.ExitTime = DateTime.Now; // veya DateTime.UtcNow
					await _db.SaveChangesAsync();
				}
			}

			// 3) Cookie + path temizliği
			await FlushCookiePathsAsync();

			await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

			return RedirectToAction("Login", "Access");
		}
		[HttpGet]
		public JsonResult GetCompletedOrdersDaily()
		{
			var userType = User.FindFirst("UserTypeId")?.Value;
			if (userType != "1")
			{
				return Json(new { success = false, message = "Unavailable connection." });
			}

			var userIdStr = User.FindFirst("UserId")?.Value
				  ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

			if (!int.TryParse(userIdStr, out var userId))
			{
				return Json(new { success = false, message = "User information cannot be readen." });
			}


			var completed = (from h in _db.OrdersHistories
							 join d in _db.OrdersDetails on h.OrderDetailId equals d.Id
							 join o in _db.Orders on d.OrderId equals o.Id
							 where h.StatusId == 5
								   && d.IsDeleted == false
								   && o.IsDeleted == false
								   && d.SupplierId == userId
							 orderby h.Date descending
							 select new
							 {
								 OrderHistoryId = h.Id,
								 h.OrderDetailId,
								 h.Date,

								 OrderId = o.Id,
								 o.UserId,
								 o.TotalAmount,
								 o.TotalPrice,
								 OrderDate = o.Date,

								 DetailId = d.Id,
								 d.ProductId,
								 d.SupplierId,
								 d.Amount,
								 d.Price,
								 LineTotal = (d.Amount ?? 0) * d.Price
							 })
							 .ToList();

			return Json(new
			{
				success = true,
				totalCompleted = completed.Count,
				data = completed
			});
		}
		[HttpGet]
		public JsonResult GetReturningAndNewCustomersChartData()
		{
			var usertypeId = User.FindFirst("UserTypeId")?.Value;
			if (usertypeId != "1")
			{
				return Json(new { success = false, message = "Unavailable access" });
			}

			// BUGÜN + SON 7 GÜN
			DateTime today = DateTime.Today;
			DateTime startDate = today.AddDays(-6); // toplam 7 gün

			// Tüm session'ları al
			var sessions = _db.Sessions
				.Where(s => s.EnterTime.Date >= startDate && s.EnterTime.Date <= today)
				.Select(s => new
				{
					s.CustomerId,
					s.EnterTime
				})
				.ToList();

			if (!sessions.Any())
			{
				return Json(new { success = true, data = new List<object>() });
			}

			// HER CUSTOMER İÇİN ESKİ / YENİ
			var customerStatus = _db.Sessions
				.GroupBy(s => s.CustomerId)
				.Select(g => new
				{
					CustomerId = g.Key,
					IsOld = g.Count() > 1
				})
				.ToDictionary(x => x.CustomerId, x => x.IsOld);

			// GÜN BAZLI HESAPLAMA
			var dailyData = sessions
				.GroupBy(s => s.EnterTime.Date)
				.Select(g =>
				{
					var distinctCustomers = g.Select(x => x.CustomerId).Distinct().ToList();

					return new
					{
						Day = g.Key.Day,
						ReturningCustomers = distinctCustomers.Count(cid => customerStatus[cid]),
						NewCustomers = distinctCustomers.Count(cid => !customerStatus[cid])
					};
				})
				.OrderBy(x => x.Day)
				.ToList();

			return Json(new
			{
				success = true,
				data = dailyData
			});
		}

	}
}
