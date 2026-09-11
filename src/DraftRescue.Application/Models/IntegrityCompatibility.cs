namespace DraftRescue.Application.Models;

/// <summary>
/// Structural relationship between DraftRescue and a target process token.
/// Unknown is deliberately not treated as compatible by later security gates.
/// </summary>
public enum IntegrityCompatibility
{
    Compatible = 0,
    Incompatible = 1,
    Unknown = 2
}
