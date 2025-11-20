using Microsoft.Extensions.FileProviders;

namespace PvZOL.Server;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddControllers();

        builder.Services.AddSingleton<PvzSocketHost>();
        builder.Services.AddSingleton<IHostedService>(p => p.GetRequiredService<PvzSocketHost>());

        var app = builder.Build();
        app.UseRouting();
        app.UseAuthorization();
        app.MapStaticAssets();
        app.MapControllers();
        app.UseWebSockets(new WebSocketOptions
        {
            KeepAliveInterval = TimeSpan.FromSeconds(15),
            KeepAliveTimeout = TimeSpan.FromSeconds(15)
        });
        
        var pvzStaticFiles = new StaticFileOptions
        {
            ServeUnknownFileTypes = true,
            FileProvider = new PvzFileProvider(new PhysicalFileProvider(app.Configuration["GameData"]!)),
            RequestPath = "",
            
            DefaultContentType = "application/octet-stream",
        };
        app.UseStaticFiles(pvzStaticFiles);

        app.Run();
    }
}