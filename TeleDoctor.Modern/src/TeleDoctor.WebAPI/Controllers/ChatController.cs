using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeleDoctor.Core.Entities;
using TeleDoctor.Core.Interfaces;

namespace TeleDoctor.WebAPI.Controllers;

/// <summary>
/// API controller for chat message management
/// Handles CRUD operations for chat messages
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        IUnitOfWork unitOfWork,
        ILogger<ChatController> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Gets chat messages between patient and doctor
    /// </summary>
    [HttpGet("conversation")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ChatMessage>>> GetConversation(
        [FromQuery] int patientId, 
        [FromQuery] int doctorId)
    {
        try
        {
            var messages = await _unitOfWork.ChatMessages.FindAsync(m => 
                (m.PatientId == patientId && m.DoctorId == doctorId) ||
                (m.PatientId == doctorId && m.DoctorId == patientId));

            return Ok(messages.OrderBy(m => m.SentAt));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving conversation between patient {PatientId} and doctor {DoctorId}", 
                patientId, doctorId);
            return StatusCode(500, "An error occurred while retrieving messages");
        }
    }

    /// <summary>
    /// Gets chat messages for an appointment
    /// </summary>
    [HttpGet("appointment/{appointmentId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ChatMessage>>> GetAppointmentMessages(int appointmentId)
    {
        try
        {
            var messages = await _unitOfWork.ChatMessages.FindAsync(m => m.AppointmentId == appointmentId);
            return Ok(messages.OrderBy(m => m.SentAt));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving messages for appointment {AppointmentId}", appointmentId);
            return StatusCode(500, "An error occurred while retrieving messages");
        }
    }

    /// <summary>
    /// Gets a specific message by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChatMessage>> GetMessage(int id)
    {
        try
        {
            var message = await _unitOfWork.ChatMessages.GetByIdAsync(id);
            
            if (message == null)
            {
                return NotFound($"Message with ID {id} not found");
            }

            return Ok(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving message {MessageId}", id);
            return StatusCode(500, "An error occurred while retrieving the message");
        }
    }

    /// <summary>
    /// Sends a new chat message
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ChatMessage>> SendMessage([FromBody] ChatMessage message)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            message.SentAt = DateTime.UtcNow;
            message.IsRead = false;

            var created = await _unitOfWork.ChatMessages.AddAsync(message);
            await _unitOfWork.SaveChangesAsync();
            
            _logger.LogInformation("Message sent from Patient:{PatientId} to Doctor:{DoctorId}", 
                message.PatientId, message.DoctorId);
            
            return CreatedAtAction(nameof(GetMessage), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message");
            return StatusCode(500, "An error occurred while sending the message");
        }
    }

    /// <summary>
    /// Marks a message as read
    /// </summary>
    [HttpPut("{id}/mark-read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        try
        {
            var message = await _unitOfWork.ChatMessages.GetByIdAsync(id);
            if (message == null)
            {
                return NotFound($"Message with ID {id} not found");
            }

            message.IsRead = true;
            message.ReadAt = DateTime.UtcNow;

            await _unitOfWork.ChatMessages.UpdateAsync(message);
            await _unitOfWork.SaveChangesAsync();
            
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking message {MessageId} as read", id);
            return StatusCode(500, "An error occurred while updating the message");
        }
    }

    /// <summary>
    /// Gets unread message count for a user
    /// </summary>
    [HttpGet("unread-count/{userId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<int>> GetUnreadCount(int userId)
    {
        try
        {
            // Count unread messages where this user is the recipient
            // For patients: count messages from doctors
            // For doctors: count messages from patients
            var count = await _unitOfWork.ChatMessages.CountAsync(m => 
                (m.PatientId == userId || m.DoctorId == userId) && !m.IsRead);
            
            return Ok(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unread count for user {UserId}", userId);
            return StatusCode(500, "An error occurred while getting unread count");
        }
    }

    /// <summary>
    /// Deletes a message
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteMessage(int id)
    {
        try
        {
            var message = await _unitOfWork.ChatMessages.GetByIdAsync(id);
            
            if (message == null)
            {
                return NotFound($"Message with ID {id} not found");
            }

            await _unitOfWork.ChatMessages.DeleteAsync(message);
            await _unitOfWork.SaveChangesAsync();
            
            _logger.LogInformation("Deleted message {MessageId}", id);
            
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting message {MessageId}", id);
            return StatusCode(500, "An error occurred while deleting the message");
        }
    }
}
