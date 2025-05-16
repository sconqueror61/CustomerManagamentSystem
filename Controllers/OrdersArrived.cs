using CustomerManagementSystem.DB;
using Microsoft.AspNetCore.Mvc;

namespace CustomerManagementSystem.Controllers
{
	public class OrdersArrived : Controller
	{
		private readonly CustomerManagementSystemContext _context;

		public OrdersArrived(CustomerManagementSystemContext context)
		{
			_context = context;
		}

		private int UserId
		{
			get
			{
				var userIdClaim = User.FindFirst("UserId")?.Value;
				if (int.TryParse(userIdClaim, out int userId))
					return userId;

				// Kullanıcı ID alınamıyorsa -1 gibi geçersiz bir değer döndür (isteğe göre hata da fırlatabilirsin)
				return -1;
			}
		}

		[HttpGet]
		public IActionResult GetOrdersHistory()
		{
			if (UserId <= 0)
			{
				return Json(new { success = false, message = "Kullanıcı oturumu bulunamadı." });
			}

			var orderDetailIds = _context.OrdersDetails
				.Where(x => x.SupplierId == UserId)
				.Select(x => x.Id)
				.ToList();

			var ordersHistory = _context.OrdersHistories
				.Where(x => orderDetailIds.Contains(x.OrderDetailId ?? 0)) // null olursa 0 olarak al
				.Select(x => new
				{
					x.Id,
					x.Date,
					x.OrderDetailId,
					x.StatusId
				})
				.ToList();

			return Json(new { success = true, data = ordersHistory });
		}

		[HttpGet]
		public IActionResult GetArrivedOrders()
		{
			if (UserId <= 0)
			{
				return BadRequest("Geçersiz kullanıcı.");
			}

			var arrivedOrders = _context.OrdersDetails
				.Where(x => x.SupplierId == UserId && x.IsDeleted == null)
				.Select(x => new
				{
					x.Id,
					x.OrderId,
					x.SupplierId,
					x.Price,
					x.Amount,
					x.ProductId,
					x.UserId,
					x.StatusId,
					x.Date,
					x.RecordDate,
					x.IsDeleted
				})
				.ToList();

			return Json(new { success = true, data = arrivedOrders });
		}

		public IActionResult Index()
		{
			return View();
		}
	}
}
