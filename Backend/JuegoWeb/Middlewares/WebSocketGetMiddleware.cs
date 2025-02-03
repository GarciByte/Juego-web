namespace JuegoWeb.Middlewares;

public class WebSocketGetMiddleware
{
    private readonly RequestDelegate _next;

    public WebSocketGetMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/socket") && context.Request.Method == HttpMethods.Connect)
        {
            // Convertir el método a GET
            context.Request.Method = HttpMethods.Get;
        }

        await _next(context);
    }
}