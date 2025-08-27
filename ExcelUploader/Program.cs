using ExcelUploader.Data;
using ExcelUploader.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Server.IIS;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers(); // Remove Views, use only Controllers

// Add DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Add Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/api/account/login";
        options.LogoutPath = "/api/account/logout";
        options.AccessDeniedPath = "/api/account/access-denied";
        options.ExpireTimeSpan = TimeSpan.FromHours(2);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });

// Add Authorization - Allow anonymous access to login and register
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    
    // Allow anonymous access to specific endpoints
    options.AddPolicy("AllowAnonymous", policy =>
        policy.RequireAssertion(_ => true));
});

// Add Services
builder.Services.AddScoped<IExcelService, ExcelService>();
builder.Services.AddScoped<IDataImportService, DataImportService>();
builder.Services.AddScoped<IPortService, PortService>();
builder.Services.AddScoped<IDynamicTableService, DynamicTableService>();

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(Program).Assembly);

// Add File Upload Configuration
builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = 52428800; // 50MB
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/api/home/error");
    app.UseHsts();
}

// Remove HTTPS redirection for development
// app.UseHttpsRedirection();

app.UseRouting();

// Serve static files
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

// Map controllers
app.MapControllers();

// Map minimal API endpoints
app.MapGet("/", () => Results.Ok(new { 
    message = "Excel Uploader API is running",
    version = "9.0",
    timestamp = DateTime.UtcNow,
    status = "Healthy"
}));
app.MapGet("/health", () => Results.Ok(new { 
    message = "Excel Uploader API is running",
    version = "9.0",
    timestamp = DateTime.UtcNow,
    status = "Healthy"
}));

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.EnsureCreated();
}

app.Run();
