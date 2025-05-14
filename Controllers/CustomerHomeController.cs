using CustomerManagementSystem.DB;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerManagementSystem.Controllers
{
	[Authorize]
	public class CustomerHomeController : Controller
	{
		private readonly CustomerManagementSystemContext _context;
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

		public CustomerHomeController(CustomerManagementSystemContext context)
		{
			_context = context;
		}

		public async Task<IActionResult> Index()
		{
			if (!UserId.HasValue)
				return Json(new { success = false, message = "Geçersiz kullanıcı oturumu." });
			var allProducts = _context.Products
				.OrderBy(x => x.Explanation).ToList();
			ViewBag.Products = allProducts;
			return View();

		}

		[HttpGet]
		public JsonResult GetAllProducts()
		{
			if (!UserId.HasValue)
				return Json(new { success = false, message = "Geçersiz kullanıcı oturumu." });

			try
			{
				var products = _context.Products
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
						Pimages = _context.Pimages
							.Where(img => img.ProductId == p.Id)
							.OrderBy(img => img.Id)
							.Select(img => img.PictureUrl)
							.ToList(),
						p.CreaterUserId,
					})
					.ToList();

				return Json(new { success = true, data = products });
			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = "Hata oluştu: " + ex.Message });
			}
		}

		[HttpGet]
		public JsonResult GetProductId(int id)
		{
			var product = _context.Products.Where(x => x.Id == id)
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
					x.CreaterUserId,
					x.Stock
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
				var products = _context.Products
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
	}
}
