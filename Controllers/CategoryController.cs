using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Vaulted.Models;


namespace Vaulted.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoryController : ControllerBase
    {

        private readonly ApplicationContext _context;
        public CategoryController(ApplicationContext context)
        {
            _context = context;
        }

        private const int MaxNameLength = 50;
        private static readonly Regex HexColor = new("^#[0-9a-fA-F]{6}$");


        // ------------ GET ------------ //
        // Returns { id, name, color, mediaCount } for every category
        [HttpGet("get-all-categories")]
        public async Task<IActionResult> GetAllCategories()
        {
            try
            {
                string SQLQueryRaw = "EXEC GetAllCategories";
                var res = await _context.Database.SqlQueryRaw<CategoryDTO>(SQLQueryRaw).ToListAsync();
                return Ok(res);
            }
            catch (Exception ex)
            {
                // Log the exception (you can use a logging framework here)
                Console.WriteLine($"Error fetching categories: {ex.Message}");
                return StatusCode(500, "An error occurred while fetching categories.");
            }
        }


        // ------------ POST ------------ //
        // Returns the created category
        [HttpPost("add-category")]
        public async Task<IActionResult> AddCategory([FromBody] CreateCategoryDTO categoryCreatedDTO)
        {
            var name = categoryCreatedDTO.Name?.Trim();
            var error = await ValidateAsync(name, categoryCreatedDTO.Color, excludeId: null);
            if (error != null) return error;

            try
            {
                string SQLQueryRaw = "EXEC AddCategory @Name, @Color";
                var parameters = new[]
                {
                    new SqlParameter("@Name", name),
                    new SqlParameter("@Color", (object?)categoryCreatedDTO.Color?.ToLowerInvariant() ?? DBNull.Value)
                };
                var res = await _context.Database.SqlQueryRaw<CategoryDTO>(SQLQueryRaw, parameters).ToListAsync();
                return Ok(res.FirstOrDefault());
            }
            catch (Exception ex)
            {
                // Log the exception (you can use a logging framework here)
                Console.WriteLine($"Error adding category: {ex.Message}");
                return StatusCode(500, "An error occurred while adding the category.");
            }
        }

        // ------------ PUT ------------ //
        // Rename and/or recolor. Returns the updated category.
        [HttpPut("update-category/{id}")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] UpdateCategoryDTO updateCategoryDTO)
        {
            var name = updateCategoryDTO.Name?.Trim();
            var error = await ValidateAsync(name, updateCategoryDTO.Color, excludeId: id);
            if (error != null) return error;

            try
            {
                string SQLQueryRaw = "EXEC UpdateCategoryById @CategoryId, @Name, @Color";
                var parameters = new[]
                {
                    new SqlParameter("@CategoryId", id),
                    new SqlParameter("@Name", name),
                    new SqlParameter("@Color", (object?)updateCategoryDTO.Color?.ToLowerInvariant() ?? DBNull.Value)
                };
                var res = await _context.Database.SqlQueryRaw<CategoryDTO>(SQLQueryRaw, parameters).ToListAsync();
                var updated = res.FirstOrDefault();
                if (updated == null) return NotFound("Category not found.");
                return Ok(updated);
            }
            catch (Exception ex)
            {
                // Log the exception (you can use a logging framework here)
                Console.WriteLine($"Error updating category: {ex.Message}");
                return StatusCode(500, "An error occurred while updating the category.");
            }
        }

        // ------------ DELETE ------------ //
        // A category that still has media items is refused with 409 { itemCount } unless
        // ?reassignToId= names another category to move those items to first.
        [HttpDelete("delete-category/{id}")]
        public async Task<IActionResult> DeleteCategory(int id, [FromQuery] int? reassignToId)
        {
            if (reassignToId != null)
            {
                if (reassignToId == id) return BadRequest("Pick a different category to move the items to.");
                if (!await _context.MediaCategories.AnyAsync(c => c.Id == reassignToId))
                    return BadRequest("The category to move items to doesn't exist.");
            }

            try
            {
                var pId = new SqlParameter("@CategoryId", id);
                var pReassign = new SqlParameter("@ReassignToId", (object?)reassignToId ?? DBNull.Value);

                var res = await _context.Database.SqlQueryRaw<CategoryDeleteResultDTO>(
                    "EXEC DeleteCategoryById @CategoryId, @ReassignToId",
                    pId, pReassign
                ).ToListAsync();

                var result = res.FirstOrDefault();
                if (result == null) return NotFound("Category not found.");
                if (!result.Deleted)
                {
                    return Conflict(new
                    {
                        message = $"This category still has {result.ItemCount} media item(s). Move them to another category first.",
                        itemCount = result.ItemCount
                    });
                }
                return Ok(new { id, movedItems = result.ItemCount, reassignToId });
            }
            catch (Exception ex)
            {
                // Log the exception (you can use a logging framework here)
                Console.WriteLine($"Error deleting category: {ex.Message}");
                return StatusCode(500, "An error occurred while deleting the category.");
            }
        }


        // Name required, at most 50 characters, unique (case-insensitive); color optional #rrggbb
        private async Task<IActionResult?> ValidateAsync(string? name, string? color, int? excludeId)
        {
            if (string.IsNullOrEmpty(name))
                return BadRequest("Category name is required.");
            if (name.Length > MaxNameLength)
                return BadRequest($"Category name must be at most {MaxNameLength} characters.");
            if (color != null && !HexColor.IsMatch(color))
                return BadRequest("Color must be a hex value like #1e90ff.");
            if (await _context.MediaCategories.AnyAsync(c => c.Name == name && c.Id != excludeId))
                return Conflict(new { message = $"A category named \"{name}\" already exists." });
            return null;
        }
    }
}
