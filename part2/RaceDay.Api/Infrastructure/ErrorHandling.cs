using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Services;

namespace RaceDay.Api.Infrastructure;

public sealed class ErrorHandling(IProblemDetailsService problems, ILogger<ErrorHandling> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception error, CancellationToken cancellation)
    {
        var status = error is ApiException api ? api.Status : 500;
        var title = error is ApiException known ? known.Message : "An unexpected server error occurred.";
        if (error is DbUpdateException update)
        {
            // Unique/check/FK races have the same safe response as a preflight business-rule conflict.
            var constraint = update.InnerException is SqlException sql && sql.Number is 2601 or 2627 or 547;
            var sqlite = update.InnerException?.GetType().FullName == "Microsoft.Data.Sqlite.SqliteException"
                && (int?)update.InnerException.GetType().GetProperty("SqliteErrorCode")?.GetValue(update.InnerException) == 19;
            if (constraint || sqlite) { status = 409; title = "The operation conflicts with existing data or a database constraint."; }
        }
        if (status == 500) logger.LogError(error, "Unhandled API failure. Trace: {TraceId}", context.TraceIdentifier);
        context.Response.StatusCode = status;
        return await problems.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = new() { Status = status, Title = title, Extensions = { ["traceId"] = context.TraceIdentifier } }
        });
    }
}
