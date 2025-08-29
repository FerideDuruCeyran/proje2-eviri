using ExcelUploader.Data;

namespace ExcelUploader.Services
{
    public interface IJwtService
    {
        string GenerateToken(ApplicationUser user);
        bool ValidateToken(string token);
        string GetUserIdFromToken(string token);
    }
}
