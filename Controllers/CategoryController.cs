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
    public class CategoryController : ControllerBase
    {

        private readonly ApplicationContext _context;
        public CategoryController(ApplicationContext context)
        {
            _context = context;
        }


        // ------------ GET ------------ //
        [HttpGet("get-all-categories")]
        public async Task<IActionResult> GetAllCategories()
        {
            try
            {
                await _context.Database.BeginTransactionAsync();

                string SQLQueryRaw = "EXEC GetAllCategories";
                var res = await _context.Database.SqlQueryRaw<CategoryDTO>(SQLQueryRaw).ToListAsync();
                await _context.Database.CommitTransactionAsync();
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
        [HttpPost("add-category")]
        public async Task<IActionResult> AddCategory([FromBody] CreateCategoryDTO categoryCreatedDTO)
        {
            try
            {
                await _context.Database.BeginTransactionAsync();

                string SQLQueryRaw = "EXEC AddCategory @Name";
                var parameters = new[]
                {
                    new SqlParameter("@Name", categoryCreatedDTO.Name)
                };
                await _context.Database.ExecuteSqlRawAsync(SQLQueryRaw, parameters);
                await _context.Database.CommitTransactionAsync();
                return Ok("Category added successfully.");
            }
            catch (Exception ex)
            {
                // Log the exception (you can use a logging framework here)
                Console.WriteLine($"Error adding category: {ex.Message}");
                return StatusCode(500, "An error occurred while adding the category.");
            }
        }

        // ------------ PUT ------------ //
        [HttpPut("update-category/{id}")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] UpdateCategoryDTO updateCategoryDTO)
        {
            try
            {
                await _context.Database.BeginTransactionAsync();

                string SQLQueryRaw = "EXEC UpdateCategoryById @CategoryId, @Name";
                var parameters = new[]
                {
                    new SqlParameter("@CategoryId", updateCategoryDTO.Id),
                    new SqlParameter("@Name", updateCategoryDTO.Name)
                };
                await _context.Database.ExecuteSqlRawAsync(SQLQueryRaw, parameters);
                await _context.Database.CommitTransactionAsync();
                return Ok("Category updated successfully.");
            }
            catch (Exception ex)
            {
                // Log the exception (you can use a logging framework here)
                Console.WriteLine($"Error updating category: {ex.Message}");
                return StatusCode(500, "An error occurred while updating the category.");
            }
        }

        // ------------ DELETE ------------ //
        [HttpDelete("delete-category/{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {   
            try
            {
                await _context.Database.BeginTransactionAsync();

                var pId = new SqlParameter("@Id", id);

                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC DeleteCategoryById @Id",
                    pId
                );

                await _context.Database.CommitTransactionAsync();
                return Ok("Category deleted successfully.");
            }
            catch (Exception ex)
            {
                // Log the exception (you can use a logging framework here)
                Console.WriteLine($"Error deleting category: {ex.Message}");
                return StatusCode(500, "An error occurred while deleting the category.");
            }
        }
    }
}