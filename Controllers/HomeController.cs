using CustomerManagementSystem.DB;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CustomerManagementSystem.Controllers
{
	[Authorize]
	public class HomeController : Controller
	{
		private readonly CustomerManagementSystemContext _dbContext;

		public HomeController(CustomerManagementSystemContext dbContext)
		{
			_dbContext = dbContext;
		}

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

			return View(new List<Customer>());
		}

		[HttpPost]
		public async Task<IActionResult> Delete(int id)
		{
			if (!UserId.HasValue)
				return Json(new { success = false, message = "Geçersiz kullanýcý oturumu." });

			using var transaction = await _dbContext.Database.BeginTransactionAsync();

			try
			{
				var customer = await _dbContext.Customers
					.Where(p => p.CreaterUserId == UserId)
					.FirstOrDefaultAsync(c => c.Id == id && c.CreaterUserId == UserId.Value);

				if (customer == null)
				{
					return Json(new { success = false, message = "Customer not found or not authorized." });
				}

				_dbContext.Customers.Remove(customer);
				await _dbContext.SaveChangesAsync();

				await transaction.CommitAsync();

				return Json(new { success = true });
			}
			catch (Exception ex)
			{
				await transaction.RollbackAsync();
				return Json(new { success = false, message = "Hata oluþtu: " + ex.Message });
			}
		}

		[HttpPost]
		public JsonResult SubmitAction(DB.Customer customer)
		{
			if (!UserId.HasValue)
				return Json(new { success = false, message = "User ID is missing or not found." });

			if (customer == null || string.IsNullOrWhiteSpace(customer.Code))
				return Json(new { success = false, message = "Geçersiz müþteri verisi veya eksik kod." });

			customer.CreaterUserId = UserId.Value;

			_dbContext.Add(customer);
			_dbContext.SaveChanges();

			return Json(new { success = true, message = "Customer baþarýyla eklendi." });
		}

		[HttpGet]
		public JsonResult GetList()
		{
			if (!UserId.HasValue)
				return Json(new { success = false, message = "Geçersiz kullanýcý ID'si." });

			var customers = _dbContext.Customers
				.Where(c => !c.IsDeleted && c.CreaterUserId == UserId.Value)
				.OrderBy(c => c.Id)
				.Select(c => new
				{
					c.Id,
					c.CompanyName,
					c.Referance,
					c.Code,
					c.ContactName,
					c.Mail,
					c.Tel,
					c.ServiceArea,
					c.ServiceId,
					c.CreaterUserId
				})
				.ToList();

			return Json(new { customers });
		}

		[HttpGet]
		public JsonResult GetCustomer(int id)
		{
			if (!UserId.HasValue)
				return Json(new { success = false, message = "Geçersiz kullanýcý." });

			var customer = _dbContext.Customers
				.Where(c => !c.IsDeleted && c.Id == id && c.CreaterUserId == UserId.Value)
				.Select(c => new
				{
					c.Id,
					c.CompanyName,
					c.Referance,
					c.Code,
					c.ContactName,
					c.Mail,
					c.Tel,
					c.ServiceArea,
					c.IsDeleted,
					c.ServiceId,
					c.CreaterUserId
				})
				.FirstOrDefault();

			if (customer == null)
				return Json(new { success = false, message = "Müþteri bulunamadý." });

			return Json(new { success = true, customer });
		}

		[HttpPost]
		public IActionResult UpdateCustomer(DB.Customer customer)
		{
			if (!UserId.HasValue)
				return BadRequest("Geçersiz kullanýcý oturumu.");

			if (customer == null || customer.Id == 0 || string.IsNullOrWhiteSpace(customer.Code))
				return BadRequest("Geçersiz müþteri verisi.");

			var existingCustomer = _dbContext.Customers
				.Where(p => p.CreaterUserId == UserId)
				.FirstOrDefault(c => c.Id == customer.Id && c.CreaterUserId == UserId.Value);

			if (existingCustomer == null)
				return NotFound("Müþteri bulunamadý veya yetkiniz yok.");

			existingCustomer.CompanyName = customer.CompanyName;
			existingCustomer.Referance = customer.Referance;
			existingCustomer.Code = customer.Code;
			existingCustomer.ContactName = customer.ContactName;
			existingCustomer.Mail = customer.Mail;
			existingCustomer.Tel = customer.Tel;
			existingCustomer.ServiceArea = customer.ServiceArea;
			existingCustomer.ServiceId = customer.ServiceId;

			_dbContext.SaveChanges();

			return Ok(new { message = "Müþteri baþarýyla güncellendi." });
		}

	}
}
