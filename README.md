# Excel Uploader - ASP.NET Core Projesi

Bu proje, Excel dosyalarını otomatik olarak SQL Server veritabanına aktaran ve kullanıcıların bu verileri görüntüleyip düzenleyebileceği modern bir web uygulamasıdır.

## 🚀 Özellikler

### 📊 Excel İşleme
- **Çoklu Format Desteği**: .xlsx ve .xls dosyaları
- **Dinamik Tablo Oluşturma**: Excel dosyasından otomatik SQL tablosu oluşturma
- **Otomatik Veri Çıkarma**: Excel'den veri okuma ve SQL'e aktarma
- **Büyük Dosya Desteği**: 50MB'a kadar dosya yükleme
- **Akıllı Veri Eşleştirme**: Excel sütunlarını otomatik tanıma ve veri tipi belirleme

### 🔐 Kullanıcı Yönetimi
- **Güvenli Kimlik Doğrulama**: ASP.NET Core Identity
- **Kullanıcı Kayıt ve Giriş**: Tam özellikli hesap yönetimi
- **Rol Tabanlı Erişim**: Farklı kullanıcı seviyeleri
- **Profil Yönetimi**: Kullanıcı bilgilerini güncelleme

### 📈 Dashboard ve Raporlama
- **Gerçek Zamanlı İstatistikler**: Toplam kayıt, işlenen veri sayısı
- **Grafiksel Gösterim**: Chart.js ile interaktif grafikler
- **Aylık Trendler**: Yükleme istatistikleri
- **Ödeme Analizi**: Hibe tutarları ve oranları

### 🔍 Veri Yönetimi
- **Dinamik Tablo Yönetimi**: Her Excel dosyası için ayrı SQL tablosu oluşturma
- **Otomatik Şema Tespiti**: Excel sütunlarından otomatik veri tipi belirleme
- **Gelişmiş Arama**: Ad, soyad, TC kimlik no ile arama
- **Filtreleme**: Başvuru yılı, hareketlilik tipi, ödeme tipi
- **Sıralama**: Çoklu sütun sıralama
- **Sayfalama**: Performanslı veri görüntüleme

### ✏️ Veri Düzenleme
- **Inline Düzenleme**: Doğrudan tabloda düzenleme
- **Detaylı Form**: Kapsamlı veri güncelleme
- **Validasyon**: Form doğrulama ve hata kontrolü
- **Toplu İşlemler**: Çoklu kayıt seçimi ve işleme

### 📤 Dışa Aktarma
- **Excel Dışa Aktarma**: Güncel verileri Excel'e aktarma
- **Filtrelenmiş Veri**: Arama sonuçlarını dışa aktarma
- **Özelleştirilebilir Format**: Sütun seçimi ve düzenleme

### 🌐 Port Yönetimi
- **Otomatik Port Tespiti**: Kullanılabilir port bulma
- **Dinamik URL**: Port bilgisine göre otomatik URL oluşturma
- **Çakışma Önleme**: Port çakışmalarını otomatik çözme

## 🛠️ Teknolojiler

- **Backend**: ASP.NET Core 9.0
- **Veritabanı**: SQL Server (Entity Framework Core)
- **Frontend**: Bootstrap 5, Chart.js
- **Excel İşleme**: EPPlus, ClosedXML, NPOI
- **Kimlik Doğrulama**: ASP.NET Core Identity
- **UI Framework**: Bootstrap 5 + Bootstrap Icons

## 📋 Gereksinimler

- .NET 9.0 SDK
- SQL Server (LocalDB, Express, veya Enterprise)
- Visual Studio 2022 veya VS Code
- Modern web tarayıcısı

## 🚀 Kurulum

### 1. Projeyi İndirin
```bash
git clone [repository-url]
cd ExcelUploader
```

### 2. Bağımlılıkları Yükleyin
```bash
dotnet restore
```

### 3. Veritabanı Bağlantısını Yapılandırın
`appsettings.json` dosyasında veritabanı bağlantı dizesini güncelleyin:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=ExcelUploaderDB;Trusted_Connection=true;MultipleActiveResultSets=true"
  }
}
```

### 4. Veritabanını Oluşturun
```bash
dotnet ef database update
```

### 5. Uygulamayı Çalıştırın
```bash
dotnet run
```

## 🔑 Varsayılan Kullanıcı

Sistem ilk çalıştırıldığında otomatik olarak bir admin kullanıcısı oluşturulur:

- **E-posta**: admin@exceluploader.com
- **Şifre**: Admin123!

## 📖 Kullanım Kılavuzu

### 1. Giriş Yapma
- Tarayıcınızda `http://localhost:5000` adresine gidin
- Varsayılan admin hesabı ile giriş yapın
- Veya yeni hesap oluşturun

