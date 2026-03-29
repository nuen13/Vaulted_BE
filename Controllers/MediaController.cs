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

        // GET: api/Media
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
                // Log the exception (you can use a logging framework here)
                Console.WriteLine($"Error fetching media: {ex.Message}");
                return StatusCode(500, "An error occurred while fetching media.");
            }
        }


        // GET: get all media order by category name
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
                // Log the exception (you can use a logging framework here)
                Console.WriteLine($"Error fetching media: {ex.Message}");
                return StatusCode(500, "An error occurred while fetching media.");
            }
        }



        // GET: get all media -> search by name
        [HttpGet("search-media-by-name/{searchTerm}")]
        public async Task<IActionResult> SearchMediaByTitle(string searchTerm)
        {
            try
            {
                await _context.Database.BeginTransactionAsync();

                SqlParameter paramName = new SqlParameter("@SearchTerm", System.Data.SqlDbType.NVarChar, 100) { Value = searchTerm };

                string SQLQueryRaw = $"EXEC SearchMediaByName @SearchTerm = '{searchTerm}'";
                var res = await _context.Database.SqlQueryRaw<MediaDTO>(SQLQueryRaw, paramName).ToListAsync();
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




        // Get by category
        [HttpGet("get-media-by-categoryid/{categoryId}")]
        public async Task<IActionResult> GetMediaByCategoryId(int categoryId)
        {
            try
            {
                await _context.Database.BeginTransactionAsync();

                SqlParameter paramCategoryId = new SqlParameter("@CategoryId", System.Data.SqlDbType.Int) { Value = categoryId };

                string SQLQueryRaw = $"EXEC GetMediaByCategoryId @CategoryId = {categoryId}";
                var res = await _context.Database.SqlQueryRaw<MediaDTO>(SQLQueryRaw, paramCategoryId).ToListAsync();
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


        [HttpGet("get-media-by-category/{categoryName}")]
        public async Task<IActionResult> GetMediaByCategory(string categoryName)
        {
            try
            {
                await _context.Database.BeginTransactionAsync();

                SqlParameter paramCategory = new SqlParameter("@Category", System.Data.SqlDbType.NVarChar, 50) { Value = categoryName };

                string SQLQueryRaw = $"EXEC GetMediaByCategory @Category = '{categoryName}'";
                var res = await _context.Database.SqlQueryRaw<MediaDTO>(SQLQueryRaw, paramCategory).ToListAsync();
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


        [HttpPost("create-media")]
        public async Task<IActionResult> CreateMedia([FromBody] MediaCreatedDTO dto)
        {
            try
            {
                // Define the parameters exactly as they appear in your ALTER PROCEDURE
                var pTitle = new SqlParameter("@MediaTitle", dto.MediaTitle ?? (object)DBNull.Value);
                var pCat = new SqlParameter("@CategoryId", dto.CategoryId);
                var pCover = new SqlParameter("@CoverPhotoUrl", dto.CoverPhotoUrl ?? (object)DBNull.Value);
                var pStatus = new SqlParameter("@Status", dto.Status ?? (object)DBNull.Value);
                var pLink = new SqlParameter("@MediaLink", dto.MediaLink ?? (object)DBNull.Value);

                // Execute the procedure
                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC InsertMediaItem @MediaTitle, @CategoryId, @CoverPhotoUrl, @Status, @MediaLink",
                    pTitle, pCat, pCover, pStatus, pLink
                );

                return Ok("Media created successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return StatusCode(500, "An error occurred.");
            }
        }


        [HttpPut("update-media/{id}")]
        public async Task<IActionResult> UpdateMedia(Guid id, [FromBody] MediaCreatedDTO dto)
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


        [HttpDelete("delete-media/{id}")]
        public async Task<IActionResult> DeleteMedia(Guid id)
        {
            try
            {
                var pId = new SqlParameter("@Id", id);

                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC DeleteMediaItemById @Id",
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
