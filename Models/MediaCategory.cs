using System;
using System.Collections.Generic;

namespace Vaulted.Models;

public partial class MediaCategory
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<MediaItem> MediaItems { get; set; } = new List<MediaItem>();
}
