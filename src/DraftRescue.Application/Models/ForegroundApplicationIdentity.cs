using DomainApplicationId = DraftRescue.Domain.Context.ApplicationId;

namespace DraftRescue.Application.Models;

/// <summary>
/// Content-free identity of the current foreground top-level window.
/// </summary>
public readonly record struct ForegroundApplicationIdentity(
    DomainApplicationId Application,
    int ProcessId,
    nint NativeWindowHandle);
