namespace HomeProjectManagement.Application.Activity;

/// <summary>
/// The discriminator values for <see cref="ActivityItemDto.Kind"/>. Kept as string constants (rather
/// than an enum) so the wire contract is an explicit, stable set of names the frontend switches on.
/// </summary>
public static class ActivityKind
{
    public const string NoteLogged = "NoteLogged";
    public const string BidOpened = "BidOpened";
    public const string WorkPackageAdded = "WorkPackageAdded";
    public const string ProjectCreated = "ProjectCreated";
}
