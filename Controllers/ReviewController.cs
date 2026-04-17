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
    public class ReviewController : ControllerBase
    {
        private readonly ApplicationContext _context;

        public ReviewController(ApplicationContext context)
        {
            _context = context;
        }

        // ------------ GET ------------ //

        // GET -> GET REVIEWS BY MEDIA ID
        // EXEC GetReviewsByMediaId @MediaId
        [HttpGet("get-reviews-by-media-id/{mediaId}")]
        public async Task<IActionResult> GetReviewsByMediaId(Guid mediaId)
        {
            try
            {
                await _context.Database.BeginTransactionAsync();

                string SQLQueryRaw = "EXEC GetReviewsByMediaId @MediaId";
                var parameters = new[]
                {
                    new SqlParameter("@MediaId", mediaId)
                };
                var res = await _context.Database.SqlQueryRaw<ReviewDTO>(SQLQueryRaw, parameters).ToListAsync();
                await _context.Database.CommitTransactionAsync();
                return Ok(res);
            }
            catch (Exception ex)
            {
                // Log the exception (you can use a logging framework here)
                Console.WriteLine($"Error fetching reviews: {ex.Message}");
                return StatusCode(500, "An error occurred while fetching reviews.");
            }
        }


        // ------------ POST ------------ //

        // POST -> ADD REVIEW TO MEDIA
        // EXEC AddReviewToMedia @MediaId, @Content, @Rating
        [HttpPost("add-review")]
        public async Task<IActionResult> AddReview([FromBody] CreateReviewDTO createReviewDTO)
        {
            try
            {
                await _context.Database.BeginTransactionAsync();

                string SQLQueryRaw = "EXEC AddReviewToMedia @MediaId, @Content, @Rating";
                var parameters = new[]
                {
                    new SqlParameter("@MediaId", createReviewDTO.MediaId),
                    new SqlParameter("@Content", createReviewDTO.Content),
                    new SqlParameter("@Rating", createReviewDTO.Rating)
                };
                await _context.Database.ExecuteSqlRawAsync(SQLQueryRaw, parameters);
                await _context.Database.CommitTransactionAsync();
                return Ok("Review added successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding review: {ex.Message}");
                return StatusCode(500, "An error occurred while adding the review.");
            }
        }


        // ------------ PUT ------------ //
        // PUT -> UPDATE REVIEW BY ID
        // EXEC UpdateReviewById @ReviewId, @Content, @Rating

        [HttpPut("update-review-by-id/{reviewId}")]
        public async Task<IActionResult> UpdateReviewById(Guid reviewId, [FromBody] UpdateReviewDTO updateReviewDTO)
        {
            try
            {
                await _context.Database.BeginTransactionAsync();

                string SQLQueryRaw = "EXEC UpdateReviewById @ReviewId, @Content, @Rating";
                var parameters = new[]
                {
                    new SqlParameter("@ReviewId", reviewId),
                    new SqlParameter("@Content", updateReviewDTO.Content),
                    new SqlParameter("@Rating", updateReviewDTO.Rating)
                };
                await _context.Database.ExecuteSqlRawAsync(SQLQueryRaw, parameters);
                await _context.Database.CommitTransactionAsync();
                return Ok("Review updated successfully.");
            }
            catch (Exception ex)
            {
                // Log the exception (you can use a logging framework here)
                Console.WriteLine($"Error updating review: {ex.Message}");
                return StatusCode(500, "An error occurred while updating the review.");
            }

        }


        // ------------ DELETE ------------ //
        // DELETE -> DELETE REVIEW BY ID
        // EXEC DeleteReviewById @ReviewId
        [HttpDelete("delete-review-by-id/{reviewId}")]
        public async Task<IActionResult> DeleteReviewById(Guid reviewId)
        {
            try
            {
                await _context.Database.BeginTransactionAsync();

                string SQLQueryRaw = "EXEC DeleteReviewById @ReviewId";
                var parameters = new[]
                {
                    new SqlParameter("@ReviewId", reviewId)
                };
                await _context.Database.ExecuteSqlRawAsync(SQLQueryRaw, parameters);
                await _context.Database.CommitTransactionAsync();
                return Ok("Review deleted successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting review: {ex.Message}");
                return StatusCode(500, "An error occurred while deleting the review.");
            }
        }
    }
}