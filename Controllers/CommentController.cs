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
    public class CommentController : ControllerBase
    {
        private readonly ApplicationContext _context;

        public CommentController(ApplicationContext context)
        {
            _context = context;
        }


        [HttpPost("add-comment")]
        public async Task<IActionResult> AddComment([FromBody] CommentCreatedDTO commentCreatedDTO)
        {
            try
            {
                await _context.Database.BeginTransactionAsync();

                string SQLQueryRaw = "EXEC AddCommentToMedia @MediaId, @Content, @Rating";
                var parameters = new[]
                {
                    new SqlParameter("@MediaId", commentCreatedDTO.MediaId),
                    new SqlParameter("@Content", commentCreatedDTO.Content),
                    new SqlParameter("@Rating", commentCreatedDTO.Rating)
                };
                await _context.Database.ExecuteSqlRawAsync(SQLQueryRaw, parameters);
                await _context.Database.CommitTransactionAsync();
                return Ok("Comment added successfully.");
            }
            catch (Exception ex)
            {
                // Log the exception (you can use a logging framework here)
                Console.WriteLine($"Error adding comment: {ex.Message}");
                return StatusCode(500, "An error occurred while adding the comment.");
            }
        }


        [HttpGet("get-comments-by-media-id/{mediaId}")]
        public async Task<IActionResult> GetCommentsByMediaId(Guid mediaId)
        {
            try
            {
                await _context.Database.BeginTransactionAsync();

                string SQLQueryRaw = "EXEC GetCommentsByMediaId @MediaId";
                var parameters = new[]
                {
                    new SqlParameter("@MediaId", mediaId)
                };
                var res = await _context.Database.SqlQueryRaw<CommentDTO>(SQLQueryRaw, parameters).ToListAsync();
                await _context.Database.CommitTransactionAsync();
                return Ok(res);
            }
            catch (Exception ex)
            {
                // Log the exception (you can use a logging framework here)
                Console.WriteLine($"Error fetching comments: {ex.Message}");
                return StatusCode(500, "An error occurred while fetching comments.");
            }



    }
    }


}