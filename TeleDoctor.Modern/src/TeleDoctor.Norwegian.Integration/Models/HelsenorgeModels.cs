using System.Text.Json.Serialization;

namespace TeleDoctor.Norwegian.Integration.Models;

public class HelsenorgePatientData
{
    [JsonPropertyName("personalNumber")]
    public string PersonalNumber { get; set; } = string.Empty;
    
    [JsonPropertyName("firstName")]
    public string FirstName { get; set; } = string.Empty;
    
    [JsonPropertyName("lastName")]
    public string LastName { get; set; } = string.Empty;
    
    [JsonPropertyName("dateOfBirth")]
    public DateTime DateOfBirth { get; set; }
    
    [JsonPropertyName("gender")]
    public string Gender { get; set; } = string.Empty;
    
    [JsonPropertyName("address")]
    public HelsenorgeAddress Address { get; set; } = new();
    
    [JsonPropertyName("contactInfo")]
    public HelsenorgeContactInfo ContactInfo { get; set; } = new();
    
    [JsonPropertyName("fastlege")]
    public HelsenorgeFastlege? Fastlege { get; set; }
    
    [JsonPropertyName("medicalHistory")]
    public List<HelsenorgeMedicalRecord> MedicalHistory { get; set; } = new();
    
    [JsonPropertyName("allergies")]
    public List<string> Allergies { get; set; } = new();
    
    [JsonPropertyName("chronicConditions")]
    public List<string> ChronicConditions { get; set; } = new();
}

public class HelsenorgeAddress
{
    [JsonPropertyName("street")]
    public string Street { get; set; } = string.Empty;
    
    [JsonPropertyName("postalCode")]
    public string PostalCode { get; set; } = string.Empty;
    
    [JsonPropertyName("city")]
    public string City { get; set; } = string.Empty;
    
    [JsonPropertyName("municipality")]
    public string Municipality { get; set; } = string.Empty;
    
    [JsonPropertyName("county")]
    public string County { get; set; } = string.Empty;
}

public class HelsenorgeContactInfo
{
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;
    
    [JsonPropertyName("phoneNumber")]
    public string PhoneNumber { get; set; } = string.Empty;
    
    [JsonPropertyName("emergencyContact")]
    public HelsenorgeEmergencyContact? EmergencyContact { get; set; }
}

public class HelsenorgeEmergencyContact
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("relationship")]
    public string Relationship { get; set; } = string.Empty;
    
    [JsonPropertyName("phoneNumber")]
    public string PhoneNumber { get; set; } = string.Empty;
}

public class HelsenorgeFastlege
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("hprNumber")]
    public string HPRNumber { get; set; } = string.Empty;
    
    [JsonPropertyName("practice")]
    public string Practice { get; set; } = string.Empty;
    
    [JsonPropertyName("address")]
    public HelsenorgeAddress Address { get; set; } = new();
    
    [JsonPropertyName("phoneNumber")]
    public string PhoneNumber { get; set; } = string.Empty;
    
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;
}

public class HelsenorgeMedicalRecord
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("date")]
    public DateTime Date { get; set; }
    
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
    
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
    
    [JsonPropertyName("diagnosis")]
    public string? Diagnosis { get; set; }
    
    [JsonPropertyName("icd10Code")]
    public string? ICD10Code { get; set; }
    
    [JsonPropertyName("provider")]
    public string Provider { get; set; } = string.Empty;
    
    [JsonPropertyName("isConfidential")]
    public bool IsConfidential { get; set; }
}

public class HelsenorgeMedication
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("genericName")]
    public string GenericName { get; set; } = string.Empty;
    
    [JsonPropertyName("atcCode")]
    public string ATCCode { get; set; } = string.Empty;
    
    [JsonPropertyName("vnrNumber")]
    public string VNRNumber { get; set; } = string.Empty;
    
    [JsonPropertyName("dosage")]
    public string Dosage { get; set; } = string.Empty;
    
    [JsonPropertyName("frequency")]
    public string Frequency { get; set; } = string.Empty;
    
    [JsonPropertyName("startDate")]
    public DateTime StartDate { get; set; }
    
    [JsonPropertyName("endDate")]
    public DateTime? EndDate { get; set; }
    
    [JsonPropertyName("prescribedBy")]
    public string PrescribedBy { get; set; } = string.Empty;
    
    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }
    
    [JsonPropertyName("instructions")]
    public string Instructions { get; set; } = string.Empty;
}

public class HelsenorgeAppointment
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("patientPersonalNumber")]
    public string PatientPersonalNumber { get; set; } = string.Empty;
    
    [JsonPropertyName("providerName")]
    public string ProviderName { get; set; } = string.Empty;
    
    [JsonPropertyName("providerHPRNumber")]
    public string ProviderHPRNumber { get; set; } = string.Empty;
    
    [JsonPropertyName("appointmentDateTime")]
    public DateTime AppointmentDateTime { get; set; }
    
    [JsonPropertyName("duration")]
    public int DurationMinutes { get; set; }
    
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty; // Consultation, Follow-up, etc.
    
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty; // Scheduled, Completed, Cancelled
    
    [JsonPropertyName("location")]
    public string Location { get; set; } = string.Empty;
    
    [JsonPropertyName("isDigital")]
    public bool IsDigital { get; set; }
    
    [JsonPropertyName("digitalMeetingUrl")]
    public string? DigitalMeetingUrl { get; set; }
    
    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;
    
    [JsonPropertyName("notes")]
    public string? Notes { get; set; }
}

public class HelsenorgeConsultationSummary
{
    [JsonPropertyName("patientPersonalNumber")]
    public string PatientPersonalNumber { get; set; } = string.Empty;
    
    [JsonPropertyName("consultationDate")]
    public DateTime ConsultationDate { get; set; }
    
    [JsonPropertyName("providerName")]
    public string ProviderName { get; set; } = string.Empty;
    
    [JsonPropertyName("providerHPRNumber")]
    public string ProviderHPRNumber { get; set; } = string.Empty;
    
    [JsonPropertyName("chiefComplaint")]
    public string ChiefComplaint { get; set; } = string.Empty;
    
    [JsonPropertyName("symptoms")]
    public List<string> Symptoms { get; set; } = new();
    
    [JsonPropertyName("diagnosis")]
    public List<HelsenorgeDiagnosis> Diagnoses { get; set; } = new();
    
    [JsonPropertyName("treatment")]
    public string Treatment { get; set; } = string.Empty;
    
    [JsonPropertyName("medications")]
    public List<HelsenorgePrescribedMedication> Medications { get; set; } = new();
    
    [JsonPropertyName("followUpInstructions")]
    public string FollowUpInstructions { get; set; } = string.Empty;
    
    [JsonPropertyName("nextAppointment")]
    public DateTime? NextAppointment { get; set; }
    
    [JsonPropertyName("referrals")]
    public List<HelsenorgeReferral> Referrals { get; set; } = new();
    
    [JsonPropertyName("sickLeave")]
    public HelsenorgeSickLeave? SickLeave { get; set; }
}

public class HelsenorgeDiagnosis
{
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
    
    [JsonPropertyName("icd10Code")]
    public string ICD10Code { get; set; } = string.Empty;
    
    [JsonPropertyName("isPrimary")]
    public bool IsPrimary { get; set; }
}

public class HelsenorgePrescribedMedication
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("dosage")]
    public string Dosage { get; set; } = string.Empty;
    
    [JsonPropertyName("frequency")]
    public string Frequency { get; set; } = string.Empty;
    
    [JsonPropertyName("duration")]
    public string Duration { get; set; } = string.Empty;
    
    [JsonPropertyName("instructions")]
    public string Instructions { get; set; } = string.Empty;
    
    [JsonPropertyName("eReseptId")]
    public string? EReseptId { get; set; }
}

public class HelsenorgeReferral
{
    [JsonPropertyName("specialty")]
    public string Specialty { get; set; } = string.Empty;
    
    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;
    
    [JsonPropertyName("urgency")]
    public string Urgency { get; set; } = string.Empty;
    
    [JsonPropertyName("preferredProvider")]
    public string? PreferredProvider { get; set; }
}

public class HelsenorgeSickLeave
{
    [JsonPropertyName("startDate")]
    public DateTime StartDate { get; set; }
    
    [JsonPropertyName("endDate")]
    public DateTime EndDate { get; set; }
    
    [JsonPropertyName("percentage")]
    public int Percentage { get; set; }
    
    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;
    
    [JsonPropertyName("diagnosis")]
    public string Diagnosis { get; set; } = string.Empty;
}
