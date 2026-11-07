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
    // Notable quotes for a media item (shown as a carousel in the details)
    [Route("api/[controller]")]
    [ApiController]
    public class QuoteController : ControllerBase
    {
        private readonly ApplicationContext _context;

        private const int MaxTextLength = 2000;
        private const int MaxFromLength = 255;

        public QuoteController(ApplicationContext context)
        {
            _context = context;
        }

        // ------------ GET ------------ //

        // GET -> GET QUOTES BY MEDIA ID (in display order)
        // EXEC GetQuotesByMediaId @MediaId
        [HttpGet("get-quotes-by-media-id/{mediaId}")]
        public async Task<IActionResult> GetQuotesByMediaId(Guid mediaId)
        {
            try
            {
                var res = await _context.Database.SqlQueryRaw<QuoteDTO>(
                    "EXEC GetQuotesByMediaId @MediaId",
                    new SqlParameter("@MediaId", mediaId)
                ).ToListAsync();
                return Ok(res);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching quotes: {ex.Message}");
                return StatusCode(500, "An error occurred while fetching quotes.");
            }
        }


        // ------------ POST ------------ //

        // POST -> ADD QUOTE (appended to the end)
        // EXEC AddQuoteToMedia @MediaId, @QuoteText, @QuoteFrom
        [HttpPost("add-quote")]
        public async Task<IActionResult> AddQuote([FromBody] CreateQuoteDTO dto)
        {
            var (text, from, error) = Validate(dto.QuoteText, dto.QuoteFrom);
            if (error != null) return error;

            try
            {
                var res = await _context.Database.SqlQueryRaw<QuoteDTO>(
                    "EXEC AddQuoteToMedia @MediaId, @QuoteText, @QuoteFrom",
                    new SqlParameter("@MediaId", dto.MediaId),
                    new SqlParameter("@QuoteText", text),
                    new SqlParameter("@QuoteFrom", (object?)from ?? DBNull.Value)
                ).ToListAsync();
                return Ok(res.FirstOrDefault());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding quote: {ex.Message}");
                return StatusCode(500, "An error occurred while adding the quote.");
            }
        }


        // ------------ PUT ------------ //

        // PUT -> EDIT QUOTE
        // EXEC UpdateQuoteById @QuoteId, @QuoteText, @QuoteFrom
        [HttpPut("update-quote/{quoteId}")]
        public async Task<IActionResult> UpdateQuote(Guid quoteId, [FromBody] UpdateQuoteDTO dto)
        {
            var (text, from, error) = Validate(dto.QuoteText, dto.QuoteFrom);
            if (error != null) return error;

            try
            {
                var res = await _context.Database.SqlQueryRaw<QuoteDTO>(
                    "EXEC UpdateQuoteById @QuoteId, @QuoteText, @QuoteFrom",
                    new SqlParameter("@QuoteId", quoteId),
                    new SqlParameter("@QuoteText", text),
                    new SqlParameter("@QuoteFrom", (object?)from ?? DBNull.Value)
                ).ToListAsync();
                var updated = res.FirstOrDefault();
                if (updated == null) return NotFound("Quote not found.");
                return Ok(updated);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating quote: {ex.Message}");
                return StatusCode(500, "An error occurred while updating the quote.");
            }
        }

        // PUT -> REORDER QUOTES FOR A MEDIA ITEM
        // EXEC ReorderMediaQuotes @MediaId, @QuoteIds (JSON array). Returns the quotes in order.
        [HttpPut("reorder-quotes/{mediaId}")]
        public async Task<IActionResult> ReorderQuotes(Guid mediaId, [FromBody] ReorderQuotesDTO dto)
        {
            try
            {
                var res = await _context.Database.SqlQueryRaw<QuoteDTO>(
                    "EXEC ReorderMediaQuotes @MediaId, @QuoteIds",
                    new SqlParameter("@MediaId", mediaId),
                    new SqlParameter("@QuoteIds", JsonSerializer.Serialize(dto.QuoteIds))
                ).ToListAsync();
                return Ok(res);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reordering quotes: {ex.Message}");
                return StatusCode(500, "An error occurred while reordering quotes.");
            }
        }


        // ------------ DELETE ------------ //

        // DELETE -> DELETE QUOTE
        // EXEC DeleteQuoteById @QuoteId
        [HttpDelete("delete-quote/{quoteId}")]
        public async Task<IActionResult> DeleteQuote(Guid quoteId)
        {
            try
            {
                var res = await _context.Database.SqlQueryRaw<QuoteDeletedDTO>(
                    "EXEC DeleteQuoteById @QuoteId",
                    new SqlParameter("@QuoteId", quoteId)
                ).ToListAsync();
                var deleted = res.FirstOrDefault();
                if (deleted == null) return NotFound("Quote not found.");
                return Ok(new { quoteId, deleted.MediaId });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting quote: {ex.Message}");
                return StatusCode(500, "An error occurred while deleting the quote.");
            }
        }


        // Text required (max 2000), "from" optional (max 255); both trimmed
        private (string text, string? from, IActionResult? error) Validate(string? quoteText, string? quoteFrom)
        {
            var text = quoteText?.Trim() ?? "";
            var from = string.IsNullOrWhiteSpace(quoteFrom) ? null : quoteFrom.Trim();
            if (text.Length == 0) return (text, from, BadRequest("The quote can't be empty."));
            if (text.Length > MaxTextLength) return (text, from, BadRequest($"Quotes can be at most {MaxTextLength} characters."));
            if (from?.Length > MaxFromLength) return (text, from, BadRequest($"\"From\" can be at most {MaxFromLength} characters."));
            return (text, from, null);
        }
    }
}
