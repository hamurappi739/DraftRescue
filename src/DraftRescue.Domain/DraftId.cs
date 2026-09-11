namespace DraftRescue.Domain.Drafts;

public readonly record struct DraftId(Guid Value)
{
    public static DraftId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString("N");
}
