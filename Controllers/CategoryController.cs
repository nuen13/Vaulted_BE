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

        [HttpGet("get-all-current-categories")]
        public async Task<IActionResult> GetAllCurrentCategories()
        {
            try
            {
                await _context.Database.BeginTransactionAsync();

                string SQLQueryRaw = "EXEC GetAllCurrentCategories";
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



    }
}