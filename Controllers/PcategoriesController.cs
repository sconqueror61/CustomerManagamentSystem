using CustomerManagementSystem.DB;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CustomerManagementSystem.Controllers
{
	[Authorize]
	public class PcategoriesController : Controller
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

		public PcategoriesController(CustomerManagementSystemContext context)
		{
			_context = context;
		}

		// GET: Pcategories
		public async Task<IActionResult> Index()
		{
			if (!UserId.HasValue)
				return Json(new { success = false, message = "Geçersiz kullanıcı oturumu." });
			return View(await _context.Pcategories.ToListAsync());
		}

		[HttpGet]
		public IActionResult GetCreatedCategories()
		{
			if (!UserId.HasValue)
				return Json(new { success = false, message = "Geçersiz kullanıcı oturumu." });

			var categoryName = _context.PmainCategories
				.Select(c => new
				{
					c.Id,
					c.Categories
				})
				.ToList(); // Bunu ekledik

			var categoriesDesc = _context.Pcategories
				.OrderBy(c => c.Id)
				.Select(c => new
				{
					c.Id,
					c.CategoryDesc,
					c.CategoryId
				})
				.ToList();

			return Ok(new
			{
				categoryName,
				categoriesDesc
			});
		}

		[HttpGet]
		public IActionResult GetCategoryByIds()
		{
			if (!UserId.HasValue)
			{
				return Json(new { success = false, message = "Geçersiz kullanıcı oturumu." });
			}

			var categories = _context.PmainCategories
				.Select(x => new
				{
					id = x.Id,
					categories = x.Categories
				})
				.ToList();

			if (!categories.Any())
			{
				return Json(new { success = false, message = "Kategori bulunamadı!" });
			}

			return Json(new { success = true, data = categories });
		}


		public IActionResult Create()
		{
			if (!UserId.HasValue)
				return Json(new { success = false, message = "Geçersiz kullanıcı oturumu." });
			return View();
		}
		[HttpPost]
		public async Task<IActionResult> Create(string categoryDesc, int categoryid)
		{
			if (!UserId.HasValue)
				return Json(new { success = false, message = "Geçersiz kullanıcı oturumu." });

			if (string.IsNullOrWhiteSpace(categoryDesc) || categoryid <= 0)
			{
				return BadRequest(new { success = false, message = "Eksik bilgi gönderildi." });
			}

			var pcategory = new Pcategory
			{
				CategoryDesc = categoryDesc,
				CategoryId = categoryid,
				CreaterUserId = UserId.Value
			};

			_context.Add(pcategory);
			await _context.SaveChangesAsync();

			return Json(new { success = true, message = "Kategori başarıyla eklendi." });
		}

		public async Task<IActionResult> Edit(int? id)
		{
			if (!UserId.HasValue)
				return Json(new { success = false, message = "Geçersiz kullanıcı oturumu." });

			if (id == null)
			{
				return NotFound();
			}

			var pcategory = await _context.Pcategories.FindAsync(id);
			if (pcategory == null)
			{
				return NotFound();
			}
			return View(pcategory);
		}

		[HttpPost]
		public async Task<IActionResult> Edit(Pcategory pcategory)
		{
			if (!UserId.HasValue)
				return Json(new { success = false, message = "Geçersiz kullanıcı oturumu." });

			if (pcategory == null || pcategory.Id == 0)
			{
				return BadRequest();
			}

			if (ModelState.IsValid)
			{
				try
				{
					pcategory.CreaterUserId = UserId.Value;
					_context.Update(pcategory);
					await _context.SaveChangesAsync();
				}
				catch (DbUpdateConcurrencyException)
				{
					if (!PcategoryExists(pcategory.Id))
					{
						return NotFound();
					}
					else
					{
						throw;
					}
				}
				return Ok();
			}
			return BadRequest(ModelState);
		}

		public async Task<IActionResult> Delete(int? id)
		{
			if (!UserId.HasValue)
				return Json(new { success = false, message = "Geçersiz kullanıcı oturumu." });

			if (id == null)
			{
				return NotFound();
			}

			var pcategory = await _context.Pcategories
				.Where(p => p.CreaterUserId == UserId)
				.FirstOrDefaultAsync(m => m.Id == id);
			if (pcategory == null)
			{
				return NotFound();
			}

			return View(pcategory);
		}

		[HttpPost, ActionName("Delete")]
		public async Task<IActionResult> DeleteConfirmed(int id)
		{
			if (!UserId.HasValue)
				return Json(new { success = false, message = "Geçersiz kullanıcı oturumu." });
			var pcategory = await _context.Pcategories.FindAsync(id);
			if (pcategory != null)
			{
				_context.Pcategories.Remove(pcategory);
			}

			await _context.SaveChangesAsync();
			return RedirectToAction(nameof(Index));
		}

		private bool PcategoryExists(int id)
		{
			return _context.Pcategories.Any(e => e.Id == id);
		}
	}
}
