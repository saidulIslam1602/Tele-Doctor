using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeleDoctor.Core.Entities;
using TeleDoctor.Core.Interfaces;
using TeleDoctor.Application.Common;

namespace TeleDoctor.WebAPI.Controllers;

/// <summary>
/// API controller for department management
/// Handles CRUD operations for hospital departments
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DepartmentsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DepartmentsController> _logger;

    public DepartmentsController(
        IUnitOfWork unitOfWork,
        ILogger<DepartmentsController> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Gets all departments with pagination
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<Department>>> GetDepartments([FromQuery] PaginationParams pagination)
    {
        try
        {
            var departments = await _unitOfWork.Departments.GetAllAsync();
            var totalCount = departments.Count();
            var pagedResult = departments.AsQueryable()
                .ToPagedResult(pagination.PageNumber, pagination.PageSize, totalCount);
            return Ok(pagedResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving departments");
            return StatusCode(500, "An error occurred while retrieving departments");
        }
    }

    /// <summary>
    /// Gets a specific department by ID
    /// </summary>
    [HttpGet("{id}")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Department>> GetDepartment(int id)
    {
        try
        {
            var department = await _unitOfWork.Departments.GetByIdAsync(id);
            
            if (department == null)
            {
                return NotFound($"Department with ID {id} not found");
            }

            return Ok(department);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving department {DepartmentId}", id);
            return StatusCode(500, "An error occurred while retrieving the department");
        }
    }

    /// <summary>
    /// Gets doctors in a department
    /// </summary>
    [HttpGet("{id}/doctors")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<Doctor>>> GetDepartmentDoctors(int id)
    {
        try
        {
            var department = await _unitOfWork.Departments.GetByIdAsync(id);
            if (department == null)
            {
                return NotFound($"Department with ID {id} not found");
            }

            var doctors = await _unitOfWork.Doctors.FindAsync(d => d.DepartmentId == id);
            return Ok(doctors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving doctors for department {DepartmentId}", id);
            return StatusCode(500, "An error occurred while retrieving doctors");
        }
    }

    /// <summary>
    /// Creates a new department
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Department>> CreateDepartment([FromBody] Department department)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var created = await _unitOfWork.Departments.AddAsync(department);
            await _unitOfWork.SaveChangesAsync();
            
            _logger.LogInformation("Created department {DepartmentId}: {DepartmentName}", 
                created.Id, created.Name);
            
            return CreatedAtAction(nameof(GetDepartment), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating department");
            return StatusCode(500, "An error occurred while creating the department");
        }
    }

    /// <summary>
    /// Updates an existing department
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateDepartment(int id, [FromBody] Department department)
    {
        try
        {
            if (id != department.Id)
            {
                return BadRequest("ID mismatch");
            }

            var existing = await _unitOfWork.Departments.GetByIdAsync(id);
            if (existing == null)
            {
                return NotFound($"Department with ID {id} not found");
            }

            await _unitOfWork.Departments.UpdateAsync(department);
            await _unitOfWork.SaveChangesAsync();
            
            _logger.LogInformation("Updated department {DepartmentId}", id);
            
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating department {DepartmentId}", id);
            return StatusCode(500, "An error occurred while updating the department");
        }
    }

    /// <summary>
    /// Deletes a department
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteDepartment(int id)
    {
        try
        {
            var department = await _unitOfWork.Departments.GetByIdAsync(id);
            
            if (department == null)
            {
                return NotFound($"Department with ID {id} not found");
            }

            // Check if department has doctors
            var doctorCount = await _unitOfWork.Doctors.CountAsync(d => d.DepartmentId == id);
            if (doctorCount > 0)
            {
                return BadRequest($"Cannot delete department with {doctorCount} doctors assigned. Please reassign doctors first.");
            }

            await _unitOfWork.Departments.DeleteAsync(department);
            await _unitOfWork.SaveChangesAsync();
            
            _logger.LogInformation("Deleted department {DepartmentId}", id);
            
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting department {DepartmentId}", id);
            return StatusCode(500, "An error occurred while deleting the department");
        }
    }

    /// <summary>
    /// Searches departments by name
    /// </summary>
    [HttpGet("search")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Department>>> SearchDepartments([FromQuery] string query)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest("Search query is required");
            }

            var departments = await _unitOfWork.Departments.FindAsync(d => 
                d.Name.Contains(query) || 
                (d.Description != null && d.Description.Contains(query)));

            return Ok(departments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching departments");
            return StatusCode(500, "An error occurred while searching departments");
        }
    }
}
