using CustomerManagementSystem.DB;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerManagementSystem.Controllers
{
	[Authorize]
	public class ProductController : Controller
	{
		private readonly IWebHostEnvironment _hostEnvironment;
		private readonly CustomerManagementSystemContext _dbContext;
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
		public ProductController(CustomerManagementSystemContext dbContext, IWebHostEnvironment hostEnvironment)
		{
			this._hostEnvironment = hostEnvironment;
			_dbContext = dbContext;
		}

		public List<Product> Products { get; set; } = new List<Product>();
		public List<Pimage> Pimages { get; set; } = new List<Pimage>();



		public IActionResult Index()
		{
			if (!UserId.HasValue)
				return Json(new { success = false, message = "Geçersiz kullanıcı oturumu." });
			return View();
		}

		public JsonResult AddProduct(DB.Product product, int categoryid)
		{
			if (!UserId.HasValue)
				return Json(new { success = false, message = "Geçersiz kullanıcı oturumu." });
			if (product == null)
			{
				return Json(new { success = false, message = "Invalid Data!" });
			}

			if (double.IsNaN(product.Price))
			{
				return Json(new { success = false, message = "Price field cannot be null or empty." });
			}

			product.CategoryId = categoryid;
			product.CreaterUserId = UserId.Value;

			//Category Descriptionu seçildi.
			//var categoryDesc = _dbContext.Pcategories
			//	.Where(x1 => x1.Id == categoryid)
			//	.Select(x1 => x1.CategoryDesc) 
			//	.FirstOrDefault(); 

			_dbContext.Add(product);
			_dbContext.SaveChanges();

			return Json(new { success = true, Id = product.Id, message = "Added successfully!" });
		}

		[HttpPost]
		public async Task<IActionResult> Create(List<IFormFile> formFiles, [FromForm] int productId)
		{
			if (!UserId.HasValue)
				return Json(new { success = false, message = "Geçersiz kullanıcı oturumu." });
			if (formFiles == null || formFiles.Count == 0)
			{
				return BadRequest("Lütfen en az bir dosya yükleyin.");
			}

			string wwwRootPath = _hostEnvironment.WebRootPath;
			string uploadsFolder = Path.Combine(wwwRootPath, "Image");

			if (!Directory.Exists(uploadsFolder))
			{
				Directory.CreateDirectory(uploadsFolder);
			}

			List<string> fileUrls = new List<string>();

			foreach (var file in formFiles)
			{
				string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
				string fileSavePath = Path.Combine(uploadsFolder, fileName);

				using (FileStream stream = new FileStream(fileSavePath, FileMode.Create))
				{
					await file.CopyToAsync(stream);
				}

				// Yeni Pimage kaydı oluştur
				var newImage = new Pimage
				{
					PictureUrl = "/Image/" + fileName,
					ProductId = productId,
					CreaterUserId = UserId.Value
				};

				_dbContext.Pimages.Add(newImage);
				fileUrls.Add(newImage.PictureUrl);
			}

			await _dbContext.SaveChangesAsync();
			ViewBag.Message = "Added successfully !";
			return Json(new { fileUrls });
		}
	}
}
