using RedirectMiddleware.Models;

var builder = WebApplication.CreateBuilder(args);


//Import Config
var pathToContentRoot = string.Join('\\',
    System.Reflection.Assembly.GetExecutingAssembly().Location.Split('\\')
        .SkipLast(1)
        .ToArray()) + "\\";
var configuration = new ConfigurationBuilder()
    .SetBasePath(pathToContentRoot)
    .AddJsonFile("appsettings.json", false)
    .Build();
builder.Services.Configure<ApplicationConfiguration>(configuration.GetSection("Application"));
// Add services to the container.
builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseMiddleware<RedirectMiddleware.RedirectMiddleware>();
//app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run(context => context.Response.WriteAsync($"Redirected Url: " + $"{context.Request.Path + context.Request.QueryString}") );
app.Run();