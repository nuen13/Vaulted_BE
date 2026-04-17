using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading.Tasks;
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
        public async Task<IActionResult> GetAllMedia()
        {
            try
            {
                await _context.Database.BeginTransactionAsync();

                string SQLQueryRaw = "EXEC GetAllMedia";
                var res = await _context.Database.SqlQueryRaw<MediaDTO>(SQLQueryRaw).ToListAsync();
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
                var res = await _context.Database.SqlQueryRaw<MediaSortedByCateDTO>(SQLQueryRaw).ToListAsync();
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

                string SQLQueryRaw = $"EXEC SearchMediaByName @SearchTerm = '{searchTerm}'";
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

        // GET -> GET MEDIA BY CATEGORY ID
        // EXEC GetMediaByCategoryId
        [HttpGet("get-media-by-categoryid/{categoryId}")]
        public async Task<IActionResult> GetMediaByCategoryId(int categoryId)
        {
            try
            {
                await _context.Database.BeginTransactionAsync();

                SqlParameter paramCategoryId = new SqlParameter("@CategoryId", System.Data.SqlDbType.Int) { Value = categoryId };

                string SQLQueryRaw = $"EXEC GetMediaByCategoryId @CategoryId = {categoryId}";
                var res = await _context.Database.SqlQueryRaw<MediaByCategoryIdDTO>(SQLQueryRaw, paramCategoryId).ToListAsync();
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

                SqlParameter paramCategory = new SqlParameter("@Category", System.Data.SqlDbType.NVarChar, 50) { Value = categoryName };

                string SQLQueryRaw = $"EXEC GetMediaByCategory @Category = '{categoryName}'";
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
        public async Task<IActionResult> GetMediaByStatus(string status)
        {
            try
            {
                await _context.Database.BeginTransactionAsync();

                SqlParameter paramStatus = new SqlParameter("@Status", System.Data.SqlDbType.NVarChar, 50) { Value = status };

                string SQLQueryRaw = $"EXEC GetMediaItemByStatus @Status = '{status}'";
                var res = await _context.Database.SqlQueryRaw<MediaByStatusDTO>(SQLQueryRaw, paramStatus).ToListAsync();
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
        public async Task<IActionResult> GetMediaItemByCategoryIdAndStatus(int categoryId, string status)
        {
            try
            {
                await _context.Database.BeginTransactionAsync();

                SqlParameter paramCategoryId = new SqlParameter("@CategoryId", System.Data.SqlDbType.Int) { Value = categoryId };
                SqlParameter paramStatus = new SqlParameter("@Status", System.Data.SqlDbType.NVarChar, 50) { Value = status };

                string SQLQueryRaw = $"EXEC GetMediaItemByCategoryIdAndStatus @CategoryId = {categoryId}, @Status = '{status}'";
                var res = await _context.Database.SqlQueryRaw<MediaByCategoryAndStatusDTO>(SQLQueryRaw, paramCategoryId, paramStatus).ToListAsync();
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

        // PUT -> UPDATE MEDIA RATING  BY ID
        // EXEC UpdateMediaRatingById
        [HttpPut("update-media-rating/{id}")]
        public async Task<IActionResult> UpdateMediaRating(Guid id, [FromBody] UpdateMediaRatingDTO dto)
        {
            try
            {
                var pId = new SqlParameter("@MediaId", id);
                var pAvgScore = new SqlParameter("@AverageRating", dto.AverageRating);

                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC UpdateMediaRatingById @MediaId, @AverageRating",
                    pId, pAvgScore
                );

                return Ok("Media rating updated successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return StatusCode(500, "An error occurred.");
            }
        }


        // ------------ DELETE ------------ //



        // DELETE -> DELETE MEDIA BY ID
        // EXEC DeleteMediaItemById
        [HttpDelete("delete-media/{id}")]
        public async Task<IActionResult> DeleteMedia(Guid id)
        {
            try
            {
                var pId = new SqlParameter("@Id", id);

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
