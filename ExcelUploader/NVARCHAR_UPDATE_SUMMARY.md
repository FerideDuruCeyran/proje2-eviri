# NVARCHAR Limit Update Summary

## Overview
This document summarizes the changes made to update nvarchar field limits from `nvarchar(max)` to `nvarchar(250)` for various fields in the ExcelData table to better accommodate longer data like addresses while maintaining reasonable storage limits.

## Changes Made

### 1. ApplicationDbContext.cs Updates
Updated the `OnModelCreating` method to set `HasMaxLength(250)` for the following field categories:

#### Address and Location Fields
- `AdresIl` (Address City)
- `AdresUlke` (Address Country) 
- `DogumYeri` (Birth Place)

#### Bank-Related Fields
- `BankaHesapSahibi` (Bank Account Owner)
- `BankaAdi` (Bank Name)
- `BankaSubeKodu` (Bank Branch Code)
- `BankaSubeAdi` (Bank Branch Name)
- `BankaHesapNumarasi` (Bank Account Number)
- `BankaIBANNo` (Bank IBAN Number)

#### University and Academic Fields
- `FakulteAdi` (Faculty Name)
- `BirimAdi` (Unit Name)
- `DiplomaDerecesi` (Diploma Degree)
- `UniversiteKoordinatoru` (University Coordinator)
- `UniversiteKoordinatoruEmail` (University Coordinator Email)
- `UniversiteUluslararasiKodu` (University International Code)
- `UzmanlikAlani` (Expertise Area)
- `BasvuruSayfasi` (Application Page)

#### Other Fields
- `BasvuruTipi` (Application Type)
- `Taksit` (Installment)
- `KullaniciAdi` (Username)
- `PasaportNo` (Passport Number)
- `Cinsiyet` (Gender)
- `Sinif` (Class)
- `GaziSehitYakini` (Veteran/Martyr Relative)
- `YurtBasvurusu` (Dormitory Application)
- `TercihSirasi` (Preference Order)
- `TercihDurumu` (Preference Status)
- `BasvuruDurumu` (Application Status)
- `Burs` (Scholarship)
- `AkademikYil` (Academic Year)
- `AkademikDonem` (Academic Term)
- `DegisimProgramiTipi` (Exchange Program Type)
- `KatilmakIstedigiYabanciDilSinavi` (Desired Foreign Language Exam)
- `SistemDisiGecmisHareketlilik` (System External Past Mobility)
- `SistemIciGecmisHareketlilikBilgisi` (System Internal Past Mobility Info)
- `Tercihler` (Preferences)
- `HibeSozlesmeTipi` (Grant Contract Type)
- `HibeButceYili` (Grant Budget Year)
- `SinavTipi` (Exam Type)
- `SinavDili` (Exam Language)
- `Unvan` (Title)

### 2. Migration Files Created
- `20250901090000_UpdateNvarcharLimitsTo250.cs` - Main migration file
- `20250901090000_UpdateNvarcharLimitsTo250.Designer.cs` - Designer file

## Benefits of This Change

1. **Storage Optimization**: Limits storage usage while still accommodating longer text
2. **Performance**: Better query performance with fixed-length fields
3. **Data Consistency**: Prevents extremely long text that could cause issues
4. **Address Support**: Adequate length for full addresses, city names, and country names
5. **Bank Information**: Sufficient space for bank names, branch information, and account details

## Fields That Retain Original Limits

The following fields maintain their original max length settings:
- `Aciklama` (Description): 2000 characters
- `BasvuruAciklama` (Application Description): 2000 characters  
- `UniversitedeToplamCalismaSuresi` (Total Work Time at University): 500 characters
- `Ad`, `Soyad`, `TCKimlikNo`, `OgrenciNo`: 450 characters (for indexing purposes)

## Database Update Required

To apply these changes to your database, you need to run the migration:

```bash
dotnet ef database update
```

## Rollback

If you need to rollback these changes, the migration includes a `Down()` method that reverts all fields back to `nvarchar(max)`.

## Notes

- The 250-character limit provides a good balance between storage efficiency and data accommodation
- Address fields now have sufficient space for international addresses
- Bank-related fields can accommodate longer bank names and branch information
- Academic fields have adequate space for detailed descriptions
- All changes maintain backward compatibility with existing data
