using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using TeleDoctor.Application.DTOs;
using TeleDoctor.Core.Entities;
using TeleDoctor.Core.Enums;
using TeleDoctor.Core.Interfaces;

namespace TeleDoctor.Application.Commands.Appointments;

/// <summary>
/// Handler for creating a new appointment
/// </summary>
public class CreateAppointmentCommandHandler : IRequestHandler<CreateAppointmentCommand, AppointmentDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateAppointmentCommandHandler> _logger;

    public CreateAppointmentCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<CreateAppointmentCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<AppointmentDto> Handle(CreateAppointmentCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating appointment for patient {PatientId} with doctor {DoctorId}", 
            request.PatientId, request.DoctorId);

        // Validate patient and doctor exist
        var patient = await _unitOfWork.Patients.GetByIdAsync(request.PatientId);
        if (patient == null)
        {
            throw new KeyNotFoundException($"Patient with ID {request.PatientId} not found");
        }

        var doctor = await _unitOfWork.Doctors.GetByIdAsync(request.DoctorId);
        if (doctor == null)
        {
            throw new KeyNotFoundException($"Doctor with ID {request.DoctorId} not found");
        }

        // Create appointment entity
        var appointment = new Appointment
        {
            PatientId = request.PatientId,
            DoctorId = request.DoctorId,
            ScheduledDateTime = request.ScheduledDateTime,
            ConsultationType = request.ConsultationType,
            ChiefComplaint = request.ReasonForVisit,
            Notes = request.Notes,
            Status = AppointmentStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _unitOfWork.Appointments.AddAsync(appointment);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Successfully created appointment {AppointmentId}", created.Id);

        return _mapper.Map<AppointmentDto>(created);
    }
}

/// <summary>
/// Handler for updating an existing appointment
/// </summary>
public class UpdateAppointmentCommandHandler : IRequestHandler<UpdateAppointmentCommand, AppointmentDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<UpdateAppointmentCommandHandler> _logger;

    public UpdateAppointmentCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<UpdateAppointmentCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<AppointmentDto> Handle(UpdateAppointmentCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating appointment {AppointmentId}", request.Id);

        var appointment = await _unitOfWork.Appointments.GetByIdAsync(request.Id);
        if (appointment == null)
        {
            throw new KeyNotFoundException($"Appointment with ID {request.Id} not found");
        }

        // Update only provided fields
        if (request.ScheduledDateTime.HasValue)
        {
            appointment.ScheduledDateTime = request.ScheduledDateTime.Value;
        }

        if (request.Status.HasValue)
        {
            appointment.Status = request.Status.Value;
        }

        if (request.Notes != null)
        {
            appointment.Notes = request.Notes;
        }

        appointment.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Appointments.UpdateAsync(appointment);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Successfully updated appointment {AppointmentId}", request.Id);

        return _mapper.Map<AppointmentDto>(appointment);
    }
}

/// <summary>
/// Handler for cancelling an appointment
/// </summary>
public class CancelAppointmentCommandHandler : IRequestHandler<CancelAppointmentCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CancelAppointmentCommandHandler> _logger;

    public CancelAppointmentCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<CancelAppointmentCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> Handle(CancelAppointmentCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Cancelling appointment {AppointmentId}", request.AppointmentId);

        var appointment = await _unitOfWork.Appointments.GetByIdAsync(request.AppointmentId);
        if (appointment == null)
        {
            throw new KeyNotFoundException($"Appointment with ID {request.AppointmentId} not found");
        }

        appointment.Status = AppointmentStatus.Cancelled;
        appointment.Notes = $"{appointment.Notes}\nCancellation reason: {request.CancellationReason}";
        appointment.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Appointments.UpdateAsync(appointment);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Successfully cancelled appointment {AppointmentId}", request.AppointmentId);

        return true;
    }
}

/// <summary>
/// Handler for deleting an appointment
/// </summary>
public class DeleteAppointmentCommandHandler : IRequestHandler<DeleteAppointmentCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteAppointmentCommandHandler> _logger;

    public DeleteAppointmentCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<DeleteAppointmentCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> Handle(DeleteAppointmentCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting appointment {AppointmentId}", request.AppointmentId);

        var appointment = await _unitOfWork.Appointments.GetByIdAsync(request.AppointmentId);
        if (appointment == null)
        {
            throw new KeyNotFoundException($"Appointment with ID {request.AppointmentId} not found");
        }

        await _unitOfWork.Appointments.DeleteAsync(appointment);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Successfully deleted appointment {AppointmentId}", request.AppointmentId);

        return true;
    }
}
