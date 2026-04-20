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
}