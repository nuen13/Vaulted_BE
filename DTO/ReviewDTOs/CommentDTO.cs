public class CommentDTO
{
    public Guid Id { get; set; }

    public Guid MediaId { get; set; }

    public string? Content { get; set; }

    public int Rating { get; set; }

    public DateTime DateCreated { get; set; }
}