using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace Vaulted.Auth;

// Stored procedures report "not found" / "not yours" / "already exists" with THROW 504xx;
// this turns those into the matching HTTP status with the procedure's message.
public static class SqlErrors
{
    public static IActionResult? ToResult(Exception ex)
    {
        if (ex is not SqlException sql) return null;
        return sql.Number switch
        {
            50403 => new ObjectResult(sql.Message) { StatusCode = StatusCodes.Status403Forbidden },
            50404 => new NotFoundObjectResult(sql.Message),
            50409 => new ConflictObjectResult(sql.Message),
            _ => null
        };
    }
}
