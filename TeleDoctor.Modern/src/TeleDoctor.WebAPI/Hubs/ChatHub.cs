using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace TeleDoctor.WebAPI.Hubs;

/// <summary>
/// SignalR Hub for real-time chat communication between patients and doctors
/// Provides instant messaging capabilities for medical consultations
/// </summary>
[Authorize]
public class ChatHub : Hub
{
    private readonly ILogger<ChatHub> _logger;
    
    // In-memory storage for active connections
    // In production, use Redis or distributed cache
    private static readonly Dictionary<string, string> _connections = new();

    public ChatHub(ILogger<ChatHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Called when a client connects to the hub
    /// Registers the connection and user mapping
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier ?? Context.ConnectionId;
        _connections[userId] = Context.ConnectionId;
        
        _logger.LogInformation("User connected to chat: {UserId}", userId);
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects from the hub
    /// Cleans up connection mappings
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier ?? Context.ConnectionId;
        _connections.Remove(userId);
        
        _logger.LogInformation("User disconnected from chat: {UserId}", userId);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Sends a chat message from one user to another
    /// </summary>
    /// <param name="recipientId">User ID of the recipient</param>
    /// <param name="message">Message content</param>
    public async Task SendMessage(string recipientId, string message)
    {
        var senderId = Context.UserIdentifier ?? Context.ConnectionId;
        
        _logger.LogDebug("Sending message from {SenderId} to {RecipientId}", senderId, recipientId);

        // Send message to recipient if they're online
        if (_connections.TryGetValue(recipientId, out var connectionId))
        {
            await Clients.Client(connectionId).SendAsync("ReceiveMessage", senderId, message, DateTime.UtcNow);
        }

        // Also send back to sender for confirmation
        await Clients.Caller.SendAsync("MessageSent", recipientId, message, DateTime.UtcNow);
    }

    /// <summary>
    /// Notifies when a user is typing
    /// </summary>
    /// <param name="recipientId">User ID to notify</param>
    public async Task UserTyping(string recipientId)
    {
        var senderId = Context.UserIdentifier ?? Context.ConnectionId;
        
        if (_connections.TryGetValue(recipientId, out var connectionId))
        {
            await Clients.Client(connectionId).SendAsync("UserTyping", senderId);
        }
    }

    /// <summary>
    /// Marks messages as read
    /// </summary>
    /// <param name="senderId">ID of the message sender</param>
    public async Task MarkAsRead(string senderId)
    {
        var recipientId = Context.UserIdentifier ?? Context.ConnectionId;
        
        if (_connections.TryGetValue(senderId, out var connectionId))
        {
            await Clients.Client(connectionId).SendAsync("MessagesRead", recipientId, DateTime.UtcNow);
        }
    }

    /// <summary>
    /// Joins a specific chat room/session
    /// </summary>
    /// <param name="roomId">Chat room identifier</param>
    public async Task JoinRoom(string roomId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
        _logger.LogInformation("User joined room: {RoomId}", roomId);
        
        await Clients.Group(roomId).SendAsync("UserJoined", Context.UserIdentifier, DateTime.UtcNow);
    }

    /// <summary>
    /// Leaves a chat room/session
    /// </summary>
    /// <param name="roomId">Chat room identifier</param>
    public async Task LeaveRoom(string roomId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);
        _logger.LogInformation("User left room: {RoomId}", roomId);
        
        await Clients.Group(roomId).SendAsync("UserLeft", Context.UserIdentifier, DateTime.UtcNow);
    }

    /// <summary>
    /// Sends a message to a specific chat room
    /// </summary>
    /// <param name="roomId">Chat room identifier</param>
    /// <param name="message">Message content</param>
    public async Task SendToRoom(string roomId, string message)
    {
        var senderId = Context.UserIdentifier ?? Context.ConnectionId;
        
        _logger.LogDebug("Sending message to room {RoomId} from {SenderId}", roomId, senderId);
        
        await Clients.Group(roomId).SendAsync("ReceiveRoomMessage", senderId, message, DateTime.UtcNow);
    }
}

/// <summary>
/// SignalR Hub for video call signaling
/// Handles WebRTC signaling for peer-to-peer video consultations
/// </summary>
[Authorize(Roles = "Doctor,Patient")]
public class VideoCallHub : Hub
{
    private readonly ILogger<VideoCallHub> _logger;
    
    // Track active video call sessions
    private static readonly Dictionary<string, VideoCallSession> _activeCalls = new();

