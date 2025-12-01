using CustomerManagementSystem.DB;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
namespace CustomerManagementSystem.Controllers
{
	public class UserBasketController : Controller
	{
		private readonly CustomerManagementSystemContext _dbcontext;

		private int? UserId
		{
			get
			{
				// ClaimTypes.NameIdentifier veya "UserId" claimlerinden çekiyoruz
				var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier)
							?? User.FindFirst("UserId")?.Value;

				if (int.TryParse(idStr, out var id))
					return id;

				return null;
			}
		}

		public UserBasketController(CustomerManagementSystemContext dbcontext)
		{
			_dbcontext = dbcontext;
		}

		public List<UserBasket> UserBaskets { get; set; } = new List<UserBasket>();

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
		[HttpPost, HttpPut]
		public JsonResult UpdateBasketAmount(int productId, int amount)
		{
			if (!UserId.HasValue)
			{
				return Json(new { success = false, message = "User not found." });
			}

			var userId = UserId.Value;

			// Sepette bu ürün var mı?
			var basketItem = _dbcontext.UserBaskets
				.FirstOrDefault(x => x.UserId == userId
									 && x.ProductId == productId
									 && x.IsDeleted == false);

			// amount 0 veya altı ise: sepetten sil
			if (amount <= 0)
			{
				if (basketItem != null)
				{
					basketItem.IsDeleted = true;
					basketItem.Amount = 0;
					_dbcontext.SaveChanges();
				}

				// güncel toplam
				var total0 = (from b in _dbcontext.UserBaskets
							  join p in _dbcontext.Products on b.ProductId equals p.Id
							  where b.UserId == userId && b.IsDeleted == false
							  select (b.Amount ?? 0) * p.Price).Sum();

				return Json(new
				{
					success = true,
					message = "Ürün sepetten çıkarıldı.",
					amount = 0,
					total = total0
				});
			}

			// amount > 0 ise: varsa güncelle, yoksa ekle (UPSERT)
			if (basketItem == null)
			{
				basketItem = new UserBasket
				{
					UserId = userId,
					ProductId = productId,
					Amount = amount,
					IsDeleted = false
				};
				_dbcontext.UserBaskets.Add(basketItem);
			}
			else
			{
				basketItem.Amount = amount;
				basketItem.IsDeleted = false;
			}

			_dbcontext.SaveChanges();

			// Güncel toplam sepet tutarı
			var total = (from b in _dbcontext.UserBaskets
						 join p in _dbcontext.Products on b.ProductId equals p.Id
						 where b.UserId == userId && b.IsDeleted == false
						 select (b.Amount ?? 0) * p.Price).Sum();

			return Json(new
			{
				success = true,
				message = "Sepet güncellendi.",
				amount = amount,
				total = total
			});
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
