using CustomerManagementSystem.DB;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CustomerManagementSystem.Controllers
{
	public class PcategoriesController : Controller
	{
		private readonly CustomerManagementSystemContext _db;

		public PcategoriesController(CustomerManagementSystemContext db)
		{
			_db = db;
		}

		public IActionResult Index()
		{
			return View();
		}

		[HttpGet]
		public IActionResult GetCategoryByIds()
		{
			var categories = _db.PmainCategories
				.Select(x => new
				{
					id = x.Id,
					categories = x.Categories
				})
				.ToList();

			return Json(new { data = categories });
		}

		// ================== OLUŞTURULMUŞ KATEGORİLER (Pcategory) ==================
		// /Pcategories/GetCreatedCategories
		[HttpGet]
		public IActionResult GetCreatedCategories()
		{
			// Pcategory => Id, CategoryId, CategoryDesc, CreaterUserId
			var descList = _db.Pcategories
				.Select(c => new
				{
					id = c.Id,
					categoryId = c.CategoryId,
					categoryDesc = c.CategoryDesc
				})
				.ToList();

			// JS tarafında: descData.categoriesDesc ile okuyorsun
			return Json(new { categoriesDesc = descList });
		}

		// /Pcategories/GetCategoryDetails/5
		[HttpGet]
		public IActionResult GetCategoryDetails(int id)
		{
			var detail = _db.Pcategories
				.Where(c => c.Id == id)
				.Select(c => new
				{
					id = c.Id,
					categoryId = c.CategoryId,
					categoryDesc = c.CategoryDesc
				})
				.FirstOrDefault();

			if (detail == null)
				return NotFound(new { message = "Kategori açıklaması bulunamadı." });

			return Json(detail);
		}

		// ================== EKLEME ==================
		// /Pcategories/Create   (POST)
		[HttpPost]
		public IActionResult Create(string CategoryDesc, int CategoryId)
		{
			if (string.IsNullOrWhiteSpace(CategoryDesc) || CategoryId == 0)
			{
				return BadRequest(new { message = "Kategori ve açıklama zorunludur." });
			}

			int? creatorUserId = null;
			var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (int.TryParse(userIdStr, out var uid))
			{
				creatorUserId = uid;
			}

			var entity = new Pcategory
			{
				CategoryId = CategoryId,
				CategoryDesc = CategoryDesc,
				CreaterUserId = creatorUserId
			};

			_db.Pcategories.Add(entity);
			_db.SaveChanges();

			return Json(new { success = true, message = "Kategori başarıyla eklendi." });
		}

		// ================== GÜNCELLEME ==================
		// /Pcategories/Edit   (POST)
		[HttpPost]
		public IActionResult Edit(int Id, string CategoryDesc, int CategoryId)
		{
			var entity = _db.Pcategories.Find(Id);
			if (entity == null)
			{
				return NotFound(new { message = "Kategori açıklaması bulunamadı." });
			}

			if (string.IsNullOrWhiteSpace(CategoryDesc) || CategoryId == 0)
			{
				return BadRequest(new { message = "Kategori ve açıklama zorunludur." });
			}

			entity.CategoryDesc = CategoryDesc;
			entity.CategoryId = CategoryId;

			_db.Pcategories.Update(entity);
			_db.SaveChanges();

			return Json(new { success = true, message = "Kategori başarıyla güncellendi." });
		}

		// ================== SİLME ==================
		// /Pcategories/Delete/5   (POST)
		[HttpPost]
		public IActionResult Delete(int id)
		{
			var entity = _db.Pcategories.Find(id);
			if (entity == null)
			{
				return NotFound(new { message = "Silinecek kayıt bulunamadı." });
			}

			_db.Pcategories.Remove(entity);
			_db.SaveChanges();

			return Json(new { success = true, message = "Kategori başarıyla silindi." });
		}
	}
}
