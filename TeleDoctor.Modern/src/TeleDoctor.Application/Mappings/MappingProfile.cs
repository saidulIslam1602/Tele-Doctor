using AutoMapper;
using TeleDoctor.Application.DTOs;
using TeleDoctor.Core.Entities;

namespace TeleDoctor.Application.Mappings;

/// <summary>
/// AutoMapper profile for entity-DTO mappings
/// </summary>
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Appointment mappings
        CreateMap<Appointment, AppointmentDto>()
            .ForMember(dest => dest.PatientName, 
                opt => opt.MapFrom(src => src.Patient != null 
                    ? $"{src.Patient.PersonalInfo.FirstName} {src.Patient.PersonalInfo.LastName}" 
                    : null))
            .ForMember(dest => dest.DoctorName, 
                opt => opt.MapFrom(src => src.Doctor != null 
                    ? $"{src.Doctor.PersonalInfo.FirstName} {src.Doctor.PersonalInfo.LastName}" 
                    : null));

        CreateMap<CreateAppointmentDto, Appointment>();

        // Patient mappings
        CreateMap<Patient, PatientDto>()
            .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.PersonalInfo.FirstName))
            .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.PersonalInfo.LastName))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.ContactInfo.Email))
            .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.ContactInfo.PhoneNumber))
            .ForMember(dest => dest.DateOfBirth, opt => opt.MapFrom(src => src.PersonalInfo.DateOfBirth))
            .ForMember(dest => dest.BloodType, opt => opt.MapFrom(src => src.MedicalInfo.BloodGroup))
            .ForMember(dest => dest.Allergies, opt => opt.MapFrom(src => src.MedicalInfo.Allergies))
            .ForMember(dest => dest.ChronicConditions, opt => opt.MapFrom(src => src.MedicalInfo.ChronicConditions));

        // Doctor mappings
        CreateMap<Doctor, DoctorDto>()
            .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.PersonalInfo.FirstName))
            .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.PersonalInfo.LastName))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.ContactInfo.Email))
            .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.ContactInfo.PhoneNumber))
            .ForMember(dest => dest.DepartmentName, opt => opt.MapFrom(src => src.Department != null ? src.Department.Name : null));

        // Prescription mappings
        CreateMap<Prescription, PrescriptionDto>()
            .ForMember(dest => dest.PatientName, 
                opt => opt.MapFrom(src => src.Patient != null 
                    ? $"{src.Patient.PersonalInfo.FirstName} {src.Patient.PersonalInfo.LastName}" 
                    : null))
            .ForMember(dest => dest.DoctorName, 
                opt => opt.MapFrom(src => src.Doctor != null 
                    ? $"{src.Doctor.PersonalInfo.FirstName} {src.Doctor.PersonalInfo.LastName}" 
                    : null));

        // Medical Record mappings
        CreateMap<MedicalRecord, MedicalRecordDto>()
            .ForMember(dest => dest.PatientName, 
                opt => opt.MapFrom(src => src.Patient != null 
                    ? $"{src.Patient.PersonalInfo.FirstName} {src.Patient.PersonalInfo.LastName}" 
                    : null))
            .ForMember(dest => dest.DoctorName, 
                opt => opt.MapFrom(src => src.Doctor != null 
                    ? $"{src.Doctor.PersonalInfo.FirstName} {src.Doctor.PersonalInfo.LastName}" 
                    : null));
    }
}
