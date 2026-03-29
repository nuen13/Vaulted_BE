using System;
using System.Collections.Generic;

namespace Vaulted.Models;

public partial class MediaQuote
{
    public Guid Id { get; set; }

    public Guid MediaId { get; set; }

    public string? QuoteFrom { get; set; }

    public string? QuoteText { get; set; }

    public virtual MediaItem Media { get; set; } = null!;
}
