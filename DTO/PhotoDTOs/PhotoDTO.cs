public class PhotoDTO
{
    public Guid Id { get; set; }

    public Guid MediaId { get; set; }

    public string? PhotoUrl { get; set; }

    public int SortOrder { get; set; }
}

public class CreatePhotoDTO
{
    public Guid MediaId { get; set; }

    public string? PhotoUrl { get; set; }
}

public class ReorderPhotosDTO
{
    // Photo ids in the new display order
    public List<Guid> PhotoIds { get; set; } = new();
}

// Returned by SetPhotoAsCover
public class CoverChangedDTO
{
    public Guid MediaId { get; set; }

    public string? CoverPhotoUrl { get; set; }
}

// Returned by DeletePhotoById
public class PhotoDeletedDTO
{
    public Guid MediaId { get; set; }
}
