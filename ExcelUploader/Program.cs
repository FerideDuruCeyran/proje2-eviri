using ExcelUploader.Data;
using ExcelUploader.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Add HttpContextAccessor for IP address detection
builder.Services.AddHttpContextAccessor();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

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

// Add JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var jwtSettings = builder.Configuration.GetSection("Jwt");
    var key = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"] ?? "your-super-secret-key-with-at-least-32-characters");
    
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ClockSkew = TimeSpan.Zero
    };
});

// Add Services
builder.Services.AddScoped<IExcelService, ExcelService>();
builder.Services.AddScoped<IDataImportService, DataImportService>();
builder.Services.AddScoped<IPortService, PortService>();
builder.Services.AddScoped<IDynamicTableService, DynamicTableService>();
builder.Services.AddScoped<IUserLoginLogService, UserLoginLogService>();
builder.Services.AddScoped<ILoginLogService, LoginLogService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IExcelAnalyzerService, ExcelAnalyzerService>();

var app = builder.Build();

// Add global exception handling
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Global error: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
        throw;
    }
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/api/home/error");
    app.UseHsts();
}

// Remove HTTPS redirection for development
// app.UseHttpsRedirection();
app.UseStaticFiles(); // Enable static files for wwwroot
app.UseRouting();

// Use CORS
app.UseCors("AllowAll");

// Use Authentication and Authorization
app.UseAuthentication();
app.UseAuthorization();

// Map API root endpoint to redirect to index.html
app.MapGet("/", () => Results.Redirect("/index.html"));

// Map HTML pages
app.MapGet("/login", () => Results.File("login.html", "text/html"));
app.MapGet("/register", () => Results.File("register.html", "text/html"));
app.MapGet("/home", () => Results.File("home.html", "text/html"));
app.MapGet("/upload", () => Results.File("upload.html", "text/html"));
app.MapGet("/data", () => Results.File("data.html", "text/html"));
app.MapGet("/tables", () => Results.File("tables.html", "text/html"));
app.MapGet("/connections", () => Results.File("connections.html", "text/html"));
app.MapGet("/login-logs", () => Results.File("login-logs.html", "text/html"));
app.MapGet("/profile", () => Results.File("profile.html", "text/html"));
app.MapGet("/excel-analysis", () => Results.File("excel-analysis.html", "text/html"));
app.MapGet("/test-auth", () => Results.File("test-auth.html", "text/html"));

// Map controllers
app.MapControllers();

// Add a simple database test endpoint
app.MapGet("/api/test-db", async (ApplicationDbContext context) =>
{
    try
    {
        var canConnect = await context.Database.CanConnectAsync();
        if (canConnect)
        {
            return Results.Ok(new { 
                success = true, 
                message = "Database connection successful",
                timestamp = DateTime.UtcNow
            });
        }
        else
        {
            return Results.Json(new { 
                success = false, 
                message = "Database connection failed",
                timestamp = DateTime.UtcNow
            }, statusCode: 500);
        }
    }
    catch (Exception ex)
    {
        return Results.Json(new { 
            success = false, 
            message = "Database test failed",
            error = ex.Message,
            timestamp = DateTime.UtcNow
        }, statusCode: 500);
    }
});

app.Run();
