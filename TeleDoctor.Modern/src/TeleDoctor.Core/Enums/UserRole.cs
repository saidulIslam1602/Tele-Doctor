namespace TeleDoctor.Core.Enums;

public enum UserRole
{
    Patient = 1,
    Doctor = 2,
    Coordinator = 3,
    Admin = 4
}

public enum AppointmentStatus
{
    Pending = 1,
    Confirmed = 2,
    InProgress = 3,
    Completed = 4,
    Cancelled = 5,
    NoShow = 6
}

public enum ConsultationType
{
    VideoCall = 1,
    AudioCall = 2,
    Chat = 3,
    InPerson = 4
}

public enum PrescriptionStatus
{
    Draft = 1,
    Issued = 2,
    Dispensed = 3,
    Completed = 4
}

public enum BloodGroup
{
    APositive,
    ANegative,
    BPositive,
    BNegative,
    ABPositive,
    ABNegative,
    OPositive,
    ONegative
}

public enum Specialization
{
    GeneralPractice = 1,
    Cardiology = 2,
    Dermatology = 3,
    Neurology = 4,
    Orthopedics = 5,
    Pediatrics = 6,
    Psychiatry = 7,
    Radiology = 8,
    Surgery = 9,
    Gynecology = 10,
    Ophthalmology = 11,
    ENT = 12,
    Urology = 13,
    Oncology = 14,
    Endocrinology = 15
}