    public VideoCallHub(ILogger<VideoCallHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Initiates a video call to another user
    /// </summary>
    /// <param name="recipientId">User ID to call</param>
    /// <param name="offer">WebRTC offer SDP</param>
    public async Task InitiateCall(string recipientId, string offer)
    {
        var callerId = Context.UserIdentifier ?? Context.ConnectionId;
        var callId = Guid.NewGuid().ToString();

        _activeCalls[callId] = new VideoCallSession
        {
            CallId = callId,
            CallerId = callerId,
            RecipientId = recipientId,
            StartTime = DateTime.UtcNow,
            Status = "Ringing"
        };

        _logger.LogInformation("Video call initiated: {CallId} from {CallerId} to {RecipientId}", 
            callId, callerId, recipientId);

        // Send call notification to recipient
        await Clients.User(recipientId).SendAsync("IncomingCall", callerId, callId, offer);
    }

    /// <summary>
    /// Accepts an incoming video call
    /// </summary>
    /// <param name="callId">Call identifier</param>
    /// <param name="answer">WebRTC answer SDP</param>
    public async Task AcceptCall(string callId, string answer)
    {
        if (_activeCalls.TryGetValue(callId, out var session))
        {
            session.Status = "Active";
            session.AcceptTime = DateTime.UtcNow;

            _logger.LogInformation("Video call accepted: {CallId}", callId);
            
            // Send answer to caller
            await Clients.User(session.CallerId).SendAsync("CallAccepted", callId, answer);
        }
    }

    /// <summary>
    /// Rejects an incoming video call
    /// </summary>
    /// <param name="callId">Call identifier</param>
    /// <param name="reason">Rejection reason</param>
    public async Task RejectCall(string callId, string reason)
    {
        if (_activeCalls.TryGetValue(callId, out var session))
        {
            session.Status = "Rejected";
            _activeCalls.Remove(callId);

            _logger.LogInformation("Video call rejected: {CallId}, Reason: {Reason}", callId, reason);
            
            // Notify caller
            await Clients.User(session.CallerId).SendAsync("CallRejected", callId, reason);
        }
    }

    /// <summary>
    /// Ends an active video call
    /// </summary>
    /// <param name="callId">Call identifier</param>
    public async Task EndCall(string callId)
    {
        if (_activeCalls.TryGetValue(callId, out var session))
        {
            session.Status = "Ended";
            session.EndTime = DateTime.UtcNow;
            _activeCalls.Remove(callId);

            _logger.LogInformation("Video call ended: {CallId}, Duration: {Duration}", 
                callId, session.EndTime - session.StartTime);
            
            // Notify both participants
            await Clients.Users(new[] { session.CallerId, session.RecipientId })
                .SendAsync("CallEnded", callId, DateTime.UtcNow);
        }
    }

    /// <summary>
    /// Exchanges ICE candidates for WebRTC connection
    /// </summary>
    /// <param name="callId">Call identifier</param>
    /// <param name="recipientId">User ID to send candidate to</param>
    /// <param name="candidate">ICE candidate data</param>
    public async Task SendICECandidate(string callId, string recipientId, string candidate)
    {
        _logger.LogDebug("Sending ICE candidate for call: {CallId}", callId);
        
        await Clients.User(recipientId).SendAsync("ICECandidate", callId, candidate);
    }

    /// <summary>
    /// Toggles video mute status
    /// </summary>
    /// <param name="callId">Call identifier</param>
    /// <param name="isMuted">Mute status</param>
    public async Task ToggleVideo(string callId, bool isMuted)
    {
        if (_activeCalls.TryGetValue(callId, out var session))
        {
            var userId = Context.UserIdentifier ?? Context.ConnectionId;
            var otherUserId = userId == session.CallerId ? session.RecipientId : session.CallerId;
            
            await Clients.User(otherUserId).SendAsync("VideoToggled", userId, isMuted);
        }
    }

    /// <summary>
    /// Toggles audio mute status
    /// </summary>
    /// <param name="callId">Call identifier</param>
    /// <param name="isMuted">Mute status</param>
    public async Task ToggleAudio(string callId, bool isMuted)
    {
        if (_activeCalls.TryGetValue(callId, out var session))
        {
            var userId = Context.UserIdentifier ?? Context.ConnectionId;
            var otherUserId = userId == session.CallerId ? session.RecipientId : session.CallerId;
            
            await Clients.User(otherUserId).SendAsync("AudioToggled", userId, isMuted);
        }
    }

    /// <summary>
    /// Internal class to track video call sessions
    /// </summary>
    private class VideoCallSession
    {
        public string CallId { get; set; } = string.Empty;
        public string CallerId { get; set; } = string.Empty;
        public string RecipientId { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime? AcceptTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
