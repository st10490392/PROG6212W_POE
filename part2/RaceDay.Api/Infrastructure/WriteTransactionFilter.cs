using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;

namespace RaceDay.Api.Infrastructure;

// Keep ownership/existence checks and writes in one serializable transaction.
// In particular, an enrolment cannot slip between an event's delete check and delete.
public sealed class WriteTransactionFilter(RaceDayDbContext db) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (HttpMethods.IsGet(context.HttpContext.Request.Method)) { await next(); return; }
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var executed = await next();
        var status = (executed.Result as ObjectResult)?.StatusCode ?? context.HttpContext.Response.StatusCode;
        if (executed.Exception is null && status < 400) await transaction.CommitAsync();
        // Disposal rolls back unsuccessful requests; the context is also scoped to this request.
    }
}
