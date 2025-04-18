using CustomerManagementSystem.DB;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerManagementSystem.Controllers
{
	[Authorize]
	public class ServiceController : Controller
	{
		private readonly CustomerManagementSystemContext _dbContext;
		public ServiceController(CustomerManagementSystemContext dbContext)
		{
			_dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
		}


		public List<Service> Services { get; set; } = new List<Service>();
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
			if (!UserId.HasValue)
			{
				return RedirectToAction("Login", "Access");
			}
			return View();
		}

		[HttpDelete]
		public async Task<IActionResult> Delete(int id)
		{
			if (!UserId.HasValue)
				return Json(new { success = false, message = "Geçersiz kullanıcı oturumu." });

			var item = await _dbContext.Services.FindAsync(id);

			if (item == null)
			{
				return Json(new { success = false, message = "Service not found." });
			}

			_dbContext.Services.Remove(item);
			await _dbContext.SaveChangesAsync();

			return Json(new { success = true });
		}



		[HttpPost]
		public JsonResult SubmitAction(Service service)
		{
			if (!UserId.HasValue)
				return Json(new { success = false, message = "Geçersiz kullanıcı oturumu." });

			try
			{
				service.CreaterUserId = UserId.Value;
				_dbContext.Services.Add(service);
				_dbContext.SaveChanges();
				return Json(new { success = true, message = "Service added successfully" });
			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = "Error adding service: " + ex.Message });
			}

		}

		[HttpGet]
		public JsonResult GetList()
		{
			if (!UserId.HasValue)
				return Json(new { success = false, message = "Geçersiz kullanıcı oturumu." });

			var services = _dbContext.Services
			.Where(p => p.CreaterUserId == UserId)
			.OrderBy(c => c.Id)
			.Select(c => new
			{
				c.Id,
				c.Description,
				c.CreaterUserId
			})
			.ToList();

			return Json(services);

		}

		public IActionResult UpdateService(DB.Service service)
		{
			if (!UserId.HasValue)
				return Json(new { success = false, message = "Geçersiz kullanıcı oturumu." });

			if (service == null || service.Id == 0)
			{
				return BadRequest("Geçersiz müşteri verisi.");
			}

			var existingCustomer = _dbContext.Services
			.Where(p => p.CreaterUserId == UserId)
			.FirstOrDefault(c => c.Id == service.Id);

			if (existingCustomer == null)
			{
				return NotFound("Müşteri bulunamadı.");
			}
			existingCustomer.CreaterUserId = UserId.Value;
			existingCustomer.Description = service.Description;

			_dbContext.SaveChanges();

			return Ok(new { message = "Müşteri başarıyla güncellendi." });
		}

	}
}
