using CustomerManagementSystem.DB;
using CustomerManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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
		public JsonResult GetOrdersStatus(int statusId)
		{
			if (statusId == 0)
			{
				var orderStatusList = _context.OrderStatuses
					.Select(x => new
					{
						x.OrderStatus1,
						x.Id
					}).ToList();

				return Json(new { success = true, data = orderStatusList });
			}

			var selectedStatus = _context.OrderStatuses
				.Where(x => x.Id == statusId)
				.Select(x => x.OrderStatus1)
				.FirstOrDefault(); // İlk bulamazsa null döner

			if (selectedStatus == null)
			{
				return Json(new { success = false, data = "Not found" });
			}

			return Json(new { success = true, data = selectedStatus });
		}

		[HttpPost]
		public IActionResult SubmitOrdersAndDetails()
		{
			if (!UserId.HasValue)
			{
				return Json(new { success = false, message = "User not found." });
			}

			var userId = UserId.Value;

			// Kullanıcının sepeti
			var userBasket = _context.UserBaskets
				.Where(x => x.UserId == userId && x.IsDeleted == false)
				.ToList();

			if (!userBasket.Any())
			{
				return Json(new { success = false, message = "Basket is empty." });
			}

			// Sepetteki ürünlerin ID'leri
			var productIds = userBasket
				.Where(x => x.ProductId.HasValue)
				.Select(x => x.ProductId.Value)
				.Distinct()
				.ToList();

			// İlgili ürünler
			var products = _context.Products
				.Where(p => productIds.Contains(p.Id))
				.ToList();

			// 1) Stok + ürün kontrolleri (henüz DB'ye yazmadan)
			foreach (var item in userBasket)
			{
				var product = products.FirstOrDefault(p => p.Id == item.ProductId);
				if (product == null)
				{
					return Json(new { success = false, message = "Sepetteki ürünlerden biri bulunamadı." });
				}

				var amount = item.Amount ?? 0;
				var stock = product.Stock ?? 0;

				if (amount <= 0)
				{
					return Json(new { success = false, message = "Ürün miktarı en az 1 olmalıdır." });
				}

				if (amount > stock)
				{
					return Json(new
					{
						success = false,
						message = $"{product.Description} için yeterli stok yok. Mevcut stok: {stock}"
					});
				}
			}

			using var transaction = _context.Database.BeginTransaction();
			try
			{
				// 2) Toplam miktar ve toplam tutar
				double totalPrice = userBasket.Sum(x =>
				{
					var product = products.FirstOrDefault(p => p.Id == x.ProductId);
					return product != null
						? (double)(x.Amount ?? 0) * product.Price
						: 0;
				});

				int totalAmount = userBasket.Sum(x => x.Amount ?? 0);

				// 3) Orders tablosuna kayıt
				var order = new Order
				{
					UserId = userId,
					TotalAmount = totalAmount,
					TotalPrice = totalPrice,
					Date = DateTime.Now,
					StatusId = (int)OrderStatusEnum.OrderReceived,
					IsDeleted = false,
					RecordDate = DateTime.Now
				};

				_context.Orders.Add(order);
				_context.SaveChanges(); // order.Id lazım

				// 4) OrdersDetail kayıtları + stok düşme
				var orderDetails = new List<OrdersDetail>();

				foreach (var item in userBasket)
				{
					var product = products.First(p => p.Id == item.ProductId);

					// stok düş
					product.Stock = (product.Stock ?? 0) - (item.Amount ?? 0);

					var detail = new OrdersDetail
					{
						OrderId = order.Id,
						SupplierId = product.CreaterUserId,   // şemanda var
						ProductId = product.Id,
						Amount = item.Amount ?? 0,
						Price = product.Price,
						UserId = item.UserId,                 // UserBasket'teki UserId
						StatusId = (int)OrderStatusEnum.OrderReceived,
						Date = DateTime.Now,
						IsDeleted = false,
						RecordDate = DateTime.Now
					};

					orderDetails.Add(detail);
				}

				_context.OrdersDetails.AddRange(orderDetails);

				// 5) Sepeti pasifle (IsDeleted = true)
				foreach (var item in userBasket)
				{
					item.IsDeleted = true;
				}

				_context.SaveChanges();

				// 6) OrdersHistory kayıtları
				var ordersHistories = orderDetails.Select(detail => new OrdersHistory
				{
					Date = DateTime.Now,
					OrderDetailId = detail.Id,
					StatusId = (int)OrderStatusEnum.OrderReceived
				}).ToList();

				_context.OrdersHistories.AddRange(ordersHistories);
				_context.SaveChanges();

				transaction.Commit();

				return Json(new { success = true, message = "Siparişiniz başarıyla oluşturuldu." });
			}
			catch (Exception)
			{
				transaction.Rollback();
				return Json(new { success = false, message = "Sipariş oluşturulurken bir hata oluştu." });
			}
		}

		public IActionResult Index()
		{
			// Kullanıcı ID'sini claim'den al
			var userIdStr = User.FindFirst("UserId")?.Value
				 ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

			// Kullanıcı giriş yapmamışsa login'e yönlendir
			if (!int.TryParse(userIdStr, out var userId))
			{
				return RedirectToAction("Login", "Access");
			}

			// ViewBag’e userId koy
			ViewBag.UserId = userId;

			// Sipariş verilerini View'e gönder
			ViewBag.Orders = _context.Orders.ToList();
			ViewBag.OrdersDetails = _context.OrdersDetails.ToList();

			return View();
		}

	}
}

