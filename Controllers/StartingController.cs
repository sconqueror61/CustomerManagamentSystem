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
			var userIdStr = User.FindFirst("UserId")?.Value
				 ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

			if (!int.TryParse(userIdStr, out var userId))
			{
				return RedirectToAction("Login", "Access");
			}

			ViewBag.UserId = userId;

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

			// Ip ve User-Agent kullanılmıyor, bu yüzden sildim.

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

		// (GetCompletedOrdersDaily metodu değişmediği için kısaltıldı)
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
		public async Task<IActionResult> GetReturningAndNewCustomersChartData()
		{
			var maxDays = 30; // Son 30 günlük veriyi alalım

			var today = DateTime.Today;
			var startDate = today.AddDays(-maxDays);

			var allSessions = await _db.Sessions
				.Where(s => s.EnterTime >= startDate)
				.OrderBy(s => s.EnterTime)
				.ToListAsync();

			var firstSessionPerCustomer = allSessions
				.GroupBy(s => s.CustomerId)
				.ToDictionary(g => g.Key, g => g.Min(s => s.EnterTime.Date));

			var dailyData = new List<object>();

			for (int i = 0; i <= maxDays; i++)
			{
				var currentDate = startDate.AddDays(i);
				var sessionsOnDay = allSessions.Where(s => s.EnterTime.Date == currentDate).ToList();

				// Bu günlük tüm session'ların ait olduğu müşteriler
				var distinctCustomersOnDay = sessionsOnDay.Select(s => s.CustomerId).Distinct().ToList();

				int returningCustomers = 0;
				int newCustomers = 0;

				foreach (var customerId in distinctCustomersOnDay)
				{
					// Müşterinin ilk session tarihi, bugünün tarihiyle aynıysa, yeni müşteridir.
					if (firstSessionPerCustomer.TryGetValue(customerId, out var firstSessionDate) && firstSessionDate.Date == currentDate)
					{
						newCustomers++;
					}
					else
					{
						returningCustomers++;
					}
				}

				dailyData.Add(new
				{
					Day = currentDate.ToString("dd/MM"), // X ekseni etiketi
					ReturningCustomers = returningCustomers,
					NewCustomers = newCustomers
				});
			}

			// Toplamlar
			var totalReturning = dailyData.Sum(d => (int)((dynamic)d).ReturningCustomers);
			var totalNew = dailyData.Sum(d => (int)((dynamic)d).NewCustomers);
			var total = totalReturning + totalNew;
			double returningPercent = total > 0 ? ((double)totalReturning / total) * 100.0 : 0.0;


			return Json(new
			{
				success = true,
				data = dailyData,
				totalReturning = totalReturning,
				totalNew = totalNew,
				returningPercent = returningPercent // Gauge için ek bilgi
			});
		}


		[HttpGet]
		public async Task<IActionResult> GetEfficiencyMetrics()
		{
			// 1) SessionDetail üzerinden, SessionId'si dolu olanları grupla
			var sessionStats = await _db.SessionDetails
				.Where(sd => sd.SessionId != null)
				.GroupBy(sd => sd.SessionId)          // g.Key => int?
				.Select(g => new
				{
					SessionId = g.Key,               // int?
					EventCount = g.Count(),
					DistinctPathCount = g.Select(x => x.Path).Distinct().Count()
				})
				.ToListAsync();

			int totalSessions = sessionStats.Count;

			if (totalSessions == 0)
			{
				return Json(new
				{
					success = true,
					totalSessions = 0,
					bounceRate = 0.0,
					actualSessionsRate = 0.0,
					newSessionsRate = 0.0,
					clickthroughRate = 0.0,
					// Grafiği doldurmak için örnek veri eklenmeli, aksi halde update() boş kalır.
					labels = new[] { "00:00", "00:00" },
					chartData = new[] { 0.0, 0.0, 0.0 }
				});
			}

			// 2) Bounce / Actual / Clickthrough sayıları
			int bounceSessions = sessionStats.Count(s => s.EventCount <= 3);
			int actualSessions = sessionStats.Count(s => s.EventCount > 3);
			int clickthroughSessions = sessionStats.Count(s => s.DistinctPathCount >= 5);

			// 3) New Sessions: müşterinin ilk oturumu
			//    -> önce nullable listeden sadece dolu olanları int'e çeviriyoruz
			var sessionIds = sessionStats
				.Where(s => s.SessionId.HasValue)
				.Select(s => s.SessionId!.Value)
				.ToList();

			var sessions = await _db.Sessions
				.Where(s => sessionIds.Contains(s.Id))
				.ToListAsync();

			var firstSessionIdsPerCustomer = sessions
				.GroupBy(s => s.CustomerId)
				.Select(g => g.OrderBy(x => x.EnterTime).First().Id)
				.ToHashSet();

			int newSessions = sessionStats.Count(s =>
				s.SessionId.HasValue &&
				firstSessionIdsPerCustomer.Contains(s.SessionId.Value)
			);

			// 4) ExitTime’e göre sıralı label listesi
			// Not: 11 nokta yerine, son 10 session'un çıkış zamanını kullanmak mantıklı.
			const int pointCount = 10;

			// Sadece ExitTime olan oturumları al
			var timelineSessions = sessions
				.Where(s => s.ExitTime.HasValue)
				.OrderByDescending(s => s.ExitTime)
				.Take(pointCount)
				.OrderBy(s => s.ExitTime) // Eski olandan yeni olana sırala
				.ToList();

			var timelineLabels = timelineSessions
				.Select(s => s.ExitTime!.Value.ToString("HH:mm")) // Sadece saat ve dakika
				.ToArray();

			// 5) Yüzdelere çevir
			double bounceRate = (double)bounceSessions / totalSessions * 100.0;
			double actualSessionsRate = (double)actualSessions / totalSessions * 100.0;
			double newSessionsRate = (double)newSessions / totalSessions * 100.0;
			double clickthroughRate = (double)clickthroughSessions / totalSessions * 100.0;

			var dataPoints = timelineLabels.Length;

			return Json(new
			{//
				success = true,
				totalSessions,
				// Metriklerin ham yüzdeleri
				bounceRate,
				actualSessionsRate,
				newSessionsRate,
				clickthroughRate,
				labels = timelineLabels,

				sessionsData = Enumerable.Repeat(actualSessionsRate, dataPoints).ToArray(),
				newSessionsData = Enumerable.Repeat(newSessionsRate, dataPoints).ToArray(),
				bounceData = Enumerable.Repeat(bounceRate, dataPoints).ToArray(),

				clickthroughData = Enumerable.Repeat(clickthroughRate, dataPoints).ToArray()
			});
		}
		[HttpGet]
		public JsonResult GetAllInformationsAboutProfits()
		{
			var userType = User.FindFirst("UserTypeId")?.Value;
			if (userType != "1")
			{
				return Json(new { success = false, message = "Erişim yetkiniz bulunmamaktadır." });
			}

			// 2. Kullanıcı ID'sini Alma
			var userIdStr = User.FindFirst("UserId")?.Value
				 ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

			if (!int.TryParse(userIdStr, out var userId))
			{
				return Json(new { success = false, message = "Kullanıcı bilgisi okunamıyor." });
			}

			// 3. Tedarikçiye Ait Sipariş Detaylarını, Siparişleri (Orders) ve Ürünleri Birleştirme
			var profitsData = (from od in _db.OrdersDetails.Where(od => od.SupplierId == userId)
								   // HATA BURADAYDI! OrdersDetails yerine Orders tablosu ile birleştirme yapılmalı.
							   join o in _db.Orders on od.OrderId equals o.Id

							   // Product tablosu ile birleştirme (Product tablosunda birincil anahtarın Id olduğunu varsayıyoruz)
							   join p in _db.Products on od.ProductId equals p.Id
							   select new
							   {
								   OrderId = od.OrderId,
								   OrderTotalPrice = o.TotalPrice, // ARTIK DOĞRU! Orders tablosundan TotalPrice alındı.
								   ProductCost = p.Cost,
								   OrderAmount = od.Amount,
								   LineCost = od.Amount * p.Cost
							   }).ToList(); // Veriyi belleğe çek

			if (!profitsData.Any())
			{
				return Json(new { success = true, data = new { TotalRevenue = "0,00", TotalOrders = "0", AverageBasketAmount = "0,00", TotalProfit = "0,00" } });
			}

			// Toplam Ciro Hesaplaması
			var distinctOrdersData = profitsData
				.GroupBy(x => x.OrderId)
				.Select(g => new { OrderTotalPrice = g.First().OrderTotalPrice })
				.ToList();

			// Hata Giderme: Sum işleminde OrderTotalPrice alanını decimal'e dönüştürerek 
			// (decimal) veya boş değer alıyorsa GetValueOrDefault() ile güvenlik sağlayın.
			decimal totalRevenue = distinctOrdersData.Sum(x => ((decimal?)x.OrderTotalPrice).GetValueOrDefault());

			// Toplam Sipariş Sayısı
			int totalOrders = distinctOrdersData.Count();

			// Ortalama Sepet Tutarı (Bölme sıfır kontrolü)
			decimal averageBasketAmount = totalOrders > 0 ? totalRevenue / totalOrders : 0;

			// Hata Giderme: LineCost alanını da aynı şekilde güvenli hale getirin.
			decimal totalCost = profitsData.Sum(x => ((decimal?)x.LineCost).GetValueOrDefault());

			// Toplam Kâr
			decimal totalProfit = totalRevenue - totalCost;

			// 5. JSON Verisini Hazırlama (Para birimi formatında)
			var result = new
			{
				// totalRevenue zaten decimal, Math.Truncate ile küsurat atılıp N2 (2 ondalık) formatında gösteriliyor.
				TotalRevenue = Math.Truncate(totalRevenue).ToString("N2"),

				// totalOrders bir int olduğu için direkt ToString("N0") kullanılır.
				// Math.Truncate(totalOrders) ifadesi CS1021'e neden oluyordu.
				TotalOrders = totalOrders.ToString("N0"),

				// averageBasketAmount zaten decimal.
				AverageBasketAmount = Math.Truncate(averageBasketAmount).ToString("N2"),

				// totalProfit zaten decimal.
				TotalProfit = Math.Truncate(totalProfit).ToString("N2")
			};
			return Json(new { success = true, data = result });
		}
	}
}