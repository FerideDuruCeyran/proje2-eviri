using ExcelUploader.Data;
using ExcelUploader.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Add HttpContextAccessor for IP address detection
builder.Services.AddHttpContextAccessor();

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

// Add Services
builder.Services.AddScoped<IExcelService, ExcelService>();
builder.Services.AddScoped<IDataImportService, DataImportService>();
builder.Services.AddScoped<IPortService, PortService>();
builder.Services.AddScoped<IDynamicTableService, DynamicTableService>();
builder.Services.AddScoped<IUserLoginLogService, UserLoginLogService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/api/home/error");
    app.UseHsts();
}

app.UseRouting();
app.UseStaticFiles();

// Map HTML pages
app.MapGet("/", () => Results.File("index.html", "text/html"));
app.MapGet("/login", () => Results.File("login.html", "text/html"));
app.MapGet("/register", () => Results.File("register.html", "text/html"));
app.MapGet("/upload", () => Results.File("upload.html", "text/html"));
app.MapGet("/data", () => Results.File("data.html", "text/html"));
app.MapGet("/tables", () => Results.File("tables.html", "text/html"));
app.MapGet("/profile", () => Results.File("profile.html", "text/html"));
app.MapGet("/logout", () => Results.File("logout.html", "text/html"));
app.MapGet("/login-logs", () => Results.File("login-logs.html", "text/html"));
app.MapGet("/sql-test", () => Results.File("tables.html", "text/html"));

app.UseAuthentication();
app.UseAuthorization();

// Map controllers
app.MapControllers();

app.Run();
