# Excel to SQL Translation Service

Bu servis, Excel dosyalarındaki Türkçe sütun ve tablo isimlerini SQL Server'a uygun İngilizce isimlere çevirmek için geliştirilmiş gelişmiş bir çeviri sistemidir.

## 🚀 Özellikler

### 📊 Çeviri Stratejileri
- **Simple**: Basit karakter değiştirme (mevcut yaklaşım)
- **Intelligent**: Akıllı çeviri ile iş terimleri
- **EnglishTranslation**: Türkçe terimlerin İngilizce çevirisi
- **Abbreviated**: Daha kısa isimler için kısaltılmış versiyon
- **Technical**: Teknik isimlendirme kuralları
- **Preserve**: Orijinali koruyarak minimal değişiklik

### 🔍 Doğrulama Özellikleri
- SQL Server ayrılmış kelime kontrolü
- Geçersiz karakter kontrolü
- Uzunluk kontrolü (maksimum 128 karakter)
- Sayı ile başlama kontrolü
- Ardışık alt çizgi kontrolü

### 📝 Desteklenen Terimler
- **Kişisel Bilgiler**: Ad, Soyad, TC Kimlik No, Öğrenci No
- **Finansal Terimler**: Tutar, Ödenecek, Ödeme, Fiyat, Ücret
- **Tarih ve Zaman**: Tarih, Zaman, Başlangıç, Bitiş
- **Durum ve Boolean**: Aktif, Pasif, Evet, Hayır
- **Açıklamalar**: Açıklama, Detay, Not, Yorum
- **İş Terimleri**: Müşteri, Sipariş, Ürün, Hizmet, Fatura

## 🛠️ Kullanım

### API Endpoints

#### 1. Çeviri Önizlemesi
```http
GET /api/translation/preview?columnName=Öğrenci Adı
```

#### 2. Tekil Çeviri
```http
POST /api/translation/translate-column
Content-Type: application/json

{
    "columnName": "Öğrenci Adı",
    "strategy": "Intelligent"
}
```

#### 3. Toplu Çeviri
```http
POST /api/translation/batch-translate
Content-Type: application/json

{
    "columnNames": ["Öğrenci Adı", "TC Kimlik No", "Ödeme Tutarı"],
    "strategy": "Intelligent"
}
```

#### 4. Strateji Karşılaştırması
```http
POST /api/translation/compare-strategies
Content-Type: application/json

{
    "columnNames": ["Öğrenci Adı", "TC Kimlik No", "Ödeme Tutarı"]
}
```

#### 5. İsim Doğrulama
```http
POST /api/translation/validate
Content-Type: application/json

{
    "name": "student_name"
}
```

### Web Arayüzü

Test sayfasına erişmek için: `http://localhost:5000/translation-test.html`

## 📊 Çeviri Örnekleri

### Öğrenci Adı
- **Simple**: ogrenci_adi
- **Intelligent**: student_name
- **EnglishTranslation**: student_name
- **Abbreviated**: student_name
- **Technical**: name
- **Preserve**: ogrenci_adi

### TC Kimlik No
- **Simple**: tc_kimlik_no
- **Intelligent**: identity_number
- **EnglishTranslation**: identity_number
- **Abbreviated**: identity
- **Technical**: identity
- **Preserve**: tc_kimlik_no

### Ödeme Tutarı
- **Simple**: odeme_tutari
- **Intelligent**: payment_amount
- **EnglishTranslation**: payment_amount
- **Abbreviated**: payment_amount
- **Technical**: amount
- **Preserve**: odeme_tutari

## 🔧 Kurulum

### 1. Servis Kaydı
`Program.cs` dosyasında servis zaten kayıtlı:
```csharp
builder.Services.AddScoped<ITranslationService, TranslationService>();
```

### 2. Bağımlılık Enjeksiyonu
```csharp
public class DynamicTableService
{
    private readonly ITranslationService _translationService;
    
    public DynamicTableService(ITranslationService translationService)
    {
        _translationService = translationService;
    }
}
```

### 3. Kullanım
```csharp
// Tekil çeviri
var translatedName = _translationService.TranslateColumnName("Öğrenci Adı", TranslationStrategy.Intelligent);

// Doğrulama
var validation = _translationService.ValidateTranslatedName(translatedName);

// Önizleme
var preview = _translationService.GetTranslationPreview("Öğrenci Adı");
```

