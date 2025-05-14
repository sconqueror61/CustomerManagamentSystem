using CustomerManagementSystem.DB;
using Microsoft.AspNetCore.Mvc;

namespace CustomerManagementSystem.Controllers
{
	public class OrderController : Controller
	{
		private readonly CustomerManagementSystemContext _context;
		public List<Order> Orders { get; set; } = new List<Order>();
		public List<OrdersDetail> OrdersDetails { get; set; } = new List<OrdersDetail>();
		public List<OrderStatus> OrderStatuses { get; set; } = new List<OrderStatus>();

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
					Pimages = _context.Pimages
						.Where(y => y.ProductId == x.ProductId)
						.Select(y => new
						{
							y.PictureUrl,
							y.ProductId,
							y.Id,
							y.CreaterUserId
						})
						.ToList() // ⬅️ BURASI EKLENDİ
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
			var userBasket = _context.UserBaskets
				.Where(x => x.UserId == UserId)
				.Select(x => new
				{
					x.UserId,
					x.Id,
					x.ProductId,
					x.RecordDate,
					x.Amount,
					x.IsDeleted
				}).ToList();

			var productIds = userBasket.Select(x => x.ProductId).ToList();

			var orderedProducts = _context.Products
				.Where(p => productIds.Contains(p.Id))
				.ToList();

			float totalPrice = 0;

			foreach (var basketItem in userBasket)
			{
				var product = orderedProducts.FirstOrDefault(p => p.Id == basketItem.ProductId);

				var amount = basketItem.Amount;
				float damount = (float)amount;
				if (product != null)
				{
					totalPrice += (float)((product.Price) * damount);
				}
			}

			var order = new Order
			{
				UserId = UserId,
				TotalAmount = userBasket.Sum(x => x.Amount),
				TotalPrice = totalPrice,
				Date = DateTime.Now,
				StatusId = 1,
				IsDeleted = false,
				RecordDate = userBasket.FirstOrDefault().RecordDate
			};

			_context.Orders.Add(order);
			_context.SaveChanges();

			foreach (var item in userBasket)
			{
				var product = orderedProducts.FirstOrDefault(p => p.Id == item.ProductId && item.IsDeleted == null);
				if (product != null)
				{
					_context.OrdersDetails.Add(new OrdersDetail
					{
						OrderId = order.Id,
						ProductId = product.Id,
						Amount = item.Amount,
						Price = product.Price,
						UserId = item.UserId,
						StatusId = 1,
						IsDeleted = false,
						Date = DateTime.Now,
						RecordDate = item.RecordDate
					});
				}
			}
			_context.SaveChanges(); // OrdersDetail kaydedildi

			// Sepeti pasif et
			var userBasketEntities = _context.UserBaskets
				.Where(x => x.UserId == UserId)
				.ToList();

			foreach (var item in userBasketEntities)
			{
				item.IsDeleted = true;
			}

			_context.SaveChanges(); // IsDeleted = true yansıt

			return Json(new { succes = true, message = "Your order has received" });
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
