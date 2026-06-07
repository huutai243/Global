namespace ECommerce.Shared.Inbox;

public enum InboxMessageStatus
{
    Received = 0,
    Processing = 1,
    Processed = 2,
    Failed = 3,
    DeadLettered = 4
}