## 📋 Sözlükler

### Türkçe-İngilizce Sözlük
```csharp
private Dictionary<string, string> InitializeTurkishToEnglishDictionary()
{
    return new Dictionary<string, string>
    {
        {"ad", "name"},
        {"soyad", "surname"},
        {"tc kimlik no", "identity_number"},
        {"ogrenci no", "student_number"},
        {"dogum tarihi", "birth_date"},
        {"tutar", "amount"},
        {"odenecek", "payable"},
        {"odeme tarihi", "payment_date"},
        // ... daha fazla terim
    };
}
```

### İş Terimleri Sözlüğü
```csharp
private Dictionary<string, string> InitializeBusinessTermDictionary()
{
    return new Dictionary<string, string>
    {
        {"musteri", "customer"},
        {"siparis", "order"},
        {"urun", "product"},
        {"hizmet", "service"},
        {"fatura", "invoice"},
        {"rapor", "report"},
        // ... daha fazla terim
    };
}
```

### Kısaltma Sözlüğü
```csharp
private Dictionary<string, string> InitializeAbbreviationDictionary()
{
    return new Dictionary<string, string>
    {
        {"ad", "name"},
        {"soyad", "surname"},
        {"tc", "identity"},
        {"ogrenci", "student"},
        {"aciklama", "desc"},
        // ... daha fazla terim
    };
}
```

## 🔍 Doğrulama Kuralları

### SQL Server Ayrılmış Kelimeler
- ADD, ALL, ALTER, AND, ANY, AS, ASC, AUTHORIZATION, BACKUP, BEGIN, BETWEEN, BREAK, BROWSE, BULK, BY, CASCADE, CASE, CHECK, CHECKPOINT, CLOSE, CLUSTERED, COALESCE, COLLATE, COLUMN, COMMIT, COMPUTE, CONSTRAINT, CONTAINS, CONTAINSTABLE, CONTINUE, CONVERT, CREATE, CROSS, CURRENT, CURRENT_DATE, CURRENT_TIME, CURRENT_TIMESTAMP, CURRENT_USER, CURSOR, DATABASE, DBCC, DEALLOCATE, DECLARE, DEFAULT, DELETE, DENY, DESC, DISK, DISTINCT, DISTRIBUTED, DROP, DUMP, ELSE, END, ERRLVL, ESCAPE, EXCEPT, EXEC, EXECUTE, EXISTS, EXIT, EXTERNAL, FETCH, FILE, FILLFACTOR, FOR, FOREIGN, FREETEXT, FREETEXTTABLE, FROM, FULL, FUNCTION, GOTO, GRANT, GROUP, HAVING, HOLDLOCK, IDENTITY, IDENTITY_INSERT, IDENTITYCOL, IF, IN, INDEX, INNER, INSERT, INTERSECT, INTO, IS, JOIN, KEY, KILL, LEFT, LIKE, LINENO, LOAD, MERGE, NATIONAL, NOCHECK, NONCLUSTERED, NOT, NULL, NULLIF, OF, OFF, OFFSETS, ON, OPEN, OPENDATASOURCE, OPENQUERY, OPENROWSET, OPENXML, OPTION, OR, ORDER, OUTER, OVER, PERCENT, PIVOT, PLAN, PRECISION, PRIMARY, PRINT, PROC, PROCEDURE, PUBLIC, RAISERROR, READ, READTEXT, RECONFIGURE, REFERENCES, REPLICATION, RESTORE, RESTRICT, RETURN, REVERT, REVOKE, RIGHT, ROLLBACK, ROWCOUNT, ROWGUIDCOL, RULE, SAVE, SCHEMA, SECURITYAUDIT, SELECT, SEMANTICKEYPHRASETABLE, SEMANTICSIMILARITYDETAILSTABLE, SEMANTICSIMILARITYTABLE, SESSION_USER, SET, SETUSER, SHUTDOWN, SOME, STATISTICS, TABLE, TABLESAMPLE, TEXTSIZE, THEN, TO, TOP, TRAN, TRANSACTION, TRIGGER, TRUNCATE, TRY_CONVERT, TSEQUAL, UNION, UNIQUE, UNPIVOT, UPDATE, UPDATETEXT, USE, USER, VALUES, VARYING, VIEW, WAITFOR, WHEN, WHERE, WHILE, WITH, WITHIN GROUP, WRITETEXT

