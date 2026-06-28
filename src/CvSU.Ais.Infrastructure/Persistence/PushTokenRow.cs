namespace CvSU.Ais.Infrastructure.Persistence;

/// <summary>Device push notification token registered by a user.</summary>
public sealed class PushTokenRow
{
    public int Id { get; set; }
    public string UserId { get; set; } = default!;
    public string Token { get; set; } = default!;
    public string Platform { get; set; } = default!;
    public bool IsActive { get; set; }
    public DateTime RegisteredOn { get; set; }
    public DateTime? LastUsedOn { get; set; }
}
