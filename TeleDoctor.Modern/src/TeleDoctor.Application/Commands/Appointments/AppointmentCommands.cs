using MediatR;
using TeleDoctor.Application.DTOs;
using TeleDoctor.Core.Enums;

namespace TeleDoctor.Application.Commands.Appointments;

/// <summary>
/// Command to create a new appointment
/// </summary>
public class CreateAppointmentCommand : IRequest<AppointmentDto>
{
    public int PatientId { get; set; }
    public int DoctorId { get; set; }
    public DateTime ScheduledDateTime { get; set; }
    public int DurationMinutes { get; set; } = 30;
    public ConsultationType ConsultationType { get; set; }
    public string? ReasonForVisit { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Command to update an existing appointment
/// </summary>
public class UpdateAppointmentCommand : IRequest<AppointmentDto>
{
    public int Id { get; set; }
    public DateTime? ScheduledDateTime { get; set; }
    public int? DurationMinutes { get; set; }
    public AppointmentStatus? Status { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Command to cancel an appointment
/// </summary>
public class CancelAppointmentCommand : IRequest<bool>
{
    public int AppointmentId { get; set; }
    public string? CancellationReason { get; set; }
}

/// <summary>
/// Command to delete an appointment
/// </summary>
public class DeleteAppointmentCommand : IRequest<bool>
{
    public int AppointmentId { get; set; }
}
