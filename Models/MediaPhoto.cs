using System;
using System.Collections.Generic;

namespace Vaulted.Models;

public partial class MediaPhoto
{
    public Guid Id { get; set; }

    public Guid MediaId { get; set; }

    public string? PhotoUrl { get; set; }

    public virtual MediaItem Media { get; set; } = null!;
}
