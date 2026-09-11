using DraftRescue.Domain.Context;

namespace DraftRescue.Application.Models;

/// <summary>
/// Minimal normalized metadata about a candidate editable field.
/// It deliberately contains no typed text or field value.
/// </summary>
public sealed record ObservedFieldContext(
    DraftRescue.Domain.Context.ApplicationId Application,
    ContextFingerprint Window,
    ContextFingerprint Field);
