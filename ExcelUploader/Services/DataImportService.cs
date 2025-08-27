using ExcelUploader.Data;
using ExcelUploader.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace ExcelUploader.Services
{
    public class DataImportService : IDataImportService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DataImportService> _logger;
        private readonly IExcelService _excelService;

        public DataImportService(ApplicationDbContext context, ILogger<DataImportService> logger, IExcelService excelService)
        {
            _context = context;
            _logger = logger;
            _excelService = excelService;
        }

        public async Task<bool> ImportDataAsync(List<ExcelData> data)
        {
            try
            {
                if (data == null || !data.Any())
                    return false;

                // Check for duplicates based on key fields
                var existingData = await _context.ExcelData
                    .Where(e => data.Any(d => 
                        (d.TCKimlikNo != null && d.TCKimlikNo == e.TCKimlikNo) ||
                        (d.OgrenciNo != null && d.OgrenciNo == e.OgrenciNo) ||
                        (d.Ad != null && d.Soyad != null && d.Ad == e.Ad && d.Soyad == e.Soyad)))
                    .ToListAsync();

                if (existingData.Any())
                {
                    _logger.LogWarning($"Duplicate data found for {existingData.Count} records");
                }

                await _context.ExcelData.AddRangeAsync(data);
                var result = await _context.SaveChangesAsync();

                _logger.LogInformation($"Successfully imported {result} records to database");
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing data to database");
                return false;
            }
        }

        public async Task<List<ExcelData>> GetAllDataAsync()
        {
            try
            {
                return await _context.ExcelData
                    .OrderByDescending(e => e.UploadDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all data from database");
                return new List<ExcelData>();
            }
        }

        public async Task<ExcelData?> GetDataByIdAsync(int id)
        {
            try
            {
                return await _context.ExcelData.FindAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving data with ID {id} from database");
                return null;
            }
        }

        public async Task<bool> UpdateDataAsync(ExcelData data)
        {
            try
            {
                var existingData = await _context.ExcelData.FindAsync(data.Id);
                if (existingData == null)
                    return false;

                // Update properties
                existingData.BasvuruYili = data.BasvuruYili;
                existingData.HareketlilikTipi = data.HareketlilikTipi;
                existingData.BasvuruTipi = data.BasvuruTipi;
                existingData.Ad = data.Ad;
                existingData.Soyad = data.Soyad;
                existingData.OdemeTipi = data.OdemeTipi;
                existingData.Taksit = data.Taksit;
                existingData.Odenecek = data.Odenecek;
                existingData.Odendiginde = data.Odendiginde;
                existingData.OdemeTarihi = data.OdemeTarihi;
                existingData.Aciklama = data.Aciklama;
                existingData.OdemeOrani = data.OdemeOrani;
                existingData.KullaniciAdi = data.KullaniciAdi;
                existingData.TCKimlikNo = data.TCKimlikNo;
                existingData.PasaportNo = data.PasaportNo;
                existingData.DogumTarihi = data.DogumTarihi;
                existingData.DogumYeri = data.DogumYeri;
                existingData.Cinsiyet = data.Cinsiyet;
                existingData.AdresIl = data.AdresIl;
                existingData.AdresUlke = data.AdresUlke;
                existingData.BankaHesapSahibi = data.BankaHesapSahibi;
                existingData.BankaAdi = data.BankaAdi;
                existingData.BankaSubeKodu = data.BankaSubeKodu;
                existingData.BankaSubeAdi = data.BankaSubeAdi;
                existingData.BankaHesapNumarasi = data.BankaHesapNumarasi;
                existingData.BankaIBANNo = data.BankaIBANNo;
                existingData.OgrenciNo = data.OgrenciNo;
                existingData.FakulteAdi = data.FakulteAdi;
                existingData.BirimAdi = data.BirimAdi;
                existingData.DiplomaDerecesi = data.DiplomaDerecesi;
                existingData.Sinif = data.Sinif;
                existingData.BasvuruAciklama = data.BasvuruAciklama;
                existingData.GaziSehitYakini = data.GaziSehitYakini;
                existingData.YurtBasvurusu = data.YurtBasvurusu;
                existingData.AkademikOrtalama = data.AkademikOrtalama;
                existingData.TercihSirasi = data.TercihSirasi;
                existingData.TercihDurumu = data.TercihDurumu;
                existingData.BasvuruDurumu = data.BasvuruDurumu;
                existingData.Burs = data.Burs;
                existingData.AkademikYil = data.AkademikYil;
                existingData.AkademikDonem = data.AkademikDonem;
                existingData.BasvuruTarihi = data.BasvuruTarihi;
                existingData.DegisimProgramiTipi = data.DegisimProgramiTipi;
                existingData.KatilmakIstedigiYabanciDilSinavi = data.KatilmakIstedigiYabanciDilSinavi;
                existingData.SistemDisiGecmisHareketlilik = data.SistemDisiGecmisHareketlilik;
                existingData.SistemIciGecmisHareketlilikBilgisi = data.SistemIciGecmisHareketlilikBilgisi;
                existingData.Tercihler = data.Tercihler;
                existingData.HibeSozlesmeTipi = data.HibeSozlesmeTipi;
                existingData.HibeButceYili = data.HibeButceYili;
                existingData.HibeOdemeOrani = data.HibeOdemeOrani;
                existingData.HibeOdeneceklerToplami = data.HibeOdeneceklerToplami;
                existingData.HibeOdenenlerToplami = data.HibeOdenenlerToplami;
                existingData.HareketlilikBaslangicTarihi = data.HareketlilikBaslangicTarihi;
                existingData.HareketlilikBitisTarihi = data.HareketlilikBitisTarihi;
                existingData.PlanlananToplamHibeliGunSayisi = data.PlanlananToplamHibeliGunSayisi;
                existingData.GerceklesenToplamHibeliGun = data.GerceklesenToplamHibeliGun;
                existingData.UniversiteKoordinatoru = data.UniversiteKoordinatoru;
                existingData.UniversiteKoordinatoruEmail = data.UniversiteKoordinatoruEmail;
                existingData.UniversiteUluslararasiKodu = data.UniversiteUluslararasiKodu;
                existingData.SinavTipi = data.SinavTipi;
                existingData.SinavPuani = data.SinavPuani;
                existingData.SinavTarihi = data.SinavTarihi;
                existingData.SinavDili = data.SinavDili;
                existingData.Unvan = data.Unvan;
                existingData.UzmanlikAlani = data.UzmanlikAlani;
                existingData.UniversitedeToplamCalismaSuresi = data.UniversitedeToplamCalismaSuresi;
                existingData.BasvuruSayfasi = data.BasvuruSayfasi;

                existingData.IsProcessed = true;
                existingData.ProcessedDate = DateTime.UtcNow;

                var result = await _context.SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating data with ID {data.Id}");
                return false;
            }
        }

        public async Task<bool> DeleteDataAsync(int id)
        {
            try
            {
                var data = await _context.ExcelData.FindAsync(id);
                if (data == null)
                    return false;

                _context.ExcelData.Remove(data);
                var result = await _context.SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting data with ID {id}");
                return false;
            }
        }

        public async Task<bool> DeleteDataByFileNameAsync(string fileName)
        {
            try
            {
                var dataToDelete = await _context.ExcelData
                    .Where(e => e.FileName == fileName)
                    .ToListAsync();

                if (!dataToDelete.Any())
                    return false;

                _context.ExcelData.RemoveRange(dataToDelete);
                var result = await _context.SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting data for file {fileName}");
                return false;
            }
        }

        public async Task<DashboardViewModel> GetDashboardDataAsync()
        {
            try
            {
                var totalRecords = await GetTotalRecordCountAsync();
                var totalGrantAmount = await GetTotalGrantAmountAsync();
                var totalPaidAmount = await GetTotalPaidAmountAsync();

                var recentUploads = await _context.ExcelData
                    .OrderByDescending(e => e.UploadDate)
                    .Take(10)
                    .ToListAsync();

                var monthlyData = await _context.ExcelData
                    .Where(e => e.UploadDate >= DateTime.UtcNow.AddMonths(-12))
                    .GroupBy(e => new { Month = e.UploadDate.Month, Year = e.UploadDate.Year })
                    .Select(g => new ChartData
                    {
                        Month = $"{g.Key.Year}-{g.Key.Month:00}",
                        Amount = g.Sum(e => e.Odenecek ?? 0),
                        Count = g.Count()
                    })
                    .OrderBy(c => c.Month)
                    .ToListAsync();

                return new DashboardViewModel
                {
                    TotalRecords = totalRecords,
                    ProcessedRecords = await _context.ExcelData.CountAsync(e => e.IsProcessed),
                    PendingRecords = await _context.ExcelData.CountAsync(e => !e.IsProcessed),
                    TotalGrantAmount = totalGrantAmount,
                    TotalPaidAmount = totalPaidAmount,
                    RecentUploads = recentUploads,
                    MonthlyData = monthlyData
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving dashboard data");
                return new DashboardViewModel();
            }
        }

        public async Task<List<ExcelData>> SearchDataAsync(string searchTerm, string? filterBy = null, string? filterValue = null)
        {
            try
            {
                var query = _context.ExcelData.AsQueryable();

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    query = query.Where(e =>
                        (e.Ad != null && e.Ad.Contains(searchTerm)) ||
                        (e.Soyad != null && e.Soyad.Contains(searchTerm)) ||
                        (e.TCKimlikNo != null && e.TCKimlikNo.Contains(searchTerm)) ||
                        (e.OgrenciNo != null && e.OgrenciNo.Contains(searchTerm)) ||
                        (e.FileName != null && e.FileName.Contains(searchTerm))
                    );
                }

                if (!string.IsNullOrWhiteSpace(filterBy) && !string.IsNullOrWhiteSpace(filterValue))
                {
                    switch (filterBy.ToLower())
                    {
                        case "basvuruyili":
                            query = query.Where(e => e.BasvuruYili == filterValue);
                            break;
                        case "hareketliliktipi":
                            query = query.Where(e => e.HareketlilikTipi == filterValue);
                            break;
                        case "odemetipi":
                            query = query.Where(e => e.OdemeTipi == filterValue);
                            break;
                        case "filename":
                            query = query.Where(e => e.FileName == filterValue);
                            break;
                        case "isprocessed":
                            if (bool.TryParse(filterValue, out var isProcessed))
                                query = query.Where(e => e.IsProcessed == isProcessed);
                            break;
                    }
                }

                return await query.OrderByDescending(e => e.UploadDate).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching data");
                return new List<ExcelData>();
            }
        }

        public async Task<DataListViewModel> GetPaginatedDataAsync(int page, int pageSize, string? searchTerm = null, string? sortBy = null, string? sortOrder = null)
        {
            try
            {
                var query = _context.ExcelData.AsQueryable();

                // Apply search
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    query = query.Where(e =>
                        (e.Ad != null && e.Ad.Contains(searchTerm)) ||
                        (e.Soyad != null && e.Soyad.Contains(searchTerm)) ||
                        (e.TCKimlikNo != null && e.TCKimlikNo.Contains(searchTerm)) ||
                        (e.OgrenciNo != null && e.OgrenciNo.Contains(searchTerm)) ||
                        (e.FileName != null && e.FileName.Contains(searchTerm))
                    );
                }

                // Apply sorting
                if (!string.IsNullOrWhiteSpace(sortBy))
                {
                    query = sortBy.ToLower() switch
                    {
                        "ad" => sortOrder == "desc" ? query.OrderByDescending(e => e.Ad) : query.OrderBy(e => e.Ad),
                        "soyad" => sortOrder == "desc" ? query.OrderByDescending(e => e.Soyad) : query.OrderBy(e => e.Soyad),
                        "uploaddate" => sortOrder == "desc" ? query.OrderByDescending(e => e.UploadDate) : query.OrderBy(e => e.UploadDate),
                        "filename" => sortOrder == "desc" ? query.OrderByDescending(e => e.FileName) : query.OrderBy(e => e.FileName),
                        _ => query.OrderByDescending(e => e.UploadDate)
                    };
                }
                else
                {
                    query = query.OrderByDescending(e => e.UploadDate);
                }

                var totalCount = await query.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                var data = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return new DataListViewModel
                {
                    ExcelData = data,
                    CurrentPage = page,
                    TotalPages = totalPages,
                    PageSize = pageSize,
                    SearchTerm = searchTerm,
                    SortBy = sortBy,
                    SortOrder = sortOrder
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving paginated data");
                return new DataListViewModel();
            }
        }

        public async Task<byte[]> ExportDataToExcelAsync(List<ExcelData> data)
        {
            try
            {
                return await _excelService.ExportToExcelAsync(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting data to Excel");
                throw;
            }
        }

        public async Task<int> GetTotalRecordCountAsync()
        {
            try
            {
                return await _context.ExcelData.CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting total record count");
                return 0;
            }
        }

        public async Task<decimal> GetTotalGrantAmountAsync()
        {
            try
            {
                return await _context.ExcelData.SumAsync(e => e.Odenecek ?? 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting total grant amount");
                return 0;
            }
        }

        public async Task<decimal> GetTotalPaidAmountAsync()
        {
            try
            {
                return await _context.ExcelData.SumAsync(e => e.Odendiginde ?? 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting total paid amount");
                return 0;
            }
        }
    }
}
