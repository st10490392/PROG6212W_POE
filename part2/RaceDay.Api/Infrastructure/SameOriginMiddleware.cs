namespace RaceDay.Api.Infrastructure;

public sealed class SameOriginMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var request = context.Request;
        if (!HttpMethods.IsGet(request.Method) && !HttpMethods.IsHead(request.Method) && !HttpMethods.IsOptions(request.Method))
        {
            var origin = request.Headers.Origin.ToString();
            var expected = $"{request.Scheme}://{request.Host}";
            if (request.Headers["Sec-Fetch-Site"] == "cross-site" || (origin.Length > 0 && origin != expected))
            {
                context.Response.StatusCode = 403;
                await context.Response.WriteAsJsonAsync(new { title = "Cross-origin writes are not allowed.", status = 403 });
                return;
            }
        }
        await next(context);
    }
}
