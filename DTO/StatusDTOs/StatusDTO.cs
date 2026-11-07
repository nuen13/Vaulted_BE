public class StatusDTO
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int SortOrder { get; set; }

    // Media items with this status (deleted items not counted)
    public int MediaCount { get; set; }
}

public class SaveStatusDTO
{
    public string? Name { get; set; }
}

// Returned by DeleteStatusById
public class StatusDeleteResultDTO
{
    public bool Deleted { get; set; }
    public int ItemCount { get; set; }
}