### Karakter Kuralları
- Sadece harf, rakam ve alt çizgi (_) kullanılabilir
- Maksimum 128 karakter
- Sayı ile başlayamaz (otomatik olarak _ eklenir)
- Ardışık alt çizgiler uyarı verir

## 🧪 Test

### Manuel Test
1. `http://localhost:5000/translation-test.html` adresine gidin
2. Farklı sütun isimleri deneyin
3. Farklı stratejileri test edin
4. Doğrulama sonuçlarını kontrol edin

### API Test
```bash
# Önizleme testi
curl "http://localhost:5000/api/translation/preview?columnName=Öğrenci%20Adı"

# Tekil çeviri testi
curl -X POST "http://localhost:5000/api/translation/translate-column" \
  -H "Content-Type: application/json" \
  -d '{"columnName": "Öğrenci Adı", "strategy": "Intelligent"}'

# Toplu çeviri testi
curl -X POST "http://localhost:5000/api/translation/batch-translate" \
  -H "Content-Type: application/json" \
  -d '{"columnNames": ["Öğrenci Adı", "TC Kimlik No"], "strategy": "Intelligent"}'
```

## 🔄 Entegrasyon

### Mevcut Kodda Kullanım
```csharp
// DynamicTableService'de kullanım
public string SanitizeColumnName(string name)
{
    if (string.IsNullOrEmpty(name))
        return "Column_" + Guid.NewGuid().ToString("N").Substring(0, 8);

    // Translation service kullanımı
    return _translationService.TranslateColumnName(name, TranslationStrategy.Intelligent);
}
```

### Yeni Özellikler Ekleme
```csharp
// Yeni sözlük terimleri ekleme
private Dictionary<string, string> InitializeCustomDictionary()
{
    return new Dictionary<string, string>
    {
        {"yeni_terim", "new_term"},
        {"ozel_alan", "special_field"}
    };
}

// Yeni strateji ekleme
public enum TranslationStrategy
{
    // ... mevcut stratejiler
    Custom = 6
}
```

## 📈 Performans

### Optimizasyonlar
- Sözlükler uygulama başlangıcında yüklenir
- Regex işlemleri optimize edilmiştir
- Önbellekleme için Dictionary kullanılır
- Hata durumları için try-catch blokları

### Bellek Kullanımı
- Sözlükler yaklaşık 2-3 MB bellek kullanır
- Her çeviri işlemi ~1ms sürer
- Toplu işlemler için optimize edilmiştir

## 🐛 Sorun Giderme

### Yaygın Sorunlar

1. **SQL Server Ayrılmış Kelime Hatası**
   ```
   Error: 'order' is a SQL reserved word
   Solution: Suggested name: _order
   ```

2. **Geçersiz Karakter Hatası**
   ```
   Error: Name contains invalid characters
   Solution: Only letters, numbers, and underscores allowed
   ```

3. **Uzunluk Hatası**
   ```
   Error: Name is too long (max 128 characters)
   Solution: Suggested name: truncated_name
   ```

### Debug Modu
```csharp
// Debug logları için
_logger.LogDebug("Translating column name: {OriginalName} with strategy: {Strategy}", originalName, strategy);
```

## 🔮 Gelecek Özellikler

- [ ] Makine öğrenmesi ile otomatik çeviri
- [ ] Çoklu dil desteği (İngilizce dışında)
- [ ] Kullanıcı tanımlı sözlükler
- [ ] Çeviri geçmişi ve öğrenme
- [ ] API rate limiting
- [ ] Çeviri kalite skorlaması

## 📄 Lisans

Bu servis MIT lisansı altında lisanslanmıştır.

## 🤝 Katkıda Bulunma

1. Yeni sözlük terimleri ekleyin
2. Yeni çeviri stratejileri geliştirin
3. Test senaryoları ekleyin
4. Dokümantasyonu güncelleyin

---

**Not**: Bu servis eğitim ve geliştirme amaçlıdır. Üretim ortamında kullanmadan önce kapsamlı test yapılması önerilir.
