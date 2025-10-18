using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeleDoctor.Core.Entities;
using TeleDoctor.Core.Interfaces;

namespace TeleDoctor.WebAPI.Controllers;

/// <summary>
/// API controller for appointment management
/// Handles CRUD operations for medical appointments
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AppointmentsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AppointmentsController> _logger;

    public AppointmentsController(
        IUnitOfWork unitOfWork,
        ILogger<AppointmentsController> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Gets all appointments for the current user
    /// </summary>
    /// <returns>List of appointments</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<Appointment>>> GetAppointments()
    {
        try
        {
            // In production, get user ID from claims
            var appointments = await _unitOfWork.Appointments.GetAllAsync();
            return Ok(appointments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving appointments");
            return StatusCode(500, "An error occurred while retrieving appointments");
        }
    }

    /// <summary>
    /// Gets a specific appointment by ID
    /// </summary>
    /// <param name="id">Appointment ID</param>
    /// <returns>Appointment details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Appointment>> GetAppointment(int id)
    {
        try
        {
            var appointment = await _unitOfWork.Appointments.GetByIdAsync(id);
            
            if (appointment == null)
            {
                return NotFound($"Appointment with ID {id} not found");
            }

            return Ok(appointment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving appointment {AppointmentId}", id);
            return StatusCode(500, "An error occurred while retrieving the appointment");
        }
    }

    /// <summary>
    /// Creates a new appointment
    /// </summary>
    /// <param name="appointment">Appointment details</param>
    /// <returns>Created appointment</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Appointment>> CreateAppointment([FromBody] Appointment appointment)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var created = await _unitOfWork.Appointments.AddAsync(appointment);
            
            _logger.LogInformation("Created appointment {AppointmentId}", created.Id);
            
            return CreatedAtAction(
                nameof(GetAppointment), 
                new { id = created.Id }, 
                created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating appointment");
            return StatusCode(500, "An error occurred while creating the appointment");
        }
    }

    /// <summary>
    /// Updates an existing appointment
    /// </summary>
    /// <param name="id">Appointment ID</param>
    /// <param name="appointment">Updated appointment details</param>
    /// <returns>No content on success</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateAppointment(int id, [FromBody] Appointment appointment)
    {
        try
        {
            if (id != appointment.Id)
            {
                return BadRequest("ID mismatch");
            }

            var existing = await _unitOfWork.Appointments.GetByIdAsync(id);
            if (existing == null)
            {
                return NotFound($"Appointment with ID {id} not found");
            }

            await _unitOfWork.Appointments.UpdateAsync(appointment);
            
            _logger.LogInformation("Updated appointment {AppointmentId}", id);
            
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating appointment {AppointmentId}", id);
            return StatusCode(500, "An error occurred while updating the appointment");
        }
    }

    /// <summary>
    /// Deletes an appointment
    /// </summary>
    /// <param name="id">Appointment ID</param>
    /// <returns>No content on success</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAppointment(int id)
    {
        try
        {
            var appointment = await _unitOfWork.Appointments.GetByIdAsync(id);
            
            if (appointment == null)
            {
                return NotFound($"Appointment with ID {id} not found");
            }

            await _unitOfWork.Appointments.DeleteAsync(appointment);
            
            _logger.LogInformation("Deleted appointment {AppointmentId}", id);
            
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting appointment {AppointmentId}", id);
            return StatusCode(500, "An error occurred while deleting the appointment");
        }
    }

    /// <summary>
    /// Gets upcoming appointments for a specific patient
    /// </summary>
    /// <param name="patientId">Patient ID</param>
    /// <returns>List of upcoming appointments</returns>
    [HttpGet("patient/{patientId}")]
    [Authorize(Roles = "Doctor,Admin,Coordinator")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Appointment>>> GetPatientAppointments(int patientId)
    {
        try
        {
            var appointments = await _unitOfWork.Appointments.GetAppointmentsByPatientIdAsync(patientId);
            return Ok(appointments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving appointments for patient {PatientId}", patientId);
            return StatusCode(500, "An error occurred while retrieving appointments");
        }
    }

    /// <summary>
    /// Gets appointments for a specific doctor
    /// </summary>
    /// <param name="doctorId">Doctor ID</param>
    /// <returns>List of doctor's appointments</returns>
    [HttpGet("doctor/{doctorId}")]
    [Authorize(Roles = "Doctor,Admin,Coordinator")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Appointment>>> GetDoctorAppointments(int doctorId)
    {
        try
        {
            var appointments = await _unitOfWork.Appointments.GetAppointmentsByDoctorIdAsync(doctorId);
            return Ok(appointments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving appointments for doctor {DoctorId}", doctorId);
            return StatusCode(500, "An error occurred while retrieving appointments");
        }
    }
}
