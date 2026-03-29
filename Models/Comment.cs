using System;
using System.Collections.Generic;

namespace Vaulted.Models;

public partial class Comment
{
    public Guid Id { get; set; }

    public Guid MediaId { get; set; }

    public int? Rating { get; set; }

    public string? Content { get; set; }

    public DateTime? DateCreated { get; set; }

    public virtual MediaItem Media { get; set; } = null!;
}
