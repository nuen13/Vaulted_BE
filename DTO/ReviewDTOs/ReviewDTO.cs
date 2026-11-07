public class ReviewDTO
{
    public Guid Id { get; set; }

    public Guid MediaId { get; set; }

    public string? Content { get; set; }

    public int? Rating { get; set; }

    public DateTime? DateCreated { get; set; }
}

// Returned by add/update: the review plus the media's recalculated average.
public class ReviewWithRatingDTO : ReviewDTO
{
    public decimal? AverageRating { get; set; }
}

// Returned by delete: the media's recalculated average.
public class MediaRatingDTO
{
    public Guid MediaId { get; set; }

    public decimal? AverageRating { get; set; }
}