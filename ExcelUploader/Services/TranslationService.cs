using System.Text.RegularExpressions;
using System.Globalization;

namespace ExcelUploader.Services
{
    public class TranslationService : ITranslationService
    {
        private readonly ILogger<TranslationService> _logger;
        private readonly Dictionary<string, string> _turkishToEnglishDictionary;
        private readonly Dictionary<string, string> _businessTermDictionary;
        private readonly Dictionary<string, string> _abbreviationDictionary;
        private readonly HashSet<string> _sqlReservedWords;

        public TranslationService(ILogger<TranslationService> logger)
        {
            _logger = logger;
            _turkishToEnglishDictionary = InitializeTurkishToEnglishDictionary();
            _businessTermDictionary = InitializeBusinessTermDictionary();
            _abbreviationDictionary = InitializeAbbreviationDictionary();
            _sqlReservedWords = InitializeSqlReservedWords();
        }

        public string TranslateColumnName(string excelColumnName, TranslationStrategy strategy = TranslationStrategy.Intelligent)
        {
            if (string.IsNullOrEmpty(excelColumnName))
                return "Column_" + Guid.NewGuid().ToString("N").Substring(0, 8);

            return strategy switch
            {
                TranslationStrategy.Simple => ApplySimpleTranslation(excelColumnName),
                TranslationStrategy.Intelligent => ApplyIntelligentTranslation(excelColumnName),
                TranslationStrategy.EnglishTranslation => ApplyEnglishTranslation(excelColumnName),
                TranslationStrategy.Abbreviated => ApplyAbbreviatedTranslation(excelColumnName),
                TranslationStrategy.Technical => ApplyTechnicalTranslation(excelColumnName),
                TranslationStrategy.Preserve => ApplyPreserveTranslation(excelColumnName),
                _ => ApplyIntelligentTranslation(excelColumnName)
            };
        }

        public string TranslateTableName(string excelTableName, TranslationStrategy strategy = TranslationStrategy.Intelligent)
        {
            if (string.IsNullOrEmpty(excelTableName))
                return "Table_" + Guid.NewGuid().ToString("N").Substring(0, 8);

            return strategy switch
            {
                TranslationStrategy.Simple => ApplySimpleTranslation(excelTableName),
                TranslationStrategy.Intelligent => ApplyIntelligentTranslation(excelTableName),
                TranslationStrategy.EnglishTranslation => ApplyEnglishTranslation(excelTableName),
                TranslationStrategy.Abbreviated => ApplyAbbreviatedTranslation(excelTableName),
                TranslationStrategy.Technical => ApplyTechnicalTranslation(excelTableName),
                TranslationStrategy.Preserve => ApplyPreserveTranslation(excelTableName),
                _ => ApplyIntelligentTranslation(excelTableName)
            };
        }

        public List<TranslationStrategy> GetAvailableStrategies()
        {
            return Enum.GetValues<TranslationStrategy>().ToList();
        }

        public Dictionary<TranslationStrategy, string> GetTranslationPreview(string excelColumnName)
        {
            var preview = new Dictionary<TranslationStrategy, string>();
            
            foreach (var strategy in GetAvailableStrategies())
            {
                try
                {
                    var translated = TranslateColumnName(excelColumnName, strategy);
                    preview[strategy] = translated;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error generating preview for strategy {Strategy}", strategy);
                    preview[strategy] = "ERROR";
                }
            }
            
            return preview;
        }

        public TranslationValidationResult ValidateTranslatedName(string translatedName)
        {
            var result = new TranslationValidationResult();
            
            if (string.IsNullOrEmpty(translatedName))
            {
                result.IsValid = false;
                result.Errors.Add("Name cannot be empty");
                return result;
            }

            // Check for SQL reserved words
            if (_sqlReservedWords.Contains(translatedName.ToUpper()))
            {
                result.IsValid = false;
                result.Errors.Add($"'{translatedName}' is a SQL reserved word");
                result.SuggestedName = "_" + translatedName;
            }

            // Check for invalid characters
            if (Regex.IsMatch(translatedName, @"[^a-zA-Z0-9_]"))
            {
                result.IsValid = false;
                result.Errors.Add("Name contains invalid characters (only letters, numbers, and underscores allowed)");
            }

            // Check length
            if (translatedName.Length > 128)
            {
                result.IsValid = false;
                result.Errors.Add("Name is too long (max 128 characters)");
                result.SuggestedName = translatedName.Substring(0, 128);
            }

            // Check if starts with number
            if (char.IsDigit(translatedName[0]))
            {
                result.Warnings.Add("Name starts with a number, which may cause issues");
                result.SuggestedName = "_" + translatedName;
            }

            // Check for consecutive underscores
            if (translatedName.Contains("__"))
            {
                result.Warnings.Add("Name contains consecutive underscores");
            }

            if (!result.Errors.Any())
            {
                result.IsValid = true;
            }

            return result;
        }

        private string ApplySimpleTranslation(string name)
        {
            // Current approach - simple character replacement
            var turkishToEnglish = new Dictionary<char, char>
            {
                {'ç', 'c'}, {'Ç', 'C'},
                {'ğ', 'g'}, {'Ğ', 'G'},
                {'ı', 'i'}, {'I', 'I'},
                {'ö', 'o'}, {'Ö', 'O'},
                {'ş', 's'}, {'Ş', 'S'},
                {'ü', 'u'}, {'Ü', 'U'},
                {'İ', 'I'}, {'i', 'i'}
            };

            var sanitized = name;
            
            foreach (var kvp in turkishToEnglish)
            {
                sanitized = sanitized.Replace(kvp.Key, kvp.Value);
            }

            sanitized = sanitized.Replace(" ", "_");
            sanitized = Regex.Replace(sanitized, @"[^a-zA-Z0-9_]", "_");
            sanitized = Regex.Replace(sanitized, @"_+", "_");
            sanitized = sanitized.Trim('_');
            
            if (sanitized.Length > 0 && char.IsDigit(sanitized[0]))
                sanitized = "_" + sanitized;
                
            return string.IsNullOrEmpty(sanitized) ? "Column_" + Guid.NewGuid().ToString("N").Substring(0, 8) : sanitized;
        }

        private string ApplyIntelligentTranslation(string name)
        {
            var translated = name.ToLowerInvariant();
            
            // Apply business term translations
            foreach (var term in _businessTermDictionary)
            {
                translated = Regex.Replace(translated, $@"\b{term.Key}\b", term.Value, RegexOptions.IgnoreCase);
            }
            
            // Apply Turkish to English translations
            foreach (var term in _turkishToEnglishDictionary)
            {
                translated = Regex.Replace(translated, $@"\b{term.Key}\b", term.Value, RegexOptions.IgnoreCase);
            }
            
            // Apply character replacements
            var turkishToEnglish = new Dictionary<char, char>
            {
                {'ç', 'c'}, {'Ç', 'C'},
                {'ğ', 'g'}, {'Ğ', 'G'},
                {'ı', 'i'}, {'I', 'I'},
                {'ö', 'o'}, {'Ö', 'O'},
                {'ş', 's'}, {'Ş', 'S'},
                {'ü', 'u'}, {'Ü', 'U'},
                {'İ', 'I'}, {'i', 'i'}
            };

            foreach (var kvp in turkishToEnglish)
            {
                translated = translated.Replace(kvp.Key, kvp.Value);
            }

            // Clean up
            translated = translated.Replace(" ", "_");
            translated = Regex.Replace(translated, @"[^a-zA-Z0-9_]", "_");
            translated = Regex.Replace(translated, @"_+", "_");
            translated = translated.Trim('_');
            
            if (translated.Length > 0 && char.IsDigit(translated[0]))
                translated = "_" + translated;
                
            return string.IsNullOrEmpty(translated) ? "Column_" + Guid.NewGuid().ToString("N").Substring(0, 8) : translated;
        }

