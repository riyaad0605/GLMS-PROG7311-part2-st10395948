using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using GLMS.Web.Data;
using GLMS.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// ── MVC ──────────────────────────────────────────
builder.Services.AddControllersWithViews();

// ── Entity Framework + Identity ──────────────────
builder.Services.AddDbContext<GlmsDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = true;
    options.SignIn.RequireConfirmedEmail = false;
})
.AddEntityFrameworkStores<GlmsDbContext>()
.AddDefaultTokenProviders();

// Configure login/logout paths
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

// ── Currency Service ──────────────────────────────
builder.Services.AddHttpClient<ICurrencyService, CurrencyService>();

// ── File Service ──────────────────────────────────
builder.Services.AddScoped<IFileService, FileService>();

// ── Email Service ─────────────────────────────────
var emailSettings = builder.Configuration
    .GetSection("EmailSettings")
    .Get<EmailSettings>() ?? new EmailSettings();

builder.Services.AddSingleton(emailSettings);
builder.Services.AddScoped<IEmailService, EmailService>();

// ── Build ─────────────────────────────────────────
var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
