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

			if (userId is not null)
				TempData["LastLoggedOutUserId"] = userId.Value;

			await FlushCookiePathsAsync();

			await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

			return RedirectToAction("Login", "Access");
		}
		public JsonResult GetCompletedOrdersDaily()
		{
			// İstersen admin kontrolü (UserTypeId = 1 admin kabul ediyorsan)
			var userType = User.FindFirst("UserTypeId")?.Value;
			if (userType != "1")
			{
				return Json(new { success = false, message = "Yetkisiz erişim." });
			}

			var completed = (from h in _db.OrdersHistories
							 join d in _db.OrdersDetails on h.OrderDetailId equals d.Id
							 join o in _db.Orders on d.OrderId equals o.Id
							 where h.StatusId == 5
								   && d.IsDeleted == false
								   && o.IsDeleted == false
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

	}
}
