using CustomerManagementSystem.DB;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Veritabaný baðlamýný (DbContext) servis konteynerine ekleyelim
builder.Services.AddDbContext<CustomerManagementSystemContext>(options =>
	options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Authentication yapýlandýrmasý
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
	options.LoginPath = "/Access/Login";
	options.ExpireTimeSpan = TimeSpan.FromMinutes(20);
});

// Session yapýlandýrmasý
builder.Services.AddDistributedMemoryCache(); // Session için memory cache
builder.Services.AddSession(options =>
{
	options.IdleTimeout = TimeSpan.FromMinutes(30); // Oturum süresi
	options.Cookie.HttpOnly = true;
	options.Cookie.IsEssential = true;
});

builder.Services.AddControllersWithViews().AddRazorRuntimeCompilation();
builder.Services.AddAuthorization();
builder.Services.AddHttpClient();

var app = builder.Build();

// HTTP isteði iþlem hattýný (middleware) yapýlandýrma
if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Home/Error");
	app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Session middleware'ini ekliyoruz
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Access}/{action=Login}/{id?}");

app.Run();
