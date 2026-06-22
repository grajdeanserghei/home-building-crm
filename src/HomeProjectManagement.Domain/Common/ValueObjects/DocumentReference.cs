namespace HomeProjectManagement.Domain.Common.ValueObjects;

/// <summary>
/// A reference to an uploaded document (future use: Contract attachments). Kept as a
/// placeholder value object so the domain shape is stable before file storage lands.
/// </summary>
public sealed class DocumentReference : ValueObject
{
    public string FileName { get; }
    public string Url { get; }
    public DateTimeOffset UploadedOn { get; }
    public UserId UploadedBy { get; }

    public DocumentReference(string fileName, string url, DateTimeOffset uploadedOn, UserId uploadedBy)
    {
        FileName = fileName;
        Url = url;
        UploadedOn = uploadedOn;
        UploadedBy = uploadedBy;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return FileName;
        yield return Url;
        yield return UploadedOn;
        yield return UploadedBy;
    }
}
