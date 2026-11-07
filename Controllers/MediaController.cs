using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlClient.DataClassification;
using Microsoft.EntityFrameworkCore;
using Vaulted.Models;
namespace Vaulted.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class MediaController : ControllerBase
    {
        private readonly ApplicationContext _context;

        public MediaController(ApplicationContext context)
        {
            _context = context;
        }

        // ------------ GET ------------ // 
        // GET -> GET ALL MEDIA -> EXEC GetAllMedia
        [HttpGet("get-all-media")]
        public async Task<IActionResult> GetAllMedia([FromQuery] bool archived = false)
        {
            try
            {
                await _context.Database.BeginTransactionAsync();

                string SQLQueryRaw = "EXEC GetAllMedia @Archived";
                var res = await _context.Database.SqlQueryRaw<MediaDTO>(SQLQueryRaw, ArchivedParam(archived)).ToListAsync();
                await _context.Database.CommitTransactionAsync();
                return Ok(res);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching media: {ex.Message}");
                return StatusCode(500, "An error occurred while fetching media.");
            }
        }

        // --- --- --- //
        // GET: GET ALL MEDIA ORDER BY CATEGORY 
        // EXEC GetAllMediaSortedByCategory
        [HttpGet("get-all-media-order-by-category")]
        public async Task<IActionResult> GetAllMediaOrderByCategory()
        {
            try
            {
                await _context.Database.BeginTransactionAsync();

                string SQLQueryRaw = "EXEC GetAllMediaSortedByCategory";
                var res = await _context.Database.SqlQueryRaw<MediaSortedByCategoryDTO>(SQLQueryRaw).ToListAsync();
                await _context.Database.CommitTransactionAsync();
                return Ok(res);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching media: {ex.Message}");
                return StatusCode(500, "An error occurred while fetching media.");
            }
        }

        // --- --- --- //
        // GET -> GET MEDIA BY MEDIA NAME 
        // EXEC SearchMediaByName
        [HttpGet("search-media-by-name/{searchTerm}")]
        public async Task<IActionResult> SearchMediaByTitle(string searchTerm)
        {
            try
            {
                await _context.Database.BeginTransactionAsync();

                SqlParameter paramName = new SqlParameter("@SearchTerm", System.Data.SqlDbType.NVarChar, 100) { Value = searchTerm };

                string SQLQueryRaw = "EXEC SearchMediaByName @SearchTerm";
                var res = await _context.Database.SqlQueryRaw<MediaSearchedBySearchTermDTO>(SQLQueryRaw, paramName).ToListAsync();
                await _context.Database.CommitTransactionAsync();
                return Ok(res);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching media: {ex.Message}");
                return StatusCode(500, "An error occurred while fetching media.");
            }
        }
        // GET -> GET MEDIA BY MEDIA ID
        // EXEC GetMediaById
        [HttpGet("get-media-by-id/{id}")]
        public async Task<IActionResult> GetMediaById(Guid id)
        {
            try
            {
                await _context.Database.BeginTransactionAsync();

                SqlParameter paramId = new SqlParameter("@MediaId", System.Data.SqlDbType.UniqueIdentifier) { Value = id };

                string SQLQueryRaw = "EXEC GetMediaById @MediaId";
                var res = await _context.Database.SqlQueryRaw<MediaByIdDTO>(SQLQueryRaw, paramId).ToListAsync();
                await _context.Database.CommitTransactionAsync();
                return Ok(res);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching media: {ex.Message}");
                return StatusCode(500, "An error occurred while fetching media.");
            }
        }


        // GET -> GET MEDIA BY CATEGORY ID
        // EXEC GetMediaByCategoryId
        [HttpGet("get-media-by-categoryid/{categoryId}")]
        public async Task<IActionResult> GetMediaByCategoryId(int categoryId, [FromQuery] bool archived = false)
        {
            try
            {
                await _context.Database.BeginTransactionAsync();

                SqlParameter paramCategoryId = new SqlParameter("@CategoryId", System.Data.SqlDbType.Int) { Value = categoryId };

                string SQLQueryRaw = "EXEC GetMediaByCategoryId @CategoryId, @Archived";
                var res = await _context.Database.SqlQueryRaw<MediaByCategoryIdDTO>(SQLQueryRaw, paramCategoryId, ArchivedParam(archived)).ToListAsync();
                await _context.Database.CommitTransactionAsync();
                return Ok(res);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching media: {ex.Message}");
                return StatusCode(500, "An error occurred while fetching media.");
            }
        }

        // GET -> GET MEDIA BY CATEGORY NAME
        // EXEC GetMediaByCategoryName
        [HttpGet("get-media-by-category/{categoryName}")]
        public async Task<IActionResult> GetMediaByCategory(string categoryName)
        {
            try
            {
                await _context.Database.BeginTransactionAsync();

                SqlParameter paramCategory = new SqlParameter("@CategoryName", System.Data.SqlDbType.NVarChar, 50) { Value = categoryName };

                string SQLQueryRaw = "EXEC GetMediaByCategoryName @CategoryName";
                var res = await _context.Database.SqlQueryRaw<MediaByCategoryNameDTO>(SQLQueryRaw, paramCategory).ToListAsync();
                await _context.Database.CommitTransactionAsync();
                return Ok(res);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching media: {ex.Message}");
                return StatusCode(500, "An error occurred while fetching media.");
            }

        }


        // GET -> GET MEDIA BY STATUS
        // EXEC GetMediaItemByStatus
        [HttpGet("get-media-by-status/{status}")]
        public async Task<IActionResult> GetMediaByStatus(string status, [FromQuery] bool archived = false)
        {
            try
            {
                await _context.Database.BeginTransactionAsync();

                SqlParameter paramStatus = new SqlParameter("@Status", System.Data.SqlDbType.NVarChar, 50) { Value = status };

                string SQLQueryRaw = "EXEC GetMediaItemByStatus @Status, @Archived";
                var res = await _context.Database.SqlQueryRaw<MediaByStatusDTO>(SQLQueryRaw, paramStatus, ArchivedParam(archived)).ToListAsync();
                await _context.Database.CommitTransactionAsync();
                return Ok(res);
            }
            catch (Exception ex)
            {
                // Log the exception (you can use a logging framework here)
                Console.WriteLine($"Error fetching media: {ex.Message}");
                return StatusCode(500, "An error occurred while fetching media.");
            }

        }

        // GET -> GET MEDIA BY CATEGORY ID AND STATUS
        // EXEC GetMediaItemByCategoryIdAndStatus
        [HttpGet("get-media-by-categoryId-and-status/categoryId={categoryId}&status={status}")]
        public async Task<IActionResult> GetMediaItemByCategoryIdAndStatus(int categoryId, string status, [FromQuery] bool archived = false)
        {
            try
            {
                await _context.Database.BeginTransactionAsync();

                SqlParameter paramCategoryId = new SqlParameter("@CategoryId", System.Data.SqlDbType.Int) { Value = categoryId };
                SqlParameter paramStatus = new SqlParameter("@Status", System.Data.SqlDbType.NVarChar, 50) { Value = status };

                string SQLQueryRaw = "EXEC GetMediaItemByCategoryIdAndStatus @CategoryId, @Status, @Archived";
                var res = await _context.Database.SqlQueryRaw<MediaByCategoryAndStatusDTO>(SQLQueryRaw, paramCategoryId, paramStatus, ArchivedParam(archived)).ToListAsync();
                await _context.Database.CommitTransactionAsync();
                return Ok(res);
            }
            catch (Exception ex)
            {
                // Log the exception (you can use a logging framework here)
                Console.WriteLine($"Error fetching media: {ex.Message}");
                return StatusCode(500, "An error occurred while fetching media.");
            }

        }


        // ------------ POST ------------ // 

        // POST -> CREATE MEDIA
        // EXEC InsertMediaItem
        [HttpPost("create-media")]
        public async Task<IActionResult> CreateMedia([FromBody] CreateMediaDTO dto)
        {
            try
            {
                var pTitle = new SqlParameter("@MediaTitle", dto.MediaTitle ?? (object)DBNull.Value);
                var pCat = new SqlParameter("@CategoryId", dto.CategoryId);
                var pCover = new SqlParameter("@CoverPhotoUrl", dto.CoverPhotoUrl ?? (object)DBNull.Value);
                var pStatus = new SqlParameter("@Status", dto.Status ?? (object)DBNull.Value);
                var pLink = new SqlParameter("@MediaLink", dto.MediaLink ?? (object)DBNull.Value);

                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC InsertMediaItem @MediaTitle, @CategoryId, @CoverPhotoUrl, @Status, @MediaLink",
                    pTitle, pCat, pCover, pStatus, pLink
                );

                return Ok("Media created successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return StatusCode(500, "An error occurred while creating media.");
            }
        }

        // ------------ PUT ------------ // 

        // PUT -> UPDATE MEDIA BY ID
        // EXEC UpdateMediaItemById
        [HttpPut("update-media/{id}")]
        public async Task<IActionResult> UpdateMedia(Guid id, [FromBody] UpdateMediaDTO dto)
        {
            try
            {
                var pId = new SqlParameter("@Id", id);
                var pTitle = new SqlParameter("@MediaTitle", dto.MediaTitle ?? (object)DBNull.Value);
                var pCat = new SqlParameter("@CategoryId", dto.CategoryId);
                var pCover = new SqlParameter("@CoverPhotoUrl", dto.CoverPhotoUrl ?? (object)DBNull.Value);
                var pStatus = new SqlParameter("@Status", dto.Status ?? (object)DBNull.Value);
                var pLink = new SqlParameter("@MediaLink", dto.MediaLink ?? (object)DBNull.Value);

                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC UpdateMediaItemById @Id, @MediaTitle, @CategoryId, @CoverPhotoUrl, @Status, @MediaLink",
                    pId, pTitle, pCat, pCover, pStatus, pLink
                );

                return Ok("Media updated successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return StatusCode(500, "An error occurred.");
            }
        }


        // PUT -> UPDATE MEDIA STATUS BY ID
        // EXEC UpdateMediaStatusById
        [HttpPut("update-media-status/{id}")]
        public async Task<IActionResult> UpdateMediaStatus(Guid id, [FromBody] UpdateMediaStatusDTO dto)
        {
            try
            {
                var pId = new SqlParameter("@MediaId", id);
                var pStatus = new SqlParameter("@Status", dto.Status ?? (object)DBNull.Value);

                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC UpdateMediaStatusById @MediaId, @Status",
                    pId, pStatus
                );

                return Ok("Media status updated successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return StatusCode(500, "An error occurred.");
            }
        }

        // Average rating is recalculated server-side whenever a review is added,
        // updated, or deleted (see ReviewStoreProcedure.sql), so there is no rating endpoint.


        // PUT -> ARCHIVE / UNARCHIVE MEDIA BY ID
        // EXEC SetMediaArchived @MediaId, @Archived
        // Archived items leave the normal lists but keep their reviews, photos and rating.
        [HttpPut("set-archived/{id}")]
        public async Task<IActionResult> SetArchived(Guid id, [FromBody] SetArchivedDTO dto)
        {
            try
            {
                var pId = new SqlParameter("@MediaId", id);
                var res = await _context.Database.SqlQueryRaw<MediaDTO>(
                    "EXEC SetMediaArchived @MediaId, @Archived",
                    pId, ArchivedParam(dto.Archived)
                ).ToListAsync();

                var item = res.FirstOrDefault();
                if (item == null) return NotFound("Media not found.");
                return Ok(item);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return StatusCode(500, "An error occurred.");
            }
        }

        private static SqlParameter ArchivedParam(bool archived) =>
            new SqlParameter("@Archived", System.Data.SqlDbType.Bit) { Value = archived };


        // PUT -> BULK EDIT (archive view): unarchive, set status, set category or delete many items
        // EXEC BulkUpdateMediaItems @Ids, @Archived, @Status, @CategoryId, @Deleted
        // Returns { updatedCount }
        [HttpPut("bulk-update")]
        public async Task<IActionResult> BulkUpdate([FromBody] BulkUpdateMediaDTO dto)
        {
            try
            {
                if (dto.Ids == null || dto.Ids.Count == 0)
                    return BadRequest("Pick at least one item.");
                if (dto.Archived == null && dto.Status == null && dto.CategoryId == null && dto.Deleted == null)
                    return BadRequest("Nothing to change.");

                var status = dto.Status?.Trim();
                if (status != null)
                {
                    var statuses = await _context.Database.SqlQueryRaw<StatusDTO>("EXEC GetAllStatuses").ToListAsync();
                    var match = statuses.FirstOrDefault(s => string.Equals(s.Name, status, StringComparison.OrdinalIgnoreCase));
                    if (match == null) return BadRequest("That status doesn't exist.");
                    status = match.Name;
                }
                if (dto.CategoryId != null)
                {
                    var categories = await _context.Database.SqlQueryRaw<CategoryDTO>("EXEC GetAllCategories").ToListAsync();
                    if (!categories.Any(c => c.Id == dto.CategoryId)) return BadRequest("That category doesn't exist.");
                }

                static SqlParameter Nullable(string name, System.Data.SqlDbType type, object? value) =>
                    new SqlParameter(name, type) { Value = value ?? DBNull.Value };

                var res = await _context.Database.SqlQueryRaw<BulkUpdateResultDTO>(
                    "EXEC BulkUpdateMediaItems @Ids, @Archived, @Status, @CategoryId, @Deleted",
                    new SqlParameter("@Ids", System.Text.Json.JsonSerializer.Serialize(dto.Ids)),
                    Nullable("@Archived", System.Data.SqlDbType.Bit, dto.Archived),
                    Nullable("@Status", System.Data.SqlDbType.VarChar, status),
                    Nullable("@CategoryId", System.Data.SqlDbType.Int, dto.CategoryId),
                    Nullable("@Deleted", System.Data.SqlDbType.Bit, dto.Deleted)
                ).ToListAsync();

                return Ok(res.FirstOrDefault() ?? new BulkUpdateResultDTO());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return StatusCode(500, "An error occurred.");
            }
        }


        // ------------ DELETE ------------ //



        // DELETE -> DELETE MEDIA BY ID (soft delete)
        // EXEC DeleteMediaItemById
        [HttpDelete("delete-media/{id}")]
        public async Task<IActionResult> DeleteMedia(Guid id)
        {
            try
            {
                var pId = new SqlParameter("@MediaId", id);

                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC DeleteMediaItemById @MediaId",
                    pId
                );

                return Ok("Media deleted successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return StatusCode(500, "An error occurred.");
            }

        }


    }
}
