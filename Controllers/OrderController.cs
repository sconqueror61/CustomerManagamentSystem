using CustomerManagementSystem.DB;
using CustomerManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;

namespace CustomerManagementSystem.Controllers
{
	public class OrderController : Controller
	{
		private readonly CustomerManagementSystemContext _context;
		public List<Order> Orders { get; set; } = new List<Order>();
		public List<OrdersDetail> OrdersDetails { get; set; } = new List<OrdersDetail>();
		public List<OrderStatus> OrderStatuses { get; set; } = new List<OrderStatus>();
		public List<OrdersHistory> OrdersHistories { get; set; } = new List<OrdersHistory>();

		private int? UserId
		{
			get
			{
				var userIdClaim = User.FindFirst("UserId")?.Value;
				if (int.TryParse(userIdClaim, out var userId))
				{ return userId; }
				return null;
			}
		}
		public OrderController(CustomerManagementSystemContext context)
		{
			_context = context;
		}

		[HttpGet]
		public JsonResult GetOrders()
		{
			var receivedOrders = _context.Orders
				.Where(x => x.UserId == UserId)
				.Select(x => new
				{
					x.StatusId,
					x.Id,
					x.UserId,
					x.IsDeleted,
					x.Date,
					x.RecordDate,
					x.TotalAmount,
					x.TotalPrice
				}).ToList();

			return Json(new { success = true, data = receivedOrders });
		}

		[HttpGet]
		public JsonResult GetOrdersDetails()
		{
			var receivedOrdersDetails = _context.OrdersDetails
				.Where(x => x.UserId == UserId)
				.Select(x => new
				{
					x.OrderId,
					x.Price,
					x.Amount,
					x.ProductId,
					x.UserId,
					x.StatusId,
					x.Date,
					x.IsDeleted,
					x.RecordDate,
					x.SupplierId,
					Pimages = _context.Pimages
						.Where(y => y.ProductId == x.ProductId)
						.Select(y => new
						{
							y.PictureUrl,
							y.ProductId,
							y.Id,
							y.CreaterUserId
						})
						.ToList(),
				}).ToList();

			return Json(new { success = true, data = receivedOrdersDetails });
		}

		[HttpGet]
		public JsonResult GetOrdersStatus()
		{
			var OrderStatus = _context.OrderStatuses
				.Select(x => new
				{
					x.OrderStatus1,
					x.Id
				}).ToList();

			return Json(new { success = true, data = OrderStatus });
		}


		[HttpPost]
		public IActionResult SubmitOrdersAndDetails()
		{
			if (UserId == null)
			{
				return Json(new { success = false, message = "User not found." });
			}

			// Sepetteki ürünleri al
			var userBasket = _context.UserBaskets
				.Where(x => x.UserId == UserId && x.IsDeleted == false)
				.ToList();

			if (!userBasket.Any())
			{
				return Json(new { success = false, message = "Sepetiniz boş." });
			}

			var productIds = userBasket.Select(x => x.ProductId).ToList();

			// Ürün detaylarını al
			var orderedProducts = _context.Products
				.Where(p => productIds.Contains(p.Id))
				.ToList();

			// Toplam fiyat ve miktar hesapla
			double totalPrice = userBasket.Sum(x =>
			{
				var product = orderedProducts.FirstOrDefault(p => p.Id == x.ProductId);
				return (product != null) ? (double)(x.Amount ?? 0) * (double)product.Price : 0;
			});

			int totalAmount = userBasket.Sum(x => x.Amount ?? 0);

			// Siparişi oluştur
			var order = new Order
			{
				UserId = UserId,
				TotalAmount = totalAmount,
				TotalPrice = totalPrice,
				Date = DateTime.Now,
				StatusId = (int)OrderStatusEnum.OrderReceived,
				RecordDate = DateTime.Now
			};

			_context.Orders.Add(order);
			_context.SaveChanges(); // Siparişi kaydettik, artık order.Id var

			// Her sepet ürünü için OrdersDetail oluştur
			var orderDetails = new List<OrdersDetail>();

			foreach (var item in userBasket)
			{
				var product = orderedProducts.FirstOrDefault(p => p.Id == item.ProductId);
				if (product == null) continue;

				var detail = new OrdersDetail
				{
					OrderId = order.Id,
					SupplierId = product.CreaterUserId,
					ProductId = product.Id,
					Amount = item.Amount ?? 0,
					Price = product.Price,
					UserId = item.UserId,
					StatusId = (int)OrderStatusEnum.OrderReceived,
					Date = DateTime.Now,
					RecordDate = DateTime.Now
				};

				orderDetails.Add(detail);
			}

			_context.OrdersDetails.AddRange(orderDetails);

			// Sepettekileri pasif et
			userBasket.ForEach(x => x.IsDeleted = true);

			_context.SaveChanges(); // OrdersDetails ve basket güncellemesi kaydedildi

			// Her OrdersDetail için OrdersHistory oluştur
			var ordersHistories = orderDetails.Select(detail => new OrdersHistory
			{
				Date = DateTime.Now,
				OrderDetailId = detail.Id,
				StatusId = (int)OrderStatusEnum.OrderReceived
			}).ToList();

			_context.OrdersHistories.AddRange(ordersHistories);
			_context.SaveChanges(); // OrdersHistories kaydedildi

			return Json(new { success = true, message = "Your order has been received." });
		}


		public IActionResult Index()
		{
			if (!UserId.HasValue)
			{
				return Json(new { success = false, message = "Geçersiz kullanıcı oturumu" });
			}
			var orders = _context.Orders;
			var ordersDetails = _context.OrdersDetails;
			ViewBag.Orders = orders;
			ViewBag.OrdersDetails = ordersDetails;
			return View();
		}
	}
}
