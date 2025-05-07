using CustomerManagementSystem.DB;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerManagementSystem.Controllers
{
	[Authorize]
	public class ProductListController : Controller
	{
		private readonly CustomerManagementSystemContext _dbContext;
		private readonly IWebHostEnvironment _hostEnvironment;

		public List<Product> Products { get; set; } = new List<Product>();
		public List<Pimage> Pimages { get; set; } = new List<Pimage>();

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

		public ProductListController(CustomerManagementSystemContext dbContext)
		{
			_dbContext = dbContext;
		}

		public async Task<IActionResult> Index()
		{
			if (!UserId.HasValue)
				return Json(new { success = false, message = "Geçersiz kullanıcı oturumu." });
			var allProducts = _dbContext.Products
				.Where(x => x.CreaterUserId == UserId)
				.OrderBy(x => x.Explanation).ToList();
			ViewBag.Products = allProducts;
			return View();

		}

		public async Task<IActionResult> CustomerIndex(int id)
		{
			if (!UserId.HasValue)
				return Json(new { success = false, message = "Geçersiz kullanıcı oturumu." });
			var allProducts = _dbContext.Products
				.OrderBy(x => x.Explanation).ToList();
			ViewBag.Products = allProducts;
			ViewBag.categoryId = id;
			return View();

		}

		[HttpDelete]
		public async Task<IActionResult> Delete(int id)
		{
			if (!UserId.HasValue)
				return Json(new { success = false, message = "Geçersiz kullanıcı oturumu." });

			var product = await _dbContext.Products.FindAsync(id);

			if (product == null)
			{
				return Json(new { success = false, message = "Product not found." });
			}

			// İlgili tüm resimleri getir
			var images = _dbContext.Pimages.Where(img => img.ProductId == id).ToList();

			if (images.Any())  // Eğer bu kategoriye ait resimler varsa, sil
			{
				_dbContext.Pimages.RemoveRange(images);
			}

			_dbContext.Products.Remove(product);
			await _dbContext.SaveChangesAsync();

			return Json(new { success = true });
		}

		[HttpGet]
		public JsonResult GetAllProducts(int? categoryid)
		{
			if (!UserId.HasValue)
				return Json(new { success = false, message = "Geçersiz kullanıcı oturumu." });
			if (User.FindFirst("UserTypeId")?.Value == "1")
			{
				try
				{
					var products = _dbContext.Products
						.Where(p => p.CreaterUserId == UserId)
						.Select(p => new
						{
							p.Id,
							p.Explanation,
							p.Description,
							p.Price,
							p.CategoryId,
							p.Width,
							p.Height,
							p.Breakibility,
							Pimages = _dbContext.Pimages
								.Where(img => img.ProductId == p.Id)
								.OrderBy(img => img.Id)
								.Select(img => img.PictureUrl)
								.ToList(),
							p.Stock,
							p.CreaterUserId
						});
					if (categoryid != null)
					{
						products = products.Where(x => x.CategoryId == categoryid);
					}
					return Json(new { success = true, data = products.ToList() });
				}
				catch (Exception ex)
				{
					return Json(new { success = false, message = "Hata oluştu: " + ex.Message });
				}
			}
			else
			{
				try
				{
					var products = _dbContext.Products
						.Select(p => new
						{
							p.Id,
							p.Explanation,
							p.Description,
							p.Price,
							p.CategoryId,
							p.Width,
							p.Height,
							p.Breakibility,
							Pimages = _dbContext.Pimages
								.Where(img => img.ProductId == p.Id)
								.OrderBy(img => img.Id)
								.Select(img => img.PictureUrl)
								.ToList(),
							p.Stock,
							p.CreaterUserId
						});
					if (categoryid != 0)
					{
						products = products.Where(x => x.CategoryId == categoryid);
					}

					return Json(new { success = true, data = products.ToList() });
				}
				catch (Exception ex)
				{
					return Json(new { success = false, message = "Hata oluştu: " + ex.Message });
				}
			}

		}

		[HttpGet]
		public JsonResult GetProduct(int itemId)
		{
			if (!UserId.HasValue)
				return Json(new { success = false, message = "Geçersiz kullanıcı oturumu." });
			var product = _dbContext.Products.Where(x => x.Id == itemId)
				.Where(p => p.CreaterUserId == UserId)
				.Select(x => new
				{
					x.Price,
					x.Explanation,
					x.Breakibility,
					x.CategoryId,
					x.Description,
					x.Id,
					x.Height,
					x.Width,
					x.Stock,
					x.CreaterUserId
				}).FirstOrDefault();

			return Json(product);
		}
		[HttpGet]
		public JsonResult GetAllProductsId()
		{
			if (!UserId.HasValue)
				return Json(new { success = false, message = "Geçersiz kullanıcı oturumu." });
			try
			{
				var products = _dbContext.Products.Where(p => p.CreaterUserId == UserId)
					.Select(p => p.Id)
					.Distinct()
					.ToList();

				return Json(new { success = true, products });
			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = "Hata oluştu: " + ex.Message });
			}
		}

		[HttpPost]
		public JsonResult UpdateProduct(Product updatedProduct)
		{
			if (!UserId.HasValue)
				return Json(new { success = false, message = "Geçersiz kullanıcı oturumu." });
			if (updatedProduct == null || updatedProduct.Id <= 0)
			{
				return Json(new { success = false, message = "Invalid Data!" });
			}

			var existingProduct = _dbContext.Products.Find(updatedProduct.Id);
			if (existingProduct == null)
			{
				return Json(new { success = false, message = "Product not found!" });
			}

			// Sadece ilgili alanları güncelle
			existingProduct.Price = updatedProduct.Price;
			existingProduct.CategoryId = updatedProduct.CategoryId;
			existingProduct.Breakibility = updatedProduct.Breakibility;
			existingProduct.Width = updatedProduct.Width;
			existingProduct.Height = updatedProduct.Height;
			existingProduct.Explanation = updatedProduct.Explanation;
			existingProduct.Description = updatedProduct.Description;
			existingProduct.Stock = updatedProduct.Stock;
			updatedProduct.CreaterUserId = UserId.Value;
			_dbContext.SaveChanges();

			return Json(new { success = true });
		}

	}
}
