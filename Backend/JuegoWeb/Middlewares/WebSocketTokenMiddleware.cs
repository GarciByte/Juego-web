namespace JuegoWeb.Middlewares;

public class WebSocketTokenMiddleware
{
    private readonly RequestDelegate _next;

    public WebSocketTokenMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/socket"))
        {
            // Obtiene el token
            var token = context.Request.Query["token"].ToString();

            if (!string.IsNullOrWhiteSpace(token))
            {
                // Se agrega al encabezado de autorización
                context.Request.Headers["Authorization"] = $"Bearer {token}";
            }
        }

        await _next(context);
    }
}