using MediatR;
using TeleDoctor.Application.DTOs;

namespace TeleDoctor.Application.Queries.Appointments;

/// <summary>
/// Query to get all appointments
/// </summary>
public class GetAllAppointmentsQuery : IRequest<IEnumerable<AppointmentDto>>
{
    public int? PatientId { get; set; }
    public int? DoctorId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

/// <summary>
/// Query to get a specific appointment by ID
/// </summary>
public class GetAppointmentByIdQuery : IRequest<AppointmentDto?>
{
    public int AppointmentId { get; set; }
}

/// <summary>
/// Query to get upcoming appointments for a patient
/// </summary>
public class GetUpcomingAppointmentsQuery : IRequest<IEnumerable<AppointmentDto>>
{
    public int PatientId { get; set; }
    public int DaysAhead { get; set; } = 30;
}

/// <summary>
/// Query to get doctor's appointments for a specific date
/// </summary>
public class GetDoctorAppointmentsByDateQuery : IRequest<IEnumerable<AppointmentDto>>
{
    public int DoctorId { get; set; }
    public DateTime Date { get; set; }
}
