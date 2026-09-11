namespace DraftRescue.Domain.Drafts;

public enum DraftLifecycleState
{
    Active = 0,
    Recoverable = 1,
    Restored = 2,
    Discarded = 3,
    Expired = 4
}
