using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeleDoctor.Core.Entities;
using TeleDoctor.Core.Interfaces;
using TeleDoctor.WebAPI.Common;
using TeleDoctor.Application.Common;
using TeleDoctor.Infrastructure.Services;

namespace TeleDoctor.WebAPI.Controllers;

/// <summary>
/// API controller for doctor management
/// Handles CRUD operations for doctors with caching support
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DoctorsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DoctorsController> _logger;
    private readonly ICacheService _cacheService;

    public DoctorsController(
        IUnitOfWork unitOfWork,
        ILogger<DoctorsController> logger,
        ICacheService cacheService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _cacheService = cacheService;
    }

    /// <summary>
    /// Gets all doctors with pagination and caching
    /// Cached for 5 minutes to improve performance
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<Doctor>>> GetDoctors([FromQuery] PaginationParams pagination)
    {
        try
        {
            var cacheKey = $"{CacheKeys.AllDoctors}:page:{pagination.PageNumber}:size:{pagination.PageSize}";
            
            var pagedResult = await _cacheService.GetOrSetAsync(
                cacheKey,
                async () =>
                {
                    var doctors = await _unitOfWork.Doctors.GetAllAsync();
                    var totalCount = doctors.Count();
                    return doctors.AsQueryable()
                        .ToPagedResult(pagination.PageNumber, pagination.PageSize, totalCount);
                },
                TimeSpan.FromMinutes(5));
            
            return Ok(pagedResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving doctors");
            return StatusCode(500, "An error occurred while retrieving doctors");
        }
    }

    /// <summary>
    /// Gets a specific doctor by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Doctor>> GetDoctor(int id)
    {
        try
        {
            var doctor = await _unitOfWork.Doctors.GetByIdAsync(id);
            
            if (doctor == null)
            {
                return NotFound($"Doctor with ID {id} not found");
            }

            return Ok(doctor);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving doctor {DoctorId}", id);
            return StatusCode(500, "An error occurred while retrieving the doctor");
        }
    }

    /// <summary>
    /// Gets available doctors on a specific date
    /// </summary>
    [HttpGet("available")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Doctor>>> GetAvailableDoctors([FromQuery] DateTime? date, [FromQuery] string? time)
    {
        try
        {
            var searchDate = date ?? DateTime.Today;
            var searchTime = string.IsNullOrEmpty(time) ? TimeSpan.FromHours(9) : TimeSpan.Parse(time);
            
            var doctors = await _unitOfWork.Doctors.GetAvailableDoctorsAsync(searchDate, searchTime);
            return Ok(doctors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving available doctors");
            return StatusCode(500, "An error occurred while retrieving available doctors");
        }
    }

    /// <summary>
    /// Gets doctors by specialization ID
    /// </summary>
    [HttpGet("specialization/{specializationId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Doctor>>> GetDoctorsBySpecialization(int specializationId)
    {
        try
        {
            var doctors = await _unitOfWork.Doctors.GetDoctorsBySpecializationAsync(specializationId);
            return Ok(doctors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving doctors by specialization {SpecializationId}", specializationId);
            return StatusCode(500, "An error occurred while retrieving doctors");
        }
    }

    /// <summary>
    /// Gets doctors by department
    /// </summary>
    [HttpGet("department/{departmentId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Doctor>>> GetDoctorsByDepartment(int departmentId)
    {
        try
        {
            var doctors = await _unitOfWork.Doctors.FindAsync(d => d.DepartmentId == departmentId);
            return Ok(doctors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving doctors by department {DepartmentId}", departmentId);
            return StatusCode(500, "An error occurred while retrieving doctors");
        }
    }

    /// <summary>
    /// Gets a doctor by email
    /// </summary>
    [HttpGet("by-email/{email}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Doctor>> GetDoctorByEmail(string email)
    {
        try
        {
            var doctor = await _unitOfWork.Doctors.GetByEmailAsync(email);
            
            if (doctor == null)
            {
                return NotFound($"Doctor with email {email} not found");
            }

            return Ok(doctor);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving doctor for email {Email}", email);
            return StatusCode(500, "An error occurred while retrieving the doctor");
        }
    }

    /// <summary>
    /// Creates a new doctor
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Doctor>> CreateDoctor([FromBody] Doctor doctor)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var created = await _unitOfWork.Doctors.AddAsync(doctor);
            await _unitOfWork.SaveChangesAsync();
            
            // Invalidate cache
            await _cacheService.RemoveAsync(CacheKeys.AllDoctors);
            
            _logger.LogInformation("Created doctor {DoctorId}", created.Id);
            
            return CreatedAtAction(nameof(GetDoctor), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating doctor");
            return StatusCode(500, "An error occurred while creating the doctor");
        }
    }

    /// <summary>
    /// Updates an existing doctor
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Doctor")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateDoctor(int id, [FromBody] Doctor doctor)
    {
        try
        {
            if (id != doctor.Id)
            {
                return BadRequest("ID mismatch");
            }

            var existing = await _unitOfWork.Doctors.GetByIdAsync(id);
            if (existing == null)
            {
                return NotFound($"Doctor with ID {id} not found");
            }

            await _unitOfWork.Doctors.UpdateAsync(doctor);
            await _unitOfWork.SaveChangesAsync();
            
            _logger.LogInformation("Updated doctor {DoctorId}", id);
            
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating doctor {DoctorId}", id);
            return StatusCode(500, "An error occurred while updating the doctor");
        }
    }

    /// <summary>
    /// Deletes a doctor
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteDoctor(int id)
    {
        try
        {
            var doctor = await _unitOfWork.Doctors.GetByIdAsync(id);
            
            if (doctor == null)
            {
                return NotFound($"Doctor with ID {id} not found");
            }

            await _unitOfWork.Doctors.DeleteAsync(doctor);
            await _unitOfWork.SaveChangesAsync();
            
            _logger.LogInformation("Deleted doctor {DoctorId}", id);
            
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting doctor {DoctorId}", id);
            return StatusCode(500, "An error occurred while deleting the doctor");
        }
    }

    /// <summary>
    /// Gets appointments for a doctor
    /// </summary>
    [HttpGet("{id}/appointments")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<Appointment>>> GetDoctorAppointments(int id)
    {
        try
        {
            var doctor = await _unitOfWork.Doctors.GetByIdAsync(id);
            if (doctor == null)
            {
                return NotFound($"Doctor with ID {id} not found");
            }

            var appointments = await _unitOfWork.Appointments.GetAppointmentsByDoctorIdAsync(id);
            return Ok(appointments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving appointments for doctor {DoctorId}", id);
            return StatusCode(500, "An error occurred while retrieving appointments");
        }
    }

    /// <summary>
    /// Gets reviews for a doctor
    /// </summary>
    [HttpGet("{id}/reviews")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<DoctorReview>>> GetDoctorReviews(int id)
    {
        try
        {
            var doctor = await _unitOfWork.Doctors.GetByIdAsync(id);
            if (doctor == null)
            {
                return NotFound($"Doctor with ID {id} not found");
            }

            var reviews = await _unitOfWork.DoctorReviews.FindAsync(r => r.DoctorId == id);
            return Ok(reviews);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving reviews for doctor {DoctorId}", id);
            return StatusCode(500, "An error occurred while retrieving reviews");
        }
    }
}
