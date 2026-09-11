namespace DraftRescue.Domain.Context;

public readonly record struct ApplicationId(string Value)
{
    public override string ToString() => Value;
}
