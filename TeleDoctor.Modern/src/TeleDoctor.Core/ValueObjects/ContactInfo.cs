using System.ComponentModel.DataAnnotations;

namespace TeleDoctor.Core.ValueObjects;

public record ContactInfo
{
    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;
    
    [Phone]
    public string? PhoneNumber { get; init; }
    
    public string? Address { get; init; }
    
    public string? City { get; init; }
    
    public string? PostalCode { get; init; }
    
    public string? Country { get; init; } = "Norway";
}

public record MedicalInfo
{
    public string? BloodGroup { get; init; }
    
    public string? Allergies { get; init; }
    
    public string? ChronicConditions { get; init; }
    
    public string? CurrentMedications { get; init; }
    
    public string? EmergencyContact { get; init; }
    
    public string? EmergencyContactPhone { get; init; }
}

public record PersonalInfo
{
    [Required]
    public string FirstName { get; init; } = string.Empty;
    
    [Required]
    public string LastName { get; init; } = string.Empty;
    
    public string FullName => $"{FirstName} {LastName}";
    
    public DateTime? DateOfBirth { get; init; }
    
    public string? Gender { get; init; }
    
    public string? ProfileImagePath { get; init; }
    
    public string? NationalId { get; init; } // Norwegian personnummer
}
