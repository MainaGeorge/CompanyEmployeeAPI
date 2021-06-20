using AutoMapper;
using Entities.DTOs;
using Entities.Models;

namespace CompanyEmployee.Mappings
{
    public class MappingProfiles : Profile
    {
        public MappingProfiles()
        {
            CreateMap<Company, CompanyDto>()
                .ForMember(m => m.FullAddress, opt =>
                {
                    opt.MapFrom(c => $"{c.Address} {c.Country}");
                });

            CreateMap<Employee, EmployeeDto>();
            CreateMap<CompanyForCreationDto, Company>();
            CreateMap<EmployeeForCreationDto, Employee>().ReverseMap();
            CreateMap<EmployeeForUpdatingDto, Employee>().ReverseMap();
            CreateMap<CompanyForUpdateDto, Company>().ReverseMap();
            CreateMap<UserForRegistrationDto, User>();

        }
    }
}
