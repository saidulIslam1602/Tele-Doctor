using FluentValidation;
using TeleDoctor.Application.Commands.Appointments;
using TeleDoctor.Application.DTOs;

namespace TeleDoctor.Application.Validators;

/// <summary>
/// Validator for CreateAppointmentCommand
/// </summary>
public class CreateAppointmentCommandValidator : AbstractValidator<CreateAppointmentCommand>
{
    public CreateAppointmentCommandValidator()
    {
        RuleFor(x => x.PatientId)
            .GreaterThan(0)
            .WithMessage("Patient ID must be greater than 0");

        RuleFor(x => x.DoctorId)
            .GreaterThan(0)
            .WithMessage("Doctor ID must be greater than 0");

        RuleFor(x => x.ScheduledDateTime)
            .GreaterThan(DateTime.UtcNow)
            .WithMessage("Appointment must be scheduled in the future");

        RuleFor(x => x.DurationMinutes)
            .GreaterThan(0)
            .LessThanOrEqualTo(480) // Max 8 hours
            .WithMessage("Duration must be between 1 and 480 minutes");

        RuleFor(x => x.ReasonForVisit)
            .MaximumLength(500)
            .WithMessage("Reason for visit must not exceed 500 characters");

        RuleFor(x => x.Notes)
            .MaximumLength(2000)
            .WithMessage("Notes must not exceed 2000 characters");
    }
}

/// <summary>
/// Validator for UpdateAppointmentCommand
/// </summary>
public class UpdateAppointmentCommandValidator : AbstractValidator<UpdateAppointmentCommand>
{
    public UpdateAppointmentCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("Appointment ID must be greater than 0");

        RuleFor(x => x.ScheduledDateTime)
            .GreaterThan(DateTime.UtcNow)
            .When(x => x.ScheduledDateTime.HasValue)
            .WithMessage("Appointment must be scheduled in the future");

        RuleFor(x => x.DurationMinutes)
            .GreaterThan(0)
            .LessThanOrEqualTo(480)
            .When(x => x.DurationMinutes.HasValue)
            .WithMessage("Duration must be between 1 and 480 minutes");

        RuleFor(x => x.Notes)
            .MaximumLength(2000)
            .When(x => !string.IsNullOrEmpty(x.Notes))
            .WithMessage("Notes must not exceed 2000 characters");
    }
}

/// <summary>
/// Validator for CreateAppointmentDto
/// </summary>
public class CreateAppointmentDtoValidator : AbstractValidator<CreateAppointmentDto>
{
    public CreateAppointmentDtoValidator()
    {
        RuleFor(x => x.PatientId)
            .GreaterThan(0)
            .WithMessage("Patient ID must be greater than 0");

        RuleFor(x => x.DoctorId)
            .GreaterThan(0)
            .WithMessage("Doctor ID must be greater than 0");

        RuleFor(x => x.ScheduledDateTime)
            .GreaterThan(DateTime.UtcNow)
            .WithMessage("Appointment must be scheduled in the future");

        RuleFor(x => x.DurationMinutes)
            .GreaterThan(0)
            .LessThanOrEqualTo(480)
            .WithMessage("Duration must be between 1 and 480 minutes");
    }
}
