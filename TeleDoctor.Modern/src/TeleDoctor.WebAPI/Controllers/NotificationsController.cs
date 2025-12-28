using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeleDoctor.Core.Entities;
using TeleDoctor.Core.Interfaces;
using System.Security.Claims;

namespace TeleDoctor.WebAPI.Controllers;

/// <summary>
/// API controller for system notifications
/// Handles CRUD operations for user notifications
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(
        IUnitOfWork unitOfWork,
        ILogger<NotificationsController> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Gets notifications for the current user
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<SystemNotification>>> GetMyNotifications(
        [FromQuery] bool unreadOnly = false)
    {
        try
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdString == null || !int.TryParse(userIdString, out int userId))
            {
                return Unauthorized("User not authenticated");
            }

            var notifications = unreadOnly
                ? await _unitOfWork.SystemNotifications.FindAsync(n => 
                    n.TargetUserId == userId && !n.IsRead)
                : await _unitOfWork.SystemNotifications.FindAsync(n => 
                    n.TargetUserId == userId);

            return Ok(notifications.OrderByDescending(n => n.CreatedAt));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving notifications");
            return StatusCode(500, "An error occurred while retrieving notifications");
        }
    }

    /// <summary>
    /// Gets notifications for a specific user (Admin only)
    /// </summary>
    [HttpGet("user/{userId}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<SystemNotification>>> GetUserNotifications(int userId)
    {
        try
        {
            var notifications = await _unitOfWork.SystemNotifications.FindAsync(n => 
                n.TargetUserId == userId);

            return Ok(notifications.OrderByDescending(n => n.CreatedAt));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving notifications for user {UserId}", userId);
            return StatusCode(500, "An error occurred while retrieving notifications");
        }
    }

    /// <summary>
    /// Gets a specific notification by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SystemNotification>> GetNotification(int id)
    {
        try
        {
            var notification = await _unitOfWork.SystemNotifications.GetByIdAsync(id);
            
            if (notification == null)
            {
                return NotFound($"Notification with ID {id} not found");
            }

            // Verify user owns this notification
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdString == null || !int.TryParse(userIdString, out int userId))
            {
                return Unauthorized("User not authenticated");
            }

            if (notification.TargetUserId != userId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            return Ok(notification);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving notification {NotificationId}", id);
            return StatusCode(500, "An error occurred while retrieving the notification");
        }
    }

    /// <summary>
    /// Gets unread notification count for current user
    /// </summary>
    [HttpGet("unread-count")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<int>> GetUnreadCount()
    {
        try
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdString == null || !int.TryParse(userIdString, out int userId))
            {
                return Unauthorized("User not authenticated");
            }

            var count = await _unitOfWork.SystemNotifications.CountAsync(n => 
                n.TargetUserId == userId && !n.IsRead);
            
            return Ok(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unread notification count");
            return StatusCode(500, "An error occurred while getting unread count");
        }
    }

    /// <summary>
    /// Creates a new notification
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Coordinator")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SystemNotification>> CreateNotification([FromBody] SystemNotification notification)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            notification.CreatedAt = DateTime.UtcNow;
            notification.IsRead = false;

            var created = await _unitOfWork.SystemNotifications.AddAsync(notification);
            await _unitOfWork.SaveChangesAsync();
            
            _logger.LogInformation("Created notification {NotificationId} for user {UserId}", 
                created.Id, created.TargetUserId);
            
            return CreatedAtAction(nameof(GetNotification), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating notification");
            return StatusCode(500, "An error occurred while creating the notification");
        }
    }

    /// <summary>
    /// Marks a notification as read
    /// </summary>
    [HttpPut("{id}/mark-read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        try
        {
            var notification = await _unitOfWork.SystemNotifications.GetByIdAsync(id);
            if (notification == null)
            {
                return NotFound($"Notification with ID {id} not found");
            }

            // Verify user owns this notification
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdString == null || !int.TryParse(userIdString, out int userId))
            {
                return Unauthorized("User not authenticated");
            }

            if (notification.TargetUserId != userId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;

            await _unitOfWork.SystemNotifications.UpdateAsync(notification);
            await _unitOfWork.SaveChangesAsync();
            
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification {NotificationId} as read", id);
            return StatusCode(500, "An error occurred while updating the notification");
        }
    }

    /// <summary>
    /// Marks all notifications as read for current user
    /// </summary>
    [HttpPut("mark-all-read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkAllAsRead()
    {
        try
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdString == null || !int.TryParse(userIdString, out int userId))
            {
                return Unauthorized("User not authenticated");
            }

            var unreadNotifications = await _unitOfWork.SystemNotifications.FindAsync(n => 
                n.TargetUserId == userId && !n.IsRead);

            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
                await _unitOfWork.SystemNotifications.UpdateAsync(notification);
            }

            await _unitOfWork.SaveChangesAsync();
            
            _logger.LogInformation("Marked all notifications as read for user {UserId}", userId);
            
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all notifications as read");
            return StatusCode(500, "An error occurred while updating notifications");
        }
    }

    /// <summary>
    /// Deletes a notification
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteNotification(int id)
    {
        try
        {
            var notification = await _unitOfWork.SystemNotifications.GetByIdAsync(id);
            
            if (notification == null)
            {
                return NotFound($"Notification with ID {id} not found");
            }

            // Verify user owns this notification
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdString == null || !int.TryParse(userIdString, out int userId))
            {
                return Unauthorized("User not authenticated");
            }

            if (notification.TargetUserId != userId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            await _unitOfWork.SystemNotifications.DeleteAsync(notification);
            await _unitOfWork.SaveChangesAsync();
            
            _logger.LogInformation("Deleted notification {NotificationId}", id);
            
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting notification {NotificationId}", id);
            return StatusCode(500, "An error occurred while deleting the notification");
        }
    }

    /// <summary>
    /// Deletes all read notifications for current user
    /// </summary>
    [HttpDelete("clear-read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ClearReadNotifications()
    {
        try
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdString == null || !int.TryParse(userIdString, out int userId))
            {
                return Unauthorized("User not authenticated");
            }

            var readNotifications = await _unitOfWork.SystemNotifications.FindAsync(n => 
                n.TargetUserId == userId && n.IsRead);

            await _unitOfWork.SystemNotifications.DeleteRangeAsync(readNotifications);
            await _unitOfWork.SaveChangesAsync();
            
            _logger.LogInformation("Cleared read notifications for user {UserId}", userId);
            
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing read notifications");
            return StatusCode(500, "An error occurred while clearing notifications");
        }
    }
}
