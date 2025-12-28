using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using TeleDoctor.Application.DTOs;
using TeleDoctor.Core.Enums;
using TeleDoctor.Core.Interfaces;

namespace TeleDoctor.Application.Queries.Appointments;

/// <summary>
/// Handler for getting all appointments with optional filters
/// </summary>
public class GetAllAppointmentsQueryHandler : IRequestHandler<GetAllAppointmentsQuery, IEnumerable<AppointmentDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<GetAllAppointmentsQueryHandler> _logger;

    public GetAllAppointmentsQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<GetAllAppointmentsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IEnumerable<AppointmentDto>> Handle(GetAllAppointmentsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving appointments with filters");

        var appointments = await _unitOfWork.Appointments.GetAllAsync();

        // Apply filters
        if (request.PatientId.HasValue)
        {
            appointments = appointments.Where(a => a.PatientId == request.PatientId.Value);
        }

        if (request.DoctorId.HasValue)
        {
            appointments = appointments.Where(a => a.DoctorId == request.DoctorId.Value);
        }

        if (request.FromDate.HasValue)
        {
            appointments = appointments.Where(a => a.ScheduledDateTime >= request.FromDate.Value);
        }

        if (request.ToDate.HasValue)
        {
            appointments = appointments.Where(a => a.ScheduledDateTime <= request.ToDate.Value);
        }

        return _mapper.Map<IEnumerable<AppointmentDto>>(appointments);
    }
}

/// <summary>
/// Handler for getting a specific appointment by ID
/// </summary>
public class GetAppointmentByIdQueryHandler : IRequestHandler<GetAppointmentByIdQuery, AppointmentDto?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<GetAppointmentByIdQueryHandler> _logger;

    public GetAppointmentByIdQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<GetAppointmentByIdQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<AppointmentDto?> Handle(GetAppointmentByIdQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving appointment {AppointmentId}", request.AppointmentId);

        var appointment = await _unitOfWork.Appointments.GetByIdAsync(request.AppointmentId);
        
        return appointment == null ? null : _mapper.Map<AppointmentDto>(appointment);
    }
}

/// <summary>
/// Handler for getting upcoming appointments for a patient
/// </summary>
public class GetUpcomingAppointmentsQueryHandler : IRequestHandler<GetUpcomingAppointmentsQuery, IEnumerable<AppointmentDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<GetUpcomingAppointmentsQueryHandler> _logger;

    public GetUpcomingAppointmentsQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<GetUpcomingAppointmentsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IEnumerable<AppointmentDto>> Handle(GetUpcomingAppointmentsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving upcoming appointments for patient {PatientId}", request.PatientId);

        var fromDate = DateTime.UtcNow;
        var toDate = fromDate.AddDays(request.DaysAhead);

        var appointments = await _unitOfWork.Appointments.FindAsync(a => 
            a.PatientId == request.PatientId &&
            a.ScheduledDateTime >= fromDate &&
            a.ScheduledDateTime <= toDate &&
            a.Status != AppointmentStatus.Cancelled &&
            a.Status != AppointmentStatus.Completed);

        return _mapper.Map<IEnumerable<AppointmentDto>>(appointments.OrderBy(a => a.ScheduledDateTime));
    }
}

/// <summary>
/// Handler for getting doctor's appointments for a specific date
/// </summary>
public class GetDoctorAppointmentsByDateQueryHandler : IRequestHandler<GetDoctorAppointmentsByDateQuery, IEnumerable<AppointmentDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<GetDoctorAppointmentsByDateQueryHandler> _logger;

    public GetDoctorAppointmentsByDateQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<GetDoctorAppointmentsByDateQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IEnumerable<AppointmentDto>> Handle(GetDoctorAppointmentsByDateQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving appointments for doctor {DoctorId} on {Date}", 
            request.DoctorId, request.Date.Date);

        var startOfDay = request.Date.Date;
        var endOfDay = startOfDay.AddDays(1).AddTicks(-1);

        var appointments = await _unitOfWork.Appointments.FindAsync(a => 
            a.DoctorId == request.DoctorId &&
            a.ScheduledDateTime >= startOfDay &&
            a.ScheduledDateTime <= endOfDay);

        return _mapper.Map<IEnumerable<AppointmentDto>>(appointments.OrderBy(a => a.ScheduledDateTime));
    }
}
