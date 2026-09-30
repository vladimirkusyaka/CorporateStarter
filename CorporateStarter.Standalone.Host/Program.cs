var builder = WebApplication.CreateBuilder(args);
builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(error => error.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsync("The service is temporarily unavailable.");
    }));
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.MapReverseProxy();
// API errors must never fall through to the SPA HTML page, including unknown API routes.
app.Map("/api/{**path}", () => Results.NotFound()).WithOrder(1000);
app.MapFallbackToFile("index.html");
app.Run();
