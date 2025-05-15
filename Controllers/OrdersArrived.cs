using Microsoft.AspNetCore.Mvc;

namespace CustomerManagementSystem.Controllers
{
	public class OrdersArrived : Controller
	{
		public IActionResult Index()
		{
			return View();
		}
	}
}
