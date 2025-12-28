using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeleDoctor.Core.Entities;
using TeleDoctor.Core.Interfaces;

namespace TeleDoctor.WebAPI.Controllers;

/// <summary>
/// API controller for medical record management
/// Handles CRUD operations for medical records
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MedicalRecordsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MedicalRecordsController> _logger;

    public MedicalRecordsController(
        IUnitOfWork unitOfWork,
        ILogger<MedicalRecordsController> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Gets all medical records
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Doctor,Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<MedicalRecord>>> GetMedicalRecords()
    {
        try
        {
            var records = await _unitOfWork.MedicalRecords.GetAllAsync();
            return Ok(records);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving medical records");
            return StatusCode(500, "An error occurred while retrieving medical records");
        }
    }

    /// <summary>
    /// Gets a specific medical record by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MedicalRecord>> GetMedicalRecord(int id)
    {
        try
        {
            var record = await _unitOfWork.MedicalRecords.GetByIdAsync(id);
            
            if (record == null)
            {
                return NotFound($"Medical record with ID {id} not found");
            }

            return Ok(record);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving medical record {RecordId}", id);
            return StatusCode(500, "An error occurred while retrieving the medical record");
        }
    }

    /// <summary>
    /// Gets medical records for a patient
    /// </summary>
    [HttpGet("patient/{patientId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<MedicalRecord>>> GetPatientMedicalRecords(int patientId)
    {
        try
        {
            var records = await _unitOfWork.MedicalRecords.GetMedicalRecordsByPatientIdAsync(patientId);
            return Ok(records);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving medical records for patient {PatientId}", patientId);
            return StatusCode(500, "An error occurred while retrieving medical records");
        }
    }

    /// <summary>
    /// Gets medical records by doctor
    /// </summary>
    [HttpGet("doctor/{doctorId}")]
    [Authorize(Roles = "Doctor,Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<MedicalRecord>>> GetDoctorMedicalRecords(int doctorId)
    {
        try
        {
            var records = await _unitOfWork.MedicalRecords.GetMedicalRecordsByDoctorIdAsync(doctorId);
            return Ok(records);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving medical records for doctor {DoctorId}", doctorId);
            return StatusCode(500, "An error occurred while retrieving medical records");
        }
    }

    /// <summary>
    /// Creates a new medical record
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Doctor")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MedicalRecord>> CreateMedicalRecord([FromBody] MedicalRecord record)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Verify patient exists
            var patient = await _unitOfWork.Patients.GetByIdAsync(record.PatientId);
            if (patient == null)
            {
                return NotFound($"Patient with ID {record.PatientId} not found");
            }

            // Verify doctor exists if provided
            if (record.DoctorId > 0)
            {
                var doctor = await _unitOfWork.Doctors.GetByIdAsync(record.DoctorId);
                if (doctor == null)
                {
                    return NotFound($"Doctor with ID {record.DoctorId} not found");
                }
            }

            var created = await _unitOfWork.MedicalRecords.AddAsync(record);
            await _unitOfWork.SaveChangesAsync();
            
            _logger.LogInformation("Created medical record {RecordId} for patient {PatientId}", 
                created.Id, created.PatientId);
            
            return CreatedAtAction(nameof(GetMedicalRecord), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating medical record");
            return StatusCode(500, "An error occurred while creating the medical record");
        }
    }

    /// <summary>
    /// Updates an existing medical record
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Doctor")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateMedicalRecord(int id, [FromBody] MedicalRecord record)
    {
        try
        {
            if (id != record.Id)
            {
                return BadRequest("ID mismatch");
            }

            var existing = await _unitOfWork.MedicalRecords.GetByIdAsync(id);
            if (existing == null)
            {
                return NotFound($"Medical record with ID {id} not found");
            }

            await _unitOfWork.MedicalRecords.UpdateAsync(record);
            await _unitOfWork.SaveChangesAsync();
            
            _logger.LogInformation("Updated medical record {RecordId}", id);
            
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating medical record {RecordId}", id);
            return StatusCode(500, "An error occurred while updating the medical record");
        }
    }

    /// <summary>
    /// Deletes a medical record
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Doctor,Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteMedicalRecord(int id)
    {
        try
        {
            var record = await _unitOfWork.MedicalRecords.GetByIdAsync(id);
            
            if (record == null)
            {
                return NotFound($"Medical record with ID {id} not found");
            }

            await _unitOfWork.MedicalRecords.DeleteAsync(record);
            await _unitOfWork.SaveChangesAsync();
            
            _logger.LogInformation("Deleted medical record {RecordId}", id);
            
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting medical record {RecordId}", id);
            return StatusCode(500, "An error occurred while deleting the medical record");
        }
    }
}
