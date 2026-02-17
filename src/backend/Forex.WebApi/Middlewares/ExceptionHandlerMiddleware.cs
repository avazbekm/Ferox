namespace Forex.WebApi.Middlewares;

using Forex.Application.Common.Exceptions;
using Forex.WebApi.Models;

public class ExceptionHandlerMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (AppException ex)
        {
            context.Response.StatusCode = (int)ex.StatusCode;

            await context.Response.WriteAsJsonAsync(new Response
            {
                StatusCode = (int)ex.StatusCode,
                Message = ex.Message,
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine("========== XATOLIK TAFSILOTI ==========");
            Console.WriteLine($"Xato turi: {ex.GetType().Name}");
            Console.WriteLine($"Xabar: {ex.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            Console.WriteLine("=======================================");

            context.Response.StatusCode = 500;
            await context.Response.WriteAsJsonAsync(new
            {
                context.Response.StatusCode,
                ex.Message
            });
        }
    }
}
