using CustomerManagementSystem.DB;
using Microsoft.AspNetCore.Mvc;

namespace CustomerManagementSystem.Controllers
{
	public class UserBasketController : Controller
	{
		private readonly CustomerManagementSystemContext _dbcontext;

		public UserBasketController(CustomerManagementSystemContext dbcontext)
		{
			_dbcontext = dbcontext;
		}

		public List<UserBasket> UserBaskets { get; set; } = new List<UserBasket>();

		private int? UserId
		{
			get
			{
				var userIdClaim = User.FindFirst("Id")?.Value;
				if (int.TryParse(userIdClaim, out int userId))
					return userId;
				return null;
			}
		}

		public IActionResult Index()
		{
			return View();
		}

		[HttpGet]
		public IActionResult GetUserBasketProducts()
		{
			var userIdClaim = User.FindFirst("UserId")?.Value;

			if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int userId))
			{
				var userBaskets = _dbcontext.UserBaskets
					.Where(ub => ub.UserId == userId)
					.ToList();

				var selectedProducts = userBaskets
					.Select(ub =>
					{
						var product = _dbcontext.Products
							.FirstOrDefault(p => p.Id == ub.ProductId);

						if (product == null) return null;

						var productImages = _dbcontext.Pimages
							.Where(img => img.ProductId == product.Id)
							.OrderBy(img => img.Id)
							.Select(img => img.PictureUrl)
							.ToList();

						return new
						{
							product.Id,
							product.Explanation,
							product.Description,
							product.Price,
							product.CategoryId,
							product.Width,
							product.Height,
							product.Breakibility,
							Pimages = productImages.Any() ? productImages : new List<string>(),
							product.Stock,
							product.CreaterUserId,
							Amount = ub.Amount, // sadece bu ürüne ait miktar
							TotalPrice = Math.Round((float)ub.Amount * (float)product.Price, 2) // toplam fiyat
						};
					})
					.Where(p => p != null)
					.ToList();

				var totalPrice = selectedProducts.Sum(p => p.TotalPrice);
				return Json(new { success = true, data = selectedProducts, total = totalPrice });
			}

			return Json(new { success = false, message = "Something else wrong" });
		}


		[HttpPost]
		public JsonResult SetTotalAmount(int quantity, int productid)
		{
			// Kullanıcı ID’sini claim üzerinden al
			var userIdClaim = User.FindFirst("UserId")?.Value;
			if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
			{
				return Json(new { success = false, message = "Geçersiz kullanıcı oturumu." });
			}

			// Ürün var mı?
			var product = _dbcontext.Products
				.FirstOrDefault(x => x.Id == productid);

			if (product == null)
				return Json(new { success = false, message = "Ürün bulunamadı." });

			if (product.Stock < quantity)
				return Json(new { success = false, message = "Yeterli stok yok." });

			// Stoğu azalt
			product.Stock -= quantity;

			// Kullanıcının sepetinde ürün varsa güncelle
			var existingBasketItem = _dbcontext.UserBaskets
				.FirstOrDefault(x => x.UserId == userId && x.ProductId == productid);

			if (existingBasketItem != null)
			{
				existingBasketItem.Amount += quantity;
			}
			else
			{
				var newBasketItem = new UserBasket
				{
					UserId = userId,
					ProductId = productid,
					Amount = quantity,
					RecordDate = DateTime.UtcNow
				};

				_dbcontext.UserBaskets.Add(newBasketItem);
			}

			_dbcontext.SaveChanges();

			return Json(new { success = true, message = "Ürün sepetinize eklendi." });
		}


		[HttpDelete]
		public IActionResult DeleteProductFromBasket(int productId)
		{
			var userIdClaim = User.FindFirst("UserId")?.Value;
			if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
			{
				return Json(new { success = false, message = "Geçersiz kullanıcı oturumu." });
			}

			// Kullanıcının sepetinden ilgili ürünün kaydını bul
			var basketProduct = _dbcontext.UserBaskets
				.FirstOrDefault(x => x.UserId == userId && x.ProductId == productId);

			if (basketProduct == null)
				return Json(new { success = false, message = "Sepette bu ürün bulunamadı." });

			// Ürünü veritabanından bul
			var product = _dbcontext.Products
				.FirstOrDefault(x => x.Id == productId);

			if (product == null)
				return Json(new { success = false, message = "Ürün veritabanında bulunamadı." });

			// Stoğu geri ekle
			product.Stock += basketProduct.Amount;

			// Sepetten ürünü çıkar
			_dbcontext.UserBaskets.Remove(basketProduct);
			_dbcontext.SaveChanges();

			return Json(new { success = true });
		}


	}
}
