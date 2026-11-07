using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Vaulted.Models;
namespace Vaulted.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PhotoController : ControllerBase
    {
        private readonly ApplicationContext _context;

        public PhotoController(ApplicationContext context)
        {
            _context = context;
        }

        // ------------ GET ------------ //

        // GET -> GET PHOTOS BY MEDIA ID (in display order)
        // EXEC GetPhotosByMediaId @MediaId
        [HttpGet("get-photos-by-media-id/{mediaId}")]
        public async Task<IActionResult> GetPhotosByMediaId(Guid mediaId)
        {
            try
            {
                string SQLQueryRaw = "EXEC GetPhotosByMediaId @MediaId";
                var parameters = new[]
                {
                    new SqlParameter("@MediaId", mediaId)
                };
                var res = await _context.Database.SqlQueryRaw<PhotoDTO>(SQLQueryRaw, parameters).ToListAsync();
                return Ok(res);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching photos: {ex.Message}");
                return StatusCode(500, "An error occurred while fetching photos.");
            }
        }


        // ------------ POST ------------ //

        // POST -> ADD PHOTO TO MEDIA (appended to the end)
        // EXEC AddPhotoToMedia @MediaId, @PhotoUrl
        [HttpPost("add-photo")]
        public async Task<IActionResult> AddPhoto([FromBody] CreatePhotoDTO createPhotoDTO)
        {
            var photoUrl = createPhotoDTO.PhotoUrl?.Trim();
            if (!IsValidPhotoUrl(photoUrl))
            {
                return BadRequest("Photo URL must be an http(s) link of at most 500 characters.");
            }

            try
            {
                string SQLQueryRaw = "EXEC AddPhotoToMedia @MediaId, @PhotoUrl";
                var parameters = new[]
                {
                    new SqlParameter("@MediaId", createPhotoDTO.MediaId),
                    new SqlParameter("@PhotoUrl", photoUrl)
                };
                var res = await _context.Database.SqlQueryRaw<PhotoDTO>(SQLQueryRaw, parameters).ToListAsync();
                return Ok(res.FirstOrDefault());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding photo: {ex.Message}");
                return StatusCode(500, "An error occurred while adding the photo.");
            }
        }


        // ------------ PUT ------------ //

        // PUT -> REORDER PHOTOS FOR A MEDIA ITEM
        // EXEC ReorderMediaPhotos @MediaId, @PhotoIds (JSON array)
        // Returns the photos in their new order.
        [HttpPut("reorder-photos/{mediaId}")]
        public async Task<IActionResult> ReorderPhotos(Guid mediaId, [FromBody] ReorderPhotosDTO reorderPhotosDTO)
        {
            try
            {
                string SQLQueryRaw = "EXEC ReorderMediaPhotos @MediaId, @PhotoIds";
                var parameters = new[]
                {
                    new SqlParameter("@MediaId", mediaId),
                    new SqlParameter("@PhotoIds", JsonSerializer.Serialize(reorderPhotosDTO.PhotoIds))
                };
                var res = await _context.Database.SqlQueryRaw<PhotoDTO>(SQLQueryRaw, parameters).ToListAsync();
                return Ok(res);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reordering photos: {ex.Message}");
                return StatusCode(500, "An error occurred while reordering photos.");
            }
        }


        // PUT -> SET PHOTO AS COVER (swaps it with the current cover)
        // EXEC SetPhotoAsCover @PhotoId
        // Returns { mediaId, coverPhotoUrl }.
        [HttpPut("set-as-cover/{photoId}")]
        public async Task<IActionResult> SetPhotoAsCover(Guid photoId)
        {
            try
            {
                string SQLQueryRaw = "EXEC SetPhotoAsCover @PhotoId";
                var parameters = new[]
                {
                    new SqlParameter("@PhotoId", photoId)
                };
                var res = await _context.Database.SqlQueryRaw<CoverChangedDTO>(SQLQueryRaw, parameters).ToListAsync();
                var changed = res.FirstOrDefault();
                if (changed == null) return NotFound("Photo not found.");
                return Ok(changed);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting cover photo: {ex.Message}");
                return StatusCode(500, "An error occurred while setting the cover photo.");
            }
        }


        // ------------ DELETE ------------ //

        // DELETE -> DELETE PHOTO BY ID
        // EXEC DeletePhotoById @PhotoId
        [HttpDelete("delete-photo-by-id/{photoId}")]
        public async Task<IActionResult> DeletePhotoById(Guid photoId)
        {
            try
            {
                string SQLQueryRaw = "EXEC DeletePhotoById @PhotoId";
                var parameters = new[]
                {
                    new SqlParameter("@PhotoId", photoId)
                };
                var res = await _context.Database.SqlQueryRaw<PhotoDeletedDTO>(SQLQueryRaw, parameters).ToListAsync();
                var deleted = res.FirstOrDefault();
                if (deleted == null) return NotFound("Photo not found.");
                return Ok(new { photoId, deleted.MediaId });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting photo: {ex.Message}");
                return StatusCode(500, "An error occurred while deleting the photo.");
            }
        }


        // Only absolute http(s) links that fit the PhotoUrl column
        private static bool IsValidPhotoUrl(string? url)
        {
            return !string.IsNullOrEmpty(url)
                && url.Length <= 500
                && Uri.TryCreate(url, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }
    }
}
