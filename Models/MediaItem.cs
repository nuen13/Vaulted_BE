using System;
using System.Collections.Generic;

namespace Vaulted.Models;

public partial class MediaItem
{
    public Guid Id { get; set; }

    public string? MediaTitle { get; set; }

    public int CategoryId { get; set; }

    public string? CoverPhotoUrl { get; set; }

    public decimal? AverageScore { get; set; }

    public string? Status { get; set; }

    public string? MediaLink { get; set; }

    public DateTime? DateCreated { get; set; }

    public DateTime? DateUpdated { get; set; }

    public bool? Deleted { get; set; }

    public virtual MediaCategory Category { get; set; } = null!;

    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();

    public virtual ICollection<MediaPhoto> MediaPhotos { get; set; } = new List<MediaPhoto>();

    public virtual ICollection<MediaQuote> MediaQuotes { get; set; } = new List<MediaQuote>();
}
