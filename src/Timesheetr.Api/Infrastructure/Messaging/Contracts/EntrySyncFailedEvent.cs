namespace Timesheetr.Api.Infrastructure.Messaging.Contracts;

public record EntrySyncFailedEvent(long TogglId, string ErrorMessage, bool LoggedToTempo);
