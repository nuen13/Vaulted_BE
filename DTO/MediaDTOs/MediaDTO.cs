public class MediaDTO
{
    public Guid Id { get; set; }

    public string? MediaTitle { get; set; }

    public int CategoryId { get; set; }
    public string? CategoryName { get; set; }

    public string? CoverPhotoUrl { get; set; }

    public decimal? AverageRating { get; set; }

    public string? Status { get; set; }

    public string? MediaLink { get; set; }

    public DateTime? DateCreated { get; set; }

    public DateTime? DateUpdated { get; set; }

    // Archived items are hidden from the normal lists (see "Archived" filter)
    public bool Archived { get; set; }

    public DateTime? DateArchived { get; set; }
}

public class SetArchivedDTO
{
    public bool Archived { get; set; }
}
// Bulk edit (archive view): change several items at once. Fields left null are not changed.
public class BulkUpdateMediaDTO
{
    public List<Guid> Ids { get; set; } = new();
    public bool? Archived { get; set; }
    public string? Status { get; set; }
    public int? CategoryId { get; set; }
    public bool? Deleted { get; set; }
}

public class BulkUpdateResultDTO
{
    public int UpdatedCount { get; set; }
}
