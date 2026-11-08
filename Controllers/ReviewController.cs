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
using Vaulted.Auth;
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
        // EXEC AddReviewToMedia @MediaId, @Content, @Rating, @UserId
        // The author is the signed-in user (never taken from the request body).
        // Returns the created review and the media's recalculated averageRating.
        // The procedure runs its own transaction.
        [HttpPost("add-review")]
        public async Task<IActionResult> AddReview([FromBody] CreateReviewDTO createReviewDTO)
        {
            try
            {
                string SQLQueryRaw = "EXEC AddReviewToMedia @MediaId, @Content, @Rating, @UserId";
                var parameters = new[]
                {
                    new SqlParameter("@MediaId", createReviewDTO.MediaId),
                    new SqlParameter("@Content", createReviewDTO.Content ?? (object)DBNull.Value),
                    new SqlParameter("@Rating", createReviewDTO.Rating),
                    new SqlParameter("@UserId", User.GetUserId())
                };

                var res = await _context.Database.SqlQueryRaw<ReviewWithRatingDTO>(SQLQueryRaw, parameters).ToListAsync();
                return Ok(res.FirstOrDefault());
            }
            catch (Exception ex)
            {
                if (SqlErrors.ToResult(ex) is { } known) return known;
                Console.WriteLine($"Error adding review: {ex.Message}");
                return StatusCode(500, "An error occurred while adding the review.");
            }
        }




        // ------------ PUT ------------ //
        // PUT -> UPDATE REVIEW BY ID
        // EXEC UpdateReviewById @ReviewId, @Content, @Rating, @UserId
        // Only the review's author can change it (403 otherwise).
        // Returns the updated review and the media's recalculated averageRating.

        [HttpPut("update-review-by-id/{reviewId}")]
        public async Task<IActionResult> UpdateReviewById(Guid reviewId, [FromBody] UpdateReviewDTO updateReviewDTO)
        {
            try
            {
                string SQLQueryRaw = "EXEC UpdateReviewById @ReviewId, @Content, @Rating, @UserId";
                var parameters = new[]
                {
                    new SqlParameter("@ReviewId", reviewId),
                    new SqlParameter("@Content", updateReviewDTO.Content ?? (object)DBNull.Value),
                    new SqlParameter("@Rating", updateReviewDTO.Rating),
                    new SqlParameter("@UserId", User.GetUserId())
                };
                var res = await _context.Database.SqlQueryRaw<ReviewWithRatingDTO>(SQLQueryRaw, parameters).ToListAsync();
                var updated = res.FirstOrDefault();
                if (updated == null) return NotFound("Review not found.");
                return Ok(updated);
            }
            catch (Exception ex)
            {
                if (SqlErrors.ToResult(ex) is { } known) return known;
                Console.WriteLine($"Error updating review: {ex.Message}");
                return StatusCode(500, "An error occurred while updating the review.");
            }

        }


        // ------------ DELETE ------------ //
        // DELETE -> DELETE REVIEW BY ID
        // EXEC DeleteReviewById @ReviewId, @UserId
        // Only the review's author can delete it (403 otherwise).
        // Returns { mediaId, averageRating } after recalculation.
        [HttpDelete("delete-review-by-id/{reviewId}")]
        public async Task<IActionResult> DeleteReviewById(Guid reviewId)
        {
            try
            {
                string SQLQueryRaw = "EXEC DeleteReviewById @ReviewId, @UserId";
                var parameters = new[]
                {
                    new SqlParameter("@ReviewId", reviewId),
                    new SqlParameter("@UserId", User.GetUserId())
                };
                var res = await _context.Database.SqlQueryRaw<MediaRatingDTO>(SQLQueryRaw, parameters).ToListAsync();
                var rating = res.FirstOrDefault();
                if (rating == null) return NotFound("Review not found.");
                return Ok(rating);
            }
            catch (Exception ex)
            {
                if (SqlErrors.ToResult(ex) is { } known) return known;
                Console.WriteLine($"Error deleting review: {ex.Message}");
                return StatusCode(500, "An error occurred while deleting the review.");
            }
        }
    }
}
