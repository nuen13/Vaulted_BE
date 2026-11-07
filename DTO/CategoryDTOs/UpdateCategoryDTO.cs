public class UpdateCategoryDTO
{
    // Kept for backward compatibility; the id in the route is what's used
    public int Id { get; set; }
    public string Name { get; set; }

    // Hex color like #1e90ff; null leaves the color unchanged
    public string? Color { get; set; }
}
