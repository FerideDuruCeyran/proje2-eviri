using AutoMapper;
using ExcelUploader.Models;
using ExcelUploader.Data;

namespace ExcelUploader.Mapping
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            // ExcelData mappings
            CreateMap<ExcelData, EditDataViewModel>()
                .ReverseMap();

            // DynamicTable mappings
            CreateMap<DynamicTable, DynamicTableDetailsViewModel>()
                .ForMember(dest => dest.Data, opt => opt.Ignore())
                .ForMember(dest => dest.TotalRows, opt => opt.Ignore())
                .ForMember(dest => dest.CurrentPage, opt => opt.Ignore())
                .ForMember(dest => dest.PageSize, opt => opt.Ignore());

            CreateMap<DynamicTable, DynamicTableDataViewModel>()
                .ForMember(dest => dest.Data, opt => opt.Ignore())
                .ForMember(dest => dest.TotalRows, opt => opt.Ignore())
                .ForMember(dest => dest.CurrentPage, opt => opt.Ignore())
                .ForMember(dest => dest.PageSize, opt => opt.Ignore())
                .ForMember(dest => dest.TotalPages, opt => opt.Ignore());

            // User mappings
            CreateMap<RegisterViewModel, ApplicationUser>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.NormalizedUserName, opt => opt.MapFrom(src => src.Email.ToUpper()))
                .ForMember(dest => dest.NormalizedEmail, opt => opt.MapFrom(src => src.Email.ToUpper()))
                .ForMember(dest => dest.EmailConfirmed, opt => opt.MapFrom(src => false))
                .ForMember(dest => dest.PhoneNumberConfirmed, opt => opt.MapFrom(src => false))
                .ForMember(dest => dest.TwoFactorEnabled, opt => opt.MapFrom(src => false))
                .ForMember(dest => dest.LockoutEnabled, opt => opt.MapFrom(src => true))
                .ForMember(dest => dest.AccessFailedCount, opt => opt.MapFrom(src => 0));
        }
    }
}