### 2. Excel Dosyası Yükleme
- Ana sayfada "Excel Yükle" butonuna tıklayın
- Dosyayı sürükleyip bırakın veya "Dosya Seç" ile seçin
- Sistem otomatik olarak Excel şemasını analiz edecek
- Yeni bir SQL tablosu oluşturulacak ve veriler aktarılacak
- İşlem sonucu dashboard'da görüntülenecek

### 3. Veri Görüntüleme
- "Dinamik Tablolar" menüsünden yüklenen Excel tablolarını görüntüleyin
- Her tablo için ayrı veri görüntüleme ve düzenleme
- Arama ve filtreleme özelliklerini kullanın
- Sayfalama ile büyük veri setlerini yönetin

### 4. Veri Düzenleme
- Herhangi bir kayıt üzerinde "Düzenle" butonuna tıklayın
- Form üzerinden verileri güncelleyin
- Değişiklikleri kaydedin

### 5. Veri Dışa Aktarma
- "Dışa Aktar" butonuna tıklayın
- Filtrelenmiş verileri Excel formatında indirin

## 🏗️ Proje Yapısı

```
ExcelUploader/
├── Controllers/          # MVC Controllers
├── Data/                # Entity Framework Context
├── Models/              # Data Models ve ViewModels
├── Services/            # Business Logic Services
├── Views/               # Razor Views
│   ├── Account/        # Authentication Views
│   ├── Home/           # Main Application Views
│   └── Shared/         # Layout ve Partial Views
├── wwwroot/            # Static Files
├── Program.cs          # Application Entry Point
└── appsettings.json    # Configuration
```

## 🔧 Yapılandırma

### Port Yapılandırması
```json
{
  "PortConfiguration": {
    "DefaultPort": 5000,
    "AutoPortDetection": true
  }
}
```

### Dosya Yükleme Ayarları
```json
{
  "FileUpload": {
    "MaxFileSize": 52428800,
    "AllowedExtensions": [".xlsx", ".xls"],
    "UploadPath": "Uploads"
  }
}
```

## 📊 Desteklenen Excel Formatları

Proje, Excel dosyalarınızdaki aşağıdaki sütunları otomatik olarak tanır:

- **Temel Bilgiler**: Ad, Soyad, TC Kimlik No, Öğrenci No
- **Başvuru Bilgileri**: Başvuru Yılı, Hareketlilik Tipi, Başvuru Tipi
- **Ödeme Bilgileri**: Ödeme Tipi, Taksit, Ödenecek, Ödenen
- **Tarih Bilgileri**: Ödeme Tarihi, Başvuru Tarihi
- **Açıklamalar**: Açıklama, Başvuru Açıklama

## 🚨 Güvenlik Özellikleri

- **Dosya Doğrulama**: Sadece Excel formatları kabul edilir
- **Boyut Sınırlaması**: Maksimum 50MB dosya boyutu
- **Virüs Koruması**: Güvenli dosya işleme
- **Kimlik Doğrulama**: Güvenli kullanıcı girişi
- **CSRF Koruması**: Cross-site request forgery koruması

## 🐛 Sorun Giderme

### Yaygın Sorunlar

1. **Veritabanı Bağlantı Hatası**
   - SQL Server'ın çalıştığından emin olun
   - Bağlantı dizesini kontrol edin

2. **Port Çakışması**
   - `appsettings.json`'da farklı port numarası deneyin
   - AutoPortDetection'ı false yapın

3. **Excel Dosya Hatası**
   - Dosya formatını kontrol edin (.xlsx veya .xls)
   - Dosya boyutunu kontrol edin (50MB altında)

### Log Dosyaları
Uygulama logları `logs/` klasöründe saklanır. Hata ayıklama için bu dosyaları kontrol edin.

## 🤝 Katkıda Bulunma

1. Projeyi fork edin
2. Feature branch oluşturun (`git checkout -b feature/AmazingFeature`)
3. Değişikliklerinizi commit edin (`git commit -m 'Add some AmazingFeature'`)
4. Branch'inizi push edin (`git push origin feature/AmazingFeature`)
5. Pull Request oluşturun

## 📄 Lisans

Bu proje MIT lisansı altında lisanslanmıştır. Detaylar için `LICENSE` dosyasına bakın.

## 📞 Destek

Herhangi bir sorun yaşarsanız:
- GitHub Issues bölümünde sorun bildirin
- Detaylı hata mesajları ve ekran görüntüleri ekleyin
- Kullandığınız .NET ve SQL Server sürümlerini belirtin

## 🔮 Gelecek Özellikler

- [ ] Toplu veri güncelleme
- [ ] Gelişmiş raporlama
- [ ] API desteği
- [ ] Mobil uygulama
- [ ] Çoklu dil desteği
- [ ] E-posta bildirimleri
- [ ] Otomatik yedekleme

---

**Not**: Bu proje eğitim ve geliştirme amaçlıdır. Üretim ortamında kullanmadan önce güvenlik testleri yapılması önerilir.
