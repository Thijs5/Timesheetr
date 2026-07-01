using Wolverine;

namespace Timesheetr.Api.Infrastructure.Messaging.Contracts;

public record EntryStatusChanged(
    long TogglId,
    string Status,
    string? ErrorMessage,
    bool LoggedToTempo,
    DateTimeOffset? NextRetryAt) : WebSocketMessage;
