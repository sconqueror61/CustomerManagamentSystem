using CustomerManagementSystem.DB;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Veritabanı bağlamını (DbContext) servis konteynerine ekleyelim
builder.Services.AddDbContext<CustomerManagementSystemContext>(options =>
	options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Authentication yapılandırması
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
	.AddCookie(options =>
	{
		options.LoginPath = "/Access/Login";
		options.ExpireTimeSpan = TimeSpan.FromMinutes(20);
	});

// Session yapılandırması
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

// HTTP isteği işlem hattını (middleware) yapılandırma
if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Home/Error");
	app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Session middlewari ekleme
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();


app.Use(async (ctx, next) =>
{
	var user = ctx.User;
	if (user?.Identity?.IsAuthenticated == true &&
		user.FindFirst("UserTypeId")?.Value == "2")
	{
		var path = ctx.Request.Path + ctx.Request.QueryString;

		var existing = ctx.Request.Cookies["cust_paths"];
		var list = (existing ?? "")
			.Split('|', StringSplitOptions.RemoveEmptyEntries)
			.ToList();

		// Ardışık duplicate önleme (opsiyonel ama faydalı)
		if (list.Count == 0 || list[^1] != path)
			list.Add(path);

		// Son 10 kaydı koru
		if (list.Count > 10)
			list = list.Skip(list.Count - 10).ToList();

		var value = string.Join('|', list);

		ctx.Response.Cookies.Append("cust_paths", value, new CookieOptions
		{
			Path = "/",                    // site geneli erişim
			HttpOnly = true,               // JS okuyamaz; controller okuyabilir
			Secure = true,                 // HTTPS altında gönder
			SameSite = SameSiteMode.Lax,   // aynı site isteklerinde gönderilir
			Expires = DateTimeOffset.UtcNow.AddHours(12)
		});
	}

	await next();
});

app.MapControllers();

app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Access}/{action=Login}/{id?}");

app.Run();