        private string ApplyEnglishTranslation(string name)
        {
            var translated = name.ToLowerInvariant();
            
            // Apply Turkish to English translations
            foreach (var term in _turkishToEnglishDictionary)
            {
                translated = Regex.Replace(translated, $@"\b{term.Key}\b", term.Value, RegexOptions.IgnoreCase);
            }
            
            // Apply character replacements
            var turkishToEnglish = new Dictionary<char, char>
            {
                {'ç', 'c'}, {'Ç', 'C'},
                {'ğ', 'g'}, {'Ğ', 'G'},
                {'ı', 'i'}, {'I', 'I'},
                {'ö', 'o'}, {'Ö', 'O'},
                {'ş', 's'}, {'Ş', 'S'},
                {'ü', 'u'}, {'Ü', 'U'},
                {'İ', 'I'}, {'i', 'i'}
            };

            foreach (var kvp in turkishToEnglish)
            {
                translated = translated.Replace(kvp.Key, kvp.Value);
            }

            // Clean up
            translated = translated.Replace(" ", "_");
            translated = Regex.Replace(translated, @"[^a-zA-Z0-9_]", "_");
            translated = Regex.Replace(translated, @"_+", "_");
            translated = translated.Trim('_');
            
            if (translated.Length > 0 && char.IsDigit(translated[0]))
                translated = "_" + translated;
                
            return string.IsNullOrEmpty(translated) ? "Column_" + Guid.NewGuid().ToString("N").Substring(0, 8) : translated;
        }

        private string ApplyAbbreviatedTranslation(string name)
        {
            var translated = name.ToLowerInvariant();
            
            // Apply abbreviations
            foreach (var abbreviation in _abbreviationDictionary)
            {
                translated = Regex.Replace(translated, $@"\b{abbreviation.Key}\b", abbreviation.Value, RegexOptions.IgnoreCase);
            }
            
            // Apply character replacements
            var turkishToEnglish = new Dictionary<char, char>
            {
                {'ç', 'c'}, {'Ç', 'C'},
                {'ğ', 'g'}, {'Ğ', 'G'},
                {'ı', 'i'}, {'I', 'I'},
                {'ö', 'o'}, {'Ö', 'O'},
                {'ş', 's'}, {'Ş', 'S'},
                {'ü', 'u'}, {'Ü', 'U'},
                {'İ', 'I'}, {'i', 'i'}
            };

            foreach (var kvp in turkishToEnglish)
            {
                translated = translated.Replace(kvp.Key, kvp.Value);
            }

            // Clean up
            translated = translated.Replace(" ", "_");
            translated = Regex.Replace(translated, @"[^a-zA-Z0-9_]", "_");
            translated = Regex.Replace(translated, @"_+", "_");
            translated = translated.Trim('_');
            
            if (translated.Length > 0 && char.IsDigit(translated[0]))
                translated = "_" + translated;
                
            return string.IsNullOrEmpty(translated) ? "Column_" + Guid.NewGuid().ToString("N").Substring(0, 8) : translated;
        }

        private string ApplyTechnicalTranslation(string name)
        {
            var translated = name.ToLowerInvariant();
            
            // Apply technical naming conventions
            translated = Regex.Replace(translated, @"\b(ad|name)\b", "name", RegexOptions.IgnoreCase);
            translated = Regex.Replace(translated, @"\b(soyad|surname|lastname)\b", "surname", RegexOptions.IgnoreCase);
            translated = Regex.Replace(translated, @"\b(tc|kimlik|identity)\b", "identity", RegexOptions.IgnoreCase);
            translated = Regex.Replace(translated, @"\b(ogrenci|student)\b", "student", RegexOptions.IgnoreCase);
            translated = Regex.Replace(translated, @"\b(tarih|date)\b", "date", RegexOptions.IgnoreCase);
            translated = Regex.Replace(translated, @"\b(tutar|amount|price)\b", "amount", RegexOptions.IgnoreCase);
            translated = Regex.Replace(translated, @"\b(aciklama|description|note)\b", "description", RegexOptions.IgnoreCase);
            
            // Apply character replacements
            var turkishToEnglish = new Dictionary<char, char>
            {
                {'ç', 'c'}, {'Ç', 'C'},
                {'ğ', 'g'}, {'Ğ', 'G'},
                {'ı', 'i'}, {'I', 'I'},
                {'ö', 'o'}, {'Ö', 'O'},
                {'ş', 's'}, {'Ş', 'S'},
                {'ü', 'u'}, {'Ü', 'U'},
                {'İ', 'I'}, {'i', 'i'}
            };

            foreach (var kvp in turkishToEnglish)
            {
                translated = translated.Replace(kvp.Key, kvp.Value);
            }

            // Clean up
            translated = translated.Replace(" ", "_");
            translated = Regex.Replace(translated, @"[^a-zA-Z0-9_]", "_");
            translated = Regex.Replace(translated, @"_+", "_");
            translated = translated.Trim('_');
            
            if (translated.Length > 0 && char.IsDigit(translated[0]))
                translated = "_" + translated;
                
            return string.IsNullOrEmpty(translated) ? "Column_" + Guid.NewGuid().ToString("N").Substring(0, 8) : translated;
        }

