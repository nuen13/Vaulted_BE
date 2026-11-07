public class QuoteDTO
{
    public Guid Id { get; set; }

    public Guid MediaId { get; set; }

    public string? QuoteText { get; set; }

    // Who said it / where it's from; optional
    public string? QuoteFrom { get; set; }

    public int SortOrder { get; set; }
}

public class CreateQuoteDTO
{
    public Guid MediaId { get; set; }

    public string? QuoteText { get; set; }

    public string? QuoteFrom { get; set; }
}

public class UpdateQuoteDTO
{
    public string? QuoteText { get; set; }

    public string? QuoteFrom { get; set; }
}

public class ReorderQuotesDTO
{
    // Quote ids in the new display order
    public List<Guid> QuoteIds { get; set; } = new();
}

// Returned by DeleteQuoteById
public class QuoteDeletedDTO
{
    public Guid MediaId { get; set; }
}
