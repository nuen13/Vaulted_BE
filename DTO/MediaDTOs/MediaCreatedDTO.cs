public class MediaCreatedDTO
{

    public string? MediaTitle { get; set; }

    public int CategoryId { get; set; }

    public string? CoverPhotoUrl { get; set; }

    public string? Status { get; set; }

    public string? MediaLink { get; set; }




    // Why need ID, and DateCreated? Because when we create a new media item, we want to generate a unique ID for it and also set the creation date. This way, we can easily track when each media item was created and ensure that each item has a unique identifier.
    public MediaCreatedDTO()
    {
    }
}