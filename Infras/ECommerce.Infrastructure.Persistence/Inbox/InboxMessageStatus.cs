namespace ECommerce.Infrastructure.Persistence.Inbox;

public enum InboxMessageStatus
{
    Received = 1,
    Processing = 2,
    Processed = 3,
    Failed = 4,
    DeadLettered = 5
}