using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Vaulted.Models;

namespace Vaulted.Controllers
{
    // Statuses (Planning, Consuming...): list, add, rename, delete. Works like categories.
    [Route("api/[controller]")]
    [ApiController]
    public class StatusController : ControllerBase
    {
        private readonly ApplicationContext _context;
        private const int MaxNameLength = 50;

        public StatusController(ApplicationContext context)
        {
            _context = context;
        }

        private Task<List<StatusDTO>> AllStatusesAsync() =>
            _context.Database.SqlQueryRaw<StatusDTO>("EXEC GetAllStatuses").ToListAsync();


        // ------------ GET ------------ //
        // Returns { id, name, sortOrder, mediaCount } in display order
        [HttpGet("get-all-statuses")]
        public async Task<IActionResult> GetAllStatuses()
        {
            try
            {
                return Ok(await AllStatusesAsync());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching statuses: {ex.Message}");
                return StatusCode(500, "An error occurred while fetching statuses.");
            }
        }


        // ------------ POST ------------ //
        [HttpPost("add-status")]
        public async Task<IActionResult> AddStatus([FromBody] SaveStatusDTO dto)
        {
            try
            {
                var name = dto.Name?.Trim();
                var error = Validate(name, await AllStatusesAsync(), excludeId: null);
                if (error != null) return error;

                var res = await _context.Database.SqlQueryRaw<StatusDTO>(
                    "EXEC AddStatus @Name", new SqlParameter("@Name", name)).ToListAsync();
                return Ok(res.FirstOrDefault());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding status: {ex.Message}");
                return StatusCode(500, "An error occurred while adding the status.");
            }
        }


        // ------------ PUT ------------ //
        // Rename; items using the old name are renamed too
        [HttpPut("update-status/{id}")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] SaveStatusDTO dto)
        {
            try
            {
                var name = dto.Name?.Trim();
                var error = Validate(name, await AllStatusesAsync(), excludeId: id);
                if (error != null) return error;

                var res = await _context.Database.SqlQueryRaw<StatusDTO>(
                    "EXEC UpdateStatusById @StatusId, @Name",
                    new SqlParameter("@StatusId", id),
                    new SqlParameter("@Name", name)).ToListAsync();
                var updated = res.FirstOrDefault();
                if (updated == null) return NotFound("Status not found.");
                return Ok(updated);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating status: {ex.Message}");
                return StatusCode(500, "An error occurred while updating the status.");
            }
        }


        // ------------ DELETE ------------ //
        // A status still in use is refused with 409 { itemCount } unless ?reassignTo= names
        // another status to move those items to first.
        [HttpDelete("delete-status/{id}")]
        public async Task<IActionResult> DeleteStatus(int id, [FromQuery] string? reassignTo)
        {
            try
            {
                var statuses = await AllStatusesAsync();
                var status = statuses.FirstOrDefault(s => s.Id == id);
                if (status == null) return NotFound("Status not found.");

                if (reassignTo != null)
                {
                    var target = statuses.FirstOrDefault(s => string.Equals(s.Name, reassignTo, StringComparison.OrdinalIgnoreCase));
                    if (target == null) return BadRequest("The status to move items to doesn't exist.");
                    if (target.Id == id) return BadRequest("Pick a different status to move the items to.");
                    reassignTo = target.Name;
                }

                var res = await _context.Database.SqlQueryRaw<StatusDeleteResultDTO>(
                    "EXEC DeleteStatusById @StatusId, @ReassignTo",
                    new SqlParameter("@StatusId", id),
                    new SqlParameter("@ReassignTo", (object?)reassignTo ?? DBNull.Value)).ToListAsync();

                var result = res.FirstOrDefault();
                if (result == null) return NotFound("Status not found.");
                if (!result.Deleted)
                {
                    return Conflict(new
                    {
                        message = $"{result.ItemCount} media item(s) still have this status. Move them to another status first.",
                        itemCount = result.ItemCount
                    });
                }
                return Ok(new { id, name = status.Name, movedItems = result.ItemCount, reassignTo });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting status: {ex.Message}");
                return StatusCode(500, "An error occurred while deleting the status.");
            }
        }


        // Name required, at most 50 characters, unique (ignoring case)
        private IActionResult? Validate(string? name, List<StatusDTO> existing, int? excludeId)
        {
            if (string.IsNullOrEmpty(name))
                return BadRequest("Status name is required.");
            if (name.Length > MaxNameLength)
                return BadRequest($"Status name must be at most {MaxNameLength} characters.");
            if (existing.Any(s => s.Id != excludeId && string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase)))
                return Conflict(new { message = $"A status named \"{name}\" already exists." });
            return null;
        }
    }
}
