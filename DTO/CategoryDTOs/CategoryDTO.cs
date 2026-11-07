public class CategoryDTO
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string? Color { get; set; }

    // Media items in this category (soft-deleted items not counted)
    public int MediaCount { get; set; }
}

// Returned by DeleteCategoryById
public class CategoryDeleteResultDTO
{
    public bool Deleted { get; set; }
    public int ItemCount { get; set; }
}
