using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeleDoctor.Core.Entities;
using TeleDoctor.Core.Interfaces;

namespace TeleDoctor.WebAPI.Controllers;

/// <summary>
/// API controller for prescription management
/// Handles CRUD operations for prescriptions
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PrescriptionsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PrescriptionsController> _logger;

    public PrescriptionsController(
        IUnitOfWork unitOfWork,
        ILogger<PrescriptionsController> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Gets all prescriptions
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Doctor,Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Prescription>>> GetPrescriptions()
    {
        try
        {
            var prescriptions = await _unitOfWork.Prescriptions.GetAllAsync();
            return Ok(prescriptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving prescriptions");
            return StatusCode(500, "An error occurred while retrieving prescriptions");
        }
    }

    /// <summary>
    /// Gets a specific prescription by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Prescription>> GetPrescription(int id)
    {
        try
        {
            var prescription = await _unitOfWork.Prescriptions.GetByIdAsync(id);
            
            if (prescription == null)
            {
                return NotFound($"Prescription with ID {id} not found");
            }

            return Ok(prescription);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving prescription {PrescriptionId}", id);
            return StatusCode(500, "An error occurred while retrieving the prescription");
        }
    }

    /// <summary>
    /// Gets prescriptions for a patient
    /// </summary>
    [HttpGet("patient/{patientId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Prescription>>> GetPatientPrescriptions(int patientId)
    {
        try
        {
            var prescriptions = await _unitOfWork.Prescriptions.GetPrescriptionsByPatientIdAsync(patientId);
            return Ok(prescriptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving prescriptions for patient {PatientId}", patientId);
            return StatusCode(500, "An error occurred while retrieving prescriptions");
        }
    }

    /// <summary>
    /// Gets active prescriptions for a patient
    /// </summary>
    [HttpGet("patient/{patientId}/active")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Prescription>>> GetActivePatientPrescriptions(int patientId)
    {
        try
        {
            var prescriptions = await _unitOfWork.Prescriptions.GetActivePrescriptionsAsync(patientId);
            return Ok(prescriptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active prescriptions for patient {PatientId}", patientId);
            return StatusCode(500, "An error occurred while retrieving active prescriptions");
        }
    }

    /// <summary>
    /// Gets prescriptions by doctor
    /// </summary>
    [HttpGet("doctor/{doctorId}")]
    [Authorize(Roles = "Doctor,Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Prescription>>> GetDoctorPrescriptions(int doctorId)
    {
        try
        {
            var prescriptions = await _unitOfWork.Prescriptions.GetPrescriptionsByDoctorIdAsync(doctorId);
            return Ok(prescriptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving prescriptions for doctor {DoctorId}", doctorId);
            return StatusCode(500, "An error occurred while retrieving prescriptions");
        }
    }

    /// <summary>
    /// Creates a new prescription
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Doctor")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Prescription>> CreatePrescription([FromBody] Prescription prescription)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Verify patient exists
            var patient = await _unitOfWork.Patients.GetByIdAsync(prescription.PatientId);
            if (patient == null)
            {
                return NotFound($"Patient with ID {prescription.PatientId} not found");
            }

            // Verify doctor exists
            var doctor = await _unitOfWork.Doctors.GetByIdAsync(prescription.DoctorId);
            if (doctor == null)
            {
                return NotFound($"Doctor with ID {prescription.DoctorId} not found");
            }

            var created = await _unitOfWork.Prescriptions.AddAsync(prescription);
            await _unitOfWork.SaveChangesAsync();
            
            _logger.LogInformation("Created prescription {PrescriptionId} for patient {PatientId}", 
                created.Id, created.PatientId);
            
            return CreatedAtAction(nameof(GetPrescription), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating prescription");
            return StatusCode(500, "An error occurred while creating the prescription");
        }
    }

    /// <summary>
    /// Updates an existing prescription
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Doctor")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdatePrescription(int id, [FromBody] Prescription prescription)
    {
        try
        {
            if (id != prescription.Id)
            {
                return BadRequest("ID mismatch");
            }

            var existing = await _unitOfWork.Prescriptions.GetByIdAsync(id);
            if (existing == null)
            {
                return NotFound($"Prescription with ID {id} not found");
            }

            await _unitOfWork.Prescriptions.UpdateAsync(prescription);
            await _unitOfWork.SaveChangesAsync();
            
            _logger.LogInformation("Updated prescription {PrescriptionId}", id);
            
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating prescription {PrescriptionId}", id);
            return StatusCode(500, "An error occurred while updating the prescription");
        }
    }

    /// <summary>
    /// Deletes a prescription
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Doctor,Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePrescription(int id)
    {
        try
        {
            var prescription = await _unitOfWork.Prescriptions.GetByIdAsync(id);
            
            if (prescription == null)
            {
                return NotFound($"Prescription with ID {id} not found");
            }

            await _unitOfWork.Prescriptions.DeleteAsync(prescription);
            await _unitOfWork.SaveChangesAsync();
            
            _logger.LogInformation("Deleted prescription {PrescriptionId}", id);
            
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting prescription {PrescriptionId}", id);
            return StatusCode(500, "An error occurred while deleting the prescription");
        }
    }
}
