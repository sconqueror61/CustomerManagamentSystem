using CustomerManagementSystem.DB;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CustomerManagementSystem.Controllers
{
	public class StartingController : Controller
	{
		private readonly CustomerManagementSystemContext _context;

		public StartingController(CustomerManagementSystemContext context)
		{
			_context = context;
		}

		// GET: Starting
		public async Task<IActionResult> Index()
		{
			return View(await _context.Users.ToListAsync());
		}

		// GET: Starting/Details/5
		public async Task<IActionResult> Details(int? id)
		{
			if (id == null)
			{
				return NotFound();
			}

			var user = await _context.Users
				.FirstOrDefaultAsync(m => m.Id == id);
			if (user == null)
			{
				return NotFound();
			}

			return View(user);
		}

		// GET: Starting/Create
		public IActionResult Create()
		{
			return View();
		}

		// POST: Starting/Create
		// To protect from overposting attacks, enable the specific properties you want to bind to.
		// For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create([Bind("Id,UserTypeId,Password,Email,Name,SurName,Adress")] User user)
		{
			if (ModelState.IsValid)
			{
				_context.Add(user);
				await _context.SaveChangesAsync();
				return RedirectToAction(nameof(Index));
			}
			return View(user);
		}

		// GET: Starting/Edit/5
		public async Task<IActionResult> Edit(int? id)
		{
			if (id == null)
			{
				return NotFound();
			}

			var user = await _context.Users.FindAsync(id);
			if (user == null)
			{
				return NotFound();
			}
			return View(user);
		}

		// POST: Starting/Edit/5
		// To protect from overposting attacks, enable the specific properties you want to bind to.
		// For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(int id, [Bind("Id,UserTypeId,Password,Email,Name,SurName,Adress")] User user)
		{
			if (id != user.Id)
			{
				return NotFound();
			}

			if (ModelState.IsValid)
			{
				try
				{
					_context.Update(user);
					await _context.SaveChangesAsync();
				}
				catch (DbUpdateConcurrencyException)
				{
					if (!UserExists(user.Id))
					{
						return NotFound();
					}
					else
					{
						throw;
					}
				}
				return RedirectToAction(nameof(Index));
			}
			return View(user);
		}

		// GET: Starting/Delete/5
		public async Task<IActionResult> Delete(int? id)
		{
			if (id == null)
			{
				return NotFound();
			}

			var user = await _context.Users
				.FirstOrDefaultAsync(m => m.Id == id);
			if (user == null)
			{
				return NotFound();
			}

			return View(user);
		}

		// POST: Starting/Delete/5
		[HttpPost, ActionName("Delete")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteConfirmed(int id)
		{
			var user = await _context.Users.FindAsync(id);
			if (user != null)
			{
				_context.Users.Remove(user);
			}

			await _context.SaveChangesAsync();
			return RedirectToAction(nameof(Index));
		}

		private bool UserExists(int id)
		{
			return _context.Users.Any(e => e.Id == id);
		}

		public async Task<IActionResult> LogOut()
		{
			await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
			return RedirectToAction("Login", "Access");
		}
	}
}