        private string ApplyPreserveTranslation(string name)
        {
            var translated = name;
            
            // Only apply minimal changes to preserve original
            var turkishToEnglish = new Dictionary<char, char>
            {
                {'ç', 'c'}, {'Ç', 'C'},
                {'ğ', 'g'}, {'Ğ', 'G'},
                {'ı', 'i'}, {'I', 'I'},
                {'ö', 'o'}, {'Ö', 'O'},
                {'ş', 's'}, {'Ş', 'S'},
                {'ü', 'u'}, {'Ü', 'U'},
                {'İ', 'I'}, {'i', 'i'}
            };

            foreach (var kvp in turkishToEnglish)
            {
                translated = translated.Replace(kvp.Key, kvp.Value);
            }

            // Only replace spaces and invalid characters
            translated = translated.Replace(" ", "_");
            translated = Regex.Replace(translated, @"[^a-zA-Z0-9_]", "_");
            translated = Regex.Replace(translated, @"_+", "_");
            translated = translated.Trim('_');
            
            if (translated.Length > 0 && char.IsDigit(translated[0]))
                translated = "_" + translated;
                
            return string.IsNullOrEmpty(translated) ? "Column_" + Guid.NewGuid().ToString("N").Substring(0, 8) : translated;
        }

        private Dictionary<string, string> InitializeTurkishToEnglishDictionary()
        {
            return new Dictionary<string, string>
            {
                // Personal Information
                {"ad", "name"},
                {"soyad", "surname"},
                {"tc kimlik no", "identity_number"},
                {"tc kimlik", "identity"},
                {"kimlik no", "identity_number"},
                {"ogrenci no", "student_number"},
                {"ogrenci", "student"},
                {"dogum tarihi", "birth_date"},
                {"dogum yeri", "birth_place"},
                {"cinsiyet", "gender"},
                
                // Financial Terms
                {"tutar", "amount"},
                {"odenecek", "payable"},
                {"odenen", "paid"},
                {"odeme tarihi", "payment_date"},
                {"odeme", "payment"},
                {"fiyat", "price"},
                {"ucret", "fee"},
                {"maliyet", "cost"},
                {"para", "money"},
                {"oran", "rate"},
                {"yuzde", "percentage"},
                
                // Dates and Time
                {"tarih", "date"},
                {"zaman", "time"},
                {"baslangic", "start"},
                {"bitis", "end"},
                {"basvuru tarihi", "application_date"},
                {"son tarih", "deadline"},
                
                // Status and Boolean
                {"aktif", "active"},
                {"pasif", "passive"},
                {"evet", "yes"},
                {"hayir", "no"},
                {"var", "exists"},
                {"yok", "not_exists"},
                {"true", "true"},
                {"false", "false"},
                
                // Descriptions
                {"aciklama", "description"},
                {"detay", "detail"},
                {"not", "note"},
                {"yorum", "comment"},
                {"adres", "address"},
                {"icerik", "content"},
                
                // Numbers and IDs
                {"numara", "number"},
                {"no", "number"},
                {"id", "id"},
                {"kod", "code"},
                {"sira", "order"},
                {"index", "index"},
                {"yil", "year"},
                {"ay", "month"},
                {"gun", "day"}
            };
        }

