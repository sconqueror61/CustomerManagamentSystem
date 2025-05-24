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
		public IActionResult GetOrdersHistory(int orderDetailId)
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
				.Where(x => orderDetailIds.Contains(x.OrderDetailId ?? 0) && x.OrderDetailId == orderDetailId)
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

		[HttpGet]
		public IActionResult GetInfo(int id)
		{
			var detail = _context.OrdersDetails.FirstOrDefault(od => od.Id == id);
			if (detail == null)
			{
				return Json(new { success = false, message = "Detay bulunamadı." });
			}

			var user = _context.Users.FirstOrDefault(u => u.Id == detail.UserId);
			var product = _context.Products.FirstOrDefault(p => p.Id == detail.ProductId);

			var result = new
			{
				orderId = detail.OrderId,
				userName = user?.Name,
				userSurname = user?.SurName,
				productExplanation = product?.Explanation,
				productDescription = product?.Description,
				unitPrice = detail.Price,
				amount = detail.Amount,
				address = user?.Adress
			};

			return Json(new { success = true, data = result });
		}


		[HttpPost]
		public IActionResult DeleteOrder(int orderId)
		{
			var order = _context.Orders.Where(o => o.Id == orderId);
			if (order != null)
			{
				foreach (var item in order)
				{
					item.IsDeleted = true;
					item.StatusId = 6;
				}

			}

			var orderDetails = _context.OrdersDetails.Where(d => d.OrderId == orderId).ToList();
			foreach (var detail in orderDetails)
			{
				detail.IsDeleted = true;
				detail.StatusId = 6;
			}

			_context.SaveChanges();

			return Json(new { success = true });
		}

		[HttpPut]
		public IActionResult SetStatusAndHistory(int statusId, int orderId)
		{
			var order = _context.Orders.FirstOrDefault(x => x.Id == orderId);
			if (order == null)
			{
				return Json(new { success = false, message = "Order not found." });
			}

			order.StatusId = statusId;

			var orderDetails = _context.OrdersDetails
				.Where(x => x.OrderId == orderId)
				.ToList();

			if (!orderDetails.Any())
			{
				return Json(new { success = false, message = "No details found for this order." });
			}

			foreach (var detail in orderDetails)
			{
				detail.StatusId = statusId;

				var history = new OrdersHistory
				{
					OrderDetailId = detail.Id,
					StatusId = statusId,
					Date = DateTime.Now
				};

				_context.OrdersHistories.Add(history);
			}

			_context.SaveChanges();

			return Json(new { success = true, message = "Status and history have been updated." });
		}

		public IActionResult Index()
		{
			return View();
		}

		//Tamamlanmış Siparşlerin View Kısmı
		public IActionResult GetCompletedAndDeletedOrders()
		{
			if (UserId <= 0)
			{
				return BadRequest("Geçersiz kullanıcı.");
			}

			var arrivedOrders = _context.OrdersDetails
				.Where(x => x.SupplierId == UserId && x.IsDeleted == true || x.StatusId == 5 || x.StatusId == 6)
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

		[HttpPost]
		public IActionResult DeleteOrderCascade(int orderDetailId)
		{
			try
			{
				// 1. İlk olarak orderDetailId ile ilgili kaydı bul
				var orderDetail = _context.OrdersDetails.FirstOrDefault(x => x.Id == orderDetailId);
				if (orderDetail == null)
				{
					return Json(new { success = false, message = "Order detail not found." });
				}

				// 2. İlgili OrderId'yi al
				var orderId = orderDetail.OrderId;

				// 3. Aynı OrderId'ye sahip tüm OrderDetail kayıtlarını al
				var relatedDetails = _context.OrdersDetails.Where(x => x.OrderId == orderId).ToList();

				// 4. Bu OrderDetail'ların Id'leri ile OrdersHistory kayıtlarını sil
				List<int> detailIds = _context.OrdersDetails
					.Where(od => od.OrderId == orderId)
					.Select(od => od.Id)
					.ToList();
				var historyRecords = _context.OrdersHistories
					.Where(h => detailIds.Contains((int)h.OrderDetailId))
					.ToList();
				_context.OrdersHistories.RemoveRange(historyRecords);

				// 5. OrderDetail kayıtlarını sil
				_context.OrdersDetails.RemoveRange(relatedDetails);

				// 6. Orders tablosundaki ilgili kaydı sil
				var order = _context.Orders.FirstOrDefault(x => x.Id == orderId);
				if (order != null)
				{
					_context.Orders.Remove(order);
				}

				// 7. Değişiklikleri kaydet
				_context.SaveChanges();

				return Json(new { success = true, message = "Order deleted successfully." });
			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = "Error occurred: " + ex.Message });
			}
		}

		public IActionResult CompletedOrdersIndex()
		{
			return View();
		}


	}
}
