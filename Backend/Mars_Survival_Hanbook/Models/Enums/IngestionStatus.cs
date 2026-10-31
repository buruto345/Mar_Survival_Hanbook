namespace Mars_Survival_Hanbook.Models.Enums;

public enum IngestionStatus
{
    Queued = 0,
    Running = 1,
    Succeeded = 2,
    PartiallySucceeded = 3,
    Failed = 4,
    Cancelled = 5
}