        private Dictionary<string, string> InitializeBusinessTermDictionary()
        {
            return new Dictionary<string, string>
            {
                // Common business terms
                {"musteri", "customer"},
                {"siparis", "order"},
                {"urun", "product"},
                {"hizmet", "service"},
                {"fatura", "invoice"},
                {"rapor", "report"},
                {"kategori", "category"},
                {"departman", "department"},
                {"sirket", "company"},
                {"firma", "firm"},
                {"sube", "branch"},
                {"sehir", "city"},
                {"ulke", "country"},
                {"telefon", "phone"},
                {"email", "email"},
                {"e-posta", "email"},
                {"web sitesi", "website"},
                {"web", "web"},
                {"site", "site"},
                {"kullanici", "user"},
                {"sifre", "password"},
                {"giris", "login"},
                {"cikis", "logout"},
                {"kayit", "registration"},
                {"profil", "profile"},
                {"ayarlar", "settings"},
                {"konfigurasyon", "configuration"},
                {"durum", "status"},
                {"tip", "type"},
                {"tur", "type"},
                {"seviye", "level"},
                {"boyut", "size"},
                {"renk", "color"},
                {"marka", "brand"},
                {"model", "model"},
                {"versiyon", "version"},
                {"sistem", "system"},
                {"modul", "module"},
                {"fonksiyon", "function"},
                {"ozellik", "feature"},
                {"yetki", "permission"},
                {"rol", "role"},
                {"grup", "group"},
                {"takim", "team"},
                {"proje", "project"},
                {"gorev", "task"},
                {"hedef", "target"},
                {"amaç", "goal"},
                {"sonuc", "result"},
                {"basari", "success"},
                {"hata", "error"},
                {"uyari", "warning"},
                {"bilgi", "info"},
                {"mesaj", "message"},
                {"bildirim", "notification"},
                {"onay", "approval"},
                {"red", "rejection"},
                {"beklemede", "pending"},
                {"tamamlandi", "completed"},
                {"iptal", "cancelled"},
                {"silindi", "deleted"},
                {"guncellendi", "updated"},
                {"olusturuldu", "created"},
                {"degistirildi", "modified"}
            };
        }

        private Dictionary<string, string> InitializeAbbreviationDictionary()
        {
            return new Dictionary<string, string>
            {
                {"ad", "name"},
                {"soyad", "surname"},
                {"tc", "identity"},
                {"kimlik", "identity"},
                {"ogrenci", "student"},
                {"ogrenci no", "student_no"},
                {"dogum", "birth"},
                {"tarih", "date"},
                {"tutar", "amount"},
                {"odenecek", "payable"},
                {"odeme", "payment"},
                {"aciklama", "desc"},
                {"musteri", "customer"},
                {"siparis", "order"},
                {"urun", "product"},
                {"hizmet", "service"},
                {"fatura", "invoice"},
                {"rapor", "report"},
                {"kategori", "category"},
                {"departman", "dept"},
                {"sirket", "company"},
                {"firma", "firm"},
                {"sube", "branch"},
                {"sehir", "city"},
                {"ulke", "country"},
                {"telefon", "phone"},
                {"email", "email"},
                {"e-posta", "email"},
                {"web sitesi", "website"},
                {"kullanici", "user"},
                {"sifre", "password"},
                {"giris", "login"},
                {"cikis", "logout"},
                {"kayit", "reg"},
                {"profil", "profile"},
                {"ayarlar", "settings"},
                {"konfigurasyon", "config"},
                {"durum", "status"},
                {"tip", "type"},
                {"tur", "type"},
                {"seviye", "level"},
                {"boyut", "size"},
                {"renk", "color"},
                {"marka", "brand"},
                {"model", "model"},
                {"versiyon", "version"},
                {"sistem", "system"},
                {"modul", "module"},
                {"fonksiyon", "function"},
                {"ozellik", "feature"},
                {"yetki", "permission"},
                {"rol", "role"},
                {"grup", "group"},
                {"takim", "team"},
                {"proje", "project"},
                {"gorev", "task"},
                {"hedef", "target"},
                {"amaç", "goal"},
                {"sonuc", "result"},
                {"basari", "success"},
                {"hata", "error"},
                {"uyari", "warning"},
                {"bilgi", "info"},
                {"mesaj", "message"},
                {"bildirim", "notification"},
                {"onay", "approval"},
                {"red", "rejection"},
                {"beklemede", "pending"},
                {"tamamlandi", "completed"},
                {"iptal", "cancelled"},
                {"silindi", "deleted"},
                {"guncellendi", "updated"},
                {"olusturuldu", "created"},
                {"degistirildi", "modified"}
            };
        }

        private HashSet<string> InitializeSqlReservedWords()
        {
            return new HashSet<string>
            {
                "ADD", "ALL", "ALTER", "AND", "ANY", "AS", "ASC", "AUTHORIZATION", "BACKUP", "BEGIN", "BETWEEN", "BREAK", "BROWSE", "BULK", "BY", "CASCADE", "CASE", "CHECK", "CHECKPOINT", "CLOSE", "CLUSTERED", "COALESCE", "COLLATE", "COLUMN", "COMMIT", "COMPUTE", "CONSTRAINT", "CONTAINS", "CONTAINSTABLE", "CONTINUE", "CONVERT", "CREATE", "CROSS", "CURRENT", "CURRENT_DATE", "CURRENT_TIME", "CURRENT_TIMESTAMP", "CURRENT_USER", "CURSOR", "DATABASE", "DBCC", "DEALLOCATE", "DECLARE", "DEFAULT", "DELETE", "DENY", "DESC", "DISK", "DISTINCT", "DISTRIBUTED", "DROP", "DUMP", "ELSE", "END", "ERRLVL", "ESCAPE", "EXCEPT", "EXEC", "EXECUTE", "EXISTS", "EXIT", "EXTERNAL", "FETCH", "FILE", "FILLFACTOR", "FOR", "FOREIGN", "FREETEXT", "FREETEXTTABLE", "FROM", "FULL", "FUNCTION", "GOTO", "GRANT", "GROUP", "HAVING", "HOLDLOCK", "IDENTITY", "IDENTITY_INSERT", "IDENTITYCOL", "IF", "IN", "INDEX", "INNER", "INSERT", "INTERSECT", "INTO", "IS", "JOIN", "KEY", "KILL", "LEFT", "LIKE", "LINENO", "LOAD", "MERGE", "NATIONAL", "NOCHECK", "NONCLUSTERED", "NOT", "NULL", "NULLIF", "OF", "OFF", "OFFSETS", "ON", "OPEN", "OPENDATASOURCE", "OPENQUERY", "OPENROWSET", "OPENXML", "OPTION", "OR", "ORDER", "OUTER", "OVER", "PERCENT", "PIVOT", "PLAN", "PRECISION", "PRIMARY", "PRINT", "PROC", "PROCEDURE", "PUBLIC", "RAISERROR", "READ", "READTEXT", "RECONFIGURE", "REFERENCES", "REPLICATION", "RESTORE", "RESTRICT", "RETURN", "REVERT", "REVOKE", "RIGHT", "ROLLBACK", "ROWCOUNT", "ROWGUIDCOL", "RULE", "SAVE", "SCHEMA", "SECURITYAUDIT", "SELECT", "SEMANTICKEYPHRASETABLE", "SEMANTICSIMILARITYDETAILSTABLE", "SEMANTICSIMILARITYTABLE", "SESSION_USER", "SET", "SETUSER", "SHUTDOWN", "SOME", "STATISTICS", "TABLE", "TABLESAMPLE", "TEXTSIZE", "THEN", "TO", "TOP", "TRAN", "TRANSACTION", "TRIGGER", "TRUNCATE", "TRY_CONVERT", "TSEQUAL", "UNION", "UNIQUE", "UNPIVOT", "UPDATE", "UPDATETEXT", "USE", "USER", "VALUES", "VARYING", "VIEW", "WAITFOR", "WHEN", "WHERE", "WHILE", "WITH", "WITHIN GROUP", "WRITETEXT"
            };
        }
    }
}
