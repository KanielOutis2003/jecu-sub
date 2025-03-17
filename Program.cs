using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using SubdivisionWebsite.Data;
using SubdivisionWebsite.Models;
using SubdivisionWebsite.Services;
using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// Add Identity services
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
    options.SignIn.RequireConfirmedAccount = false;
    options.SignIn.RequireConfirmedEmail = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders()
.AddSignInManager<SignInManager<ApplicationUser>>();  // ✅ Explicitly add SignInManager

// Register ActivityLogService
builder.Services.AddScoped<IActivityLogService, ActivityLogService>();

// Add this line to configure cookie settings
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(1);
});

builder.Services.AddControllersWithViews();

// Add this after builder.Services.AddControllersWithViews()
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 10 * 1024 * 1024; // 10MB max file size
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
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

// Add this after your service configurations but before app.Run()
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        // Ensure default directories exist
        var webHostEnvironment = services.GetRequiredService<IWebHostEnvironment>();
        var uploadsFolder = Path.Combine(webHostEnvironment.WebRootPath, "uploads");
        var facilitiesFolder = Path.Combine(uploadsFolder, "facilities");
        var imagesFolder = Path.Combine(webHostEnvironment.WebRootPath, "images", "facilities");
        
        if (!Directory.Exists(uploadsFolder))
            Directory.CreateDirectory(uploadsFolder);
            
        if (!Directory.Exists(facilitiesFolder))
            Directory.CreateDirectory(facilitiesFolder);
            
        if (!Directory.Exists(imagesFolder))
            Directory.CreateDirectory(imagesFolder);
            
        // Create default facility images if they don't exist
        var defaultImagePath = Path.Combine(imagesFolder, "default-facility.jpg");
        if (!File.Exists(defaultImagePath))
        {
            using var fs = File.Create(defaultImagePath);
        }
        
        await DbInitializer.Initialize(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database or initializing directories.");
    }
}

app.Run();
