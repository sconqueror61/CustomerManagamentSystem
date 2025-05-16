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
				var userIdClaim = User.FindFirst("UserId")?.Value;
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
					.Where(ub => ub.IsDeleted != true)
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
							Amount = ub.Amount,
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

		[HttpPut]
		public JsonResult UpdateBasketAmount(int productId, int amount)
		{
			var userIdClaim = User.FindFirst("UserId")?.Value;

			if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
			{
				return Json(new { success = false, message = "Geçersiz kullanıcı oturumu." });
			}

			var product = _dbcontext.Products.FirstOrDefault(x => x.Id == productId);

			if (product == null)
				return Json(new { success = false, message = "Ürün bulunamadı." });

			var userBasket = _dbcontext.UserBaskets
				.FirstOrDefault(x => x.ProductId == productId && x.UserId == userId && x.IsDeleted != true);

			if (userBasket == null)
			{
				// Ürün sepette yoksa, yeni kayıt oluştur
				if (amount > product.Stock)
					return Json(new { success = false, message = "Yeterli stok yok." });

				userBasket = new UserBasket()
				{
					ProductId = productId,
					Amount = amount,
					UserId = userId,
					RecordDate = DateTime.Now,
					IsDeleted = false
				};

				product.Stock -= amount;
				_dbcontext.UserBaskets.Add(userBasket);
			}
			else
			{
				// Ürün sepette varsa, sadece miktar güncellemesi yap
				int difference = amount - (userBasket.Amount ?? 0);

				if (difference > 0)
				{
					// Kullanıcı sepetteki miktarı artırmak istiyor
					if (product.Stock < difference)
						return Json(new { success = false, message = "Stok yetersiz." });

					product.Stock -= difference;
				}
				else if (difference < 0)
				{
					// Kullanıcı sepetteki miktarı azaltmak istiyor
					product.Stock += Math.Abs(difference);
				}

				userBasket.Amount = amount;
				userBasket.RecordDate = DateTime.Now;
				_dbcontext.UserBaskets.Update(userBasket);
			}

			_dbcontext.Products.Update(product);
			_dbcontext.SaveChanges();

			return Json(new { success = true, message = "Sepet güncellendi." });
		}


		[HttpDelete]
		public IActionResult DeleteProductFromBasket(int productId)
		{
			var userIdClaim = User.FindFirst("UserId")?.Value;
			if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
			{
				return Json(new { success = false, message = "Geçersiz kullanıcı oturumu." });
			}

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
