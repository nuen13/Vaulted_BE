using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.Extensions.FileProviders;
using Vaulted.Models;
using Vaulted.Auth;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;

        options.JsonSerializerOptions.ReferenceHandler =
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });


// Only the sites listed in Cors:AllowedOrigins may call the API from a browser
// (e.g. env var Cors__AllowedOrigins__0=https://vaulted.example.com when hosted).
// Locally the Vite dev server proxies /api, so the browser sees one origin and needs no entry.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? Array.Empty<string>();

builder.Services.AddCors(options =>
    options.AddPolicy("Frontend", policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyMethod()
        .AllowAnyHeader()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ApplicationContext>(options =>
    options.UseSqlServer(connectionString)
);

// Google sign-in + allowlist; every endpoint requires a signed-in user (see Auth/AuthSetup.cs)
builder.Services.AddVaultedAuth(builder.Configuration);

// Compress the frontend bundle (Cloud Run gives only 1 GB of free outbound data a month).
// Only static file types: API JSON can carry the sign-in token, so it stays uncompressed (BREACH).
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.MimeTypes = new[] { "text/html", "text/css", "application/javascript", "text/javascript", "image/svg+xml" };
});


var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

else
{
    // Sign-in tokens must only travel over HTTPS once hosted.
    // Behind a host that ends HTTPS for you (most PaaS), set ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
    // so the app sees the original https request and doesn't redirect in a loop.
    app.UseHsts();
    app.UseHttpsRedirection();
}

// Order matters: CORS before auth (so a 401 still carries CORS headers), auth before controllers.
// When hosted, the built frontend sits in wwwroot: "/" serves index.html (static files skip auth).
app.UseResponseCompression();
app.UseDefaultFiles();
app.UseStaticFiles(new StaticFileOptions
{
    // Vite puts a content hash in every /assets file name, so browsers can keep them for good.
    OnPrepareResponse = ctx =>
    {
        if (ctx.Context.Request.Path.StartsWithSegments("/assets"))
            ctx.Context.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
    }
});
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();




app.Run();

