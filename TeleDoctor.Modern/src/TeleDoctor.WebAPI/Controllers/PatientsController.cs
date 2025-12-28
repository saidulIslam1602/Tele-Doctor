using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeleDoctor.Core.Entities;
using TeleDoctor.Core.Interfaces;
using TeleDoctor.Application.Common;

namespace TeleDoctor.WebAPI.Controllers;

/// <summary>
/// API controller for patient management
/// Handles CRUD operations for patients
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PatientsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PatientsController> _logger;

    public PatientsController(
        IUnitOfWork unitOfWork,
        ILogger<PatientsController> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Gets all patients with pagination
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Doctor,Admin,Coordinator")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<Patient>>> GetPatients([FromQuery] PaginationParams pagination)
    {
        try
        {
            var patients = await _unitOfWork.Patients.GetAllAsync();
            var totalCount = patients.Count();
            var pagedResult = patients.AsQueryable()
                .ToPagedResult(pagination.PageNumber, pagination.PageSize, totalCount);
            return Ok(pagedResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving patients");
            return StatusCode(500, "An error occurred while retrieving patients");
        }
    }

    /// <summary>
    /// Gets a specific patient by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Patient>> GetPatient(int id)
    {
        try
        {
            var patient = await _unitOfWork.Patients.GetByIdAsync(id);
            
            if (patient == null)
            {
                return NotFound($"Patient with ID {id} not found");
            }

            return Ok(patient);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving patient {PatientId}", id);
            return StatusCode(500, "An error occurred while retrieving the patient");
        }
    }

    /// <summary>
    /// Gets a patient by email
    /// </summary>
    [HttpGet("by-email/{email}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Patient>> GetPatientByEmail(string email)
    {
        try
        {
            var patient = await _unitOfWork.Patients.GetByEmailAsync(email);
            
            if (patient == null)
            {
                return NotFound($"Patient with email {email} not found");
            }

            return Ok(patient);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving patient for email {Email}", email);
            return StatusCode(500, "An error occurred while retrieving the patient");
        }
    }

    /// <summary>
    /// Creates a new patient
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Coordinator")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Patient>> CreatePatient([FromBody] Patient patient)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var created = await _unitOfWork.Patients.AddAsync(patient);
            await _unitOfWork.SaveChangesAsync();
            
            _logger.LogInformation("Created patient {PatientId}", created.Id);
            
            return CreatedAtAction(nameof(GetPatient), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating patient");
            return StatusCode(500, "An error occurred while creating the patient");
        }
    }

    /// <summary>
    /// Updates an existing patient
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdatePatient(int id, [FromBody] Patient patient)
    {
        try
        {
            if (id != patient.Id)
            {
                return BadRequest("ID mismatch");
            }

            var existing = await _unitOfWork.Patients.GetByIdAsync(id);
            if (existing == null)
            {
                return NotFound($"Patient with ID {id} not found");
            }

            await _unitOfWork.Patients.UpdateAsync(patient);
            await _unitOfWork.SaveChangesAsync();
            
            _logger.LogInformation("Updated patient {PatientId}", id);
            
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating patient {PatientId}", id);
            return StatusCode(500, "An error occurred while updating the patient");
        }
    }

    /// <summary>
    /// Deletes a patient
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePatient(int id)
    {
        try
        {
            var patient = await _unitOfWork.Patients.GetByIdAsync(id);
            
            if (patient == null)
            {
                return NotFound($"Patient with ID {id} not found");
            }

            await _unitOfWork.Patients.DeleteAsync(patient);
            await _unitOfWork.SaveChangesAsync();
            
            _logger.LogInformation("Deleted patient {PatientId}", id);
            
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting patient {PatientId}", id);
            return StatusCode(500, "An error occurred while deleting the patient");
        }
    }

    /// <summary>
    /// Gets medical history for a patient
    /// </summary>
    [HttpGet("{id}/medical-records")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<MedicalRecord>>> GetPatientMedicalRecords(int id)
    {
        try
        {
            var patient = await _unitOfWork.Patients.GetByIdAsync(id);
            if (patient == null)
            {
                return NotFound($"Patient with ID {id} not found");
            }

            var records = await _unitOfWork.MedicalRecords.GetMedicalRecordsByPatientIdAsync(id);
            return Ok(records);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving medical records for patient {PatientId}", id);
            return StatusCode(500, "An error occurred while retrieving medical records");
        }
    }

    /// <summary>
    /// Gets prescriptions for a patient
    /// </summary>
    [HttpGet("{id}/prescriptions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<Prescription>>> GetPatientPrescriptions(int id)
    {
        try
        {
            var patient = await _unitOfWork.Patients.GetByIdAsync(id);
            if (patient == null)
            {
                return NotFound($"Patient with ID {id} not found");
            }

            var prescriptions = await _unitOfWork.Prescriptions.GetPrescriptionsByPatientIdAsync(id);
            return Ok(prescriptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving prescriptions for patient {PatientId}", id);
            return StatusCode(500, "An error occurred while retrieving prescriptions");
        }
    }
}
