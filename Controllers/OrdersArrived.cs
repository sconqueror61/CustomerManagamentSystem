using CustomerManagementSystem.DB;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

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
				return int.TryParse(userIdClaim, out var userId) ? userId : -1;
			}
		}

		// ORDER HISTORY
		[HttpGet]
		public async Task<IActionResult> GetOrdersHistory(int orderDetailId)
		{
			if (UserId <= 0)
				return Json(new { success = false, message = "Kullanıcı oturumu bulunamadı." });

			// Tek sorguda, ilgili OrderDetail gerçekten bu tedarikçiye mi ait onu da kontrol edelim
			var ordersHistory = await (
				from h in _context.OrdersHistories.AsNoTracking()
				join d in _context.OrdersDetails.AsNoTracking()
					on h.OrderDetailId equals d.Id
				where d.SupplierId == UserId && h.OrderDetailId == orderDetailId
				orderby h.Date
				select new
				{
					h.Id,
					h.Date,
					h.OrderDetailId,
					h.StatusId
				}
			).ToListAsync();

			return Json(new { success = true, data = ordersHistory });
		}

		// ARRIVED ORDERS
		[HttpGet]
		public async Task<IActionResult> GetArrivedOrders()
		{
			if (UserId <= 0)
				return BadRequest("Geçersiz kullanıcı.");

			var arrivedOrders = await _context.OrdersDetails
				.Where(x => x.SupplierId == UserId && (x.IsDeleted == null || x.IsDeleted == false))
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
				.ToListAsync();

			return Json(new { success = true, data = arrivedOrders });
		}

		// INFO (tek sorguda join ile)
		[HttpGet]
		public async Task<IActionResult> GetInfo(int id)
		{
			var data = await (
				from od in _context.OrdersDetails
				join u in _context.Users on od.UserId equals u.Id
				join p in _context.Products on od.ProductId equals p.Id
				where od.Id == id
				select new
				{
					orderId = od.OrderId,
					userName = u.Name,
					userSurname = u.SurName,
					productExplanation = p.Explanation,
					productDescription = p.Description,
					unitPrice = od.Price,
					amount = od.Amount,
					address = u.Adress
				}
			).AsNoTracking().FirstOrDefaultAsync();

			if (data == null)
				return Json(new { success = false, message = "Detay bulunamadı." });

			return Json(new { success = true, data });
		}

		// DELETE ORDER (soft delete)
		[HttpPost]
		public async Task<IActionResult> DeleteOrder(int orderId)
		{
			var orders = await _context.Orders
				.Where(o => o.Id == orderId)
				.ToListAsync();

			if (orders.Any())
			{
				foreach (var item in orders)
				{
					item.IsDeleted = true;
					item.StatusId = 6;
				}
			}

			var orderDetails = await _context.OrdersDetails
				.Where(d => d.OrderId == orderId)
				.ToListAsync();

			foreach (var detail in orderDetails)
			{
				detail.IsDeleted = true;
				detail.StatusId = 6;
			}

			await _context.SaveChangesAsync();

			return Json(new { success = true });
		}

		// STATUS + HISTORY
		[HttpPut]
		public async Task<IActionResult> SetStatusAndHistory(int statusId, int orderId)
		{
			var order = await _context.Orders.FirstOrDefaultAsync(x => x.Id == orderId);
			if (order == null)
				return Json(new { success = false, message = "Order not found." });

			order.StatusId = statusId;

			var orderDetails = await _context.OrdersDetails
				.Where(x => x.OrderId == orderId)
				.ToListAsync();

			if (!orderDetails.Any())
				return Json(new { success = false, message = "No details found for this order." });

			foreach (var detail in orderDetails)
			{
				detail.StatusId = statusId;

				_context.OrdersHistories.Add(new OrdersHistory
				{
					OrderDetailId = detail.Id,
					StatusId = statusId,
					Date = DateTime.Now
				});
			}

			await _context.SaveChangesAsync();

			return Json(new { success = true, message = "Status and history have been updated." });
		}

		public IActionResult Index()
		{
			// Kullanıcı ID'sini claim'den al
			var userIdStr = User.FindFirst("UserId")?.Value
				 ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

			// Kullanıcı giriş yapmamışsa -> direkt engelle
			if (!int.TryParse(userIdStr, out var userId))
			{
				return RedirectToAction("Login", "Access");
			}

			// Kullanıcı giriş yapmış -> sayfayı aç
			ViewBag.UserId = userId;
			return View();
		}



		// TAMAMLANMIŞ / SİLİNMİŞ SİPARİŞLER
		[HttpGet]
		public async Task<IActionResult> GetCompletedAndDeletedOrders()
		{
			if (UserId <= 0)
				return BadRequest("Geçersiz kullanıcı.");

			// Parantez düzeltildi
			var arrivedOrders = await _context.OrdersDetails
				.AsNoTracking()
				.Where(x =>
					x.SupplierId == UserId &&
					(
						x.IsDeleted == true ||
						x.StatusId == 5 ||
						x.StatusId == 6
					)
				)
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
				.ToListAsync();

			return Json(new { success = true, data = arrivedOrders });
		}

		// CASCADE DELETE
		[HttpPost]
		public async Task<IActionResult> DeleteOrderCascade(int orderDetailId)
		{
			try
			{
				var orderDetail = await _context.OrdersDetails
					.FirstOrDefaultAsync(x => x.Id == orderDetailId);

				if (orderDetail == null)
					return Json(new { success = false, message = "Order detail not found." });

				var orderId = orderDetail.OrderId;

				var relatedDetails = await _context.OrdersDetails
					.Where(x => x.OrderId == orderId)
					.ToListAsync();

				var detailIds = relatedDetails
					.Select(od => od.Id)
					.ToList();

				var historyRecords = await _context.OrdersHistories
					.Where(h => detailIds.Contains((int)h.OrderDetailId))
					.ToListAsync();

				_context.OrdersHistories.RemoveRange(historyRecords);
				_context.OrdersDetails.RemoveRange(relatedDetails);

				var order = await _context.Orders.FirstOrDefaultAsync(x => x.Id == orderId);
				if (order != null)
					_context.Orders.Remove(order);

				await _context.SaveChangesAsync();

				return Json(new { success = true, message = "Order deleted successfully." });
			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = "Error occurred: " + ex.Message });
			}
		}

		public IActionResult CompletedOrdersIndex()
		{
			var userIdStr = User.FindFirst("UserId")?.Value
			 ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

			// Kullanıcı giriş yapmamışsa -> direkt engelle
			if (!int.TryParse(userIdStr, out var userId))
			{
				return RedirectToAction("Login", "Access");
			}
			return View();
		}
	}
}
