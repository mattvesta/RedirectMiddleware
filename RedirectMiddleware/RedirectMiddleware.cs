using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RedirectMiddleware.Models;

namespace RedirectMiddleware;

public class RedirectMiddleware 
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;
    private readonly ILogger<RedirectMiddleware> _logger;
    private readonly ApplicationConfiguration _config;

    public RedirectMiddleware(RequestDelegate next, IMemoryCache cache, ILogger<RedirectMiddleware> logger, IOptions<ApplicationConfiguration> config)
    {
        _next = next;
        _cache = cache;
        _logger = logger;
        _config = config.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        //Check if the redirect data is in cache
        //TODO: move this to separate service
        if (!_cache.TryGetValue("RedirectData", out List<RedirectRule>? redirectRules))
        {
            try
            {
                var mockResponse = "[\n\n    {\n    \n       \"redirectUrl\": \"/campaignA\",\n    \n       \"targetUrl\": \"/campaigns/targetcampaign\",\n    \n       \"redirectType\": 302,\n    \n       \"useRelative\": false\n    \n    },\n    \n    {\n    \n       \"redirectUrl\": \"/campaignB\",\n    \n       \"targetUrl\": \"/campaigns/targetcampaign/channelB\",\n    \n       \"redirectType\": 302,\n    \n       \"useRelative\": false\n    \n    },\n    \n    {\n    \n       \"redirectUrl\": \"/product-directory\",\n    \n       \"targetUrl\": \"/products\",\n    \n       \"redirectType\": 301,\n    \n       \"useRelative\": true\n    \n    }\n    \n]";
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                redirectRules = JsonSerializer.Deserialize<List<RedirectRule>>(mockResponse, options);
                //Cache the data with a configurable duration
                _cache.Set("RedirectData", redirectRules, TimeSpan.FromMinutes(_config.RefreshMinutes));
                _logger.LogInformation("Cache refresh successfully");
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "API Call Failed!");
            }
        }

        try
        {
            var requestUrl = context.Request.Path.ToString();
            //Separate out URL components
            var url = requestUrl.Split('/');
            var urlCount = url.Length;

            var found = redirectRules?.Find(e => e.RedirectUrl.ToUpper() == $"/{url[1].ToUpper()}");

            if (found != null)
            {
                var redirectUrl = found.TargetUrl;

                if (found.UseRelative)
                {
                    for (var i = 2; i < urlCount; i++)
                    {
                        redirectUrl = redirectUrl + $"/{url[i]}";
                    }
                }

                var redirectType = found.RedirectType switch
                {
                    301 => true,
                    302 => false,
                    307 => false,
                    308 => false,
                    _ => false
                };

                context.Response.Redirect(redirectUrl, redirectType);
                _logger.LogInformation("Successful Redirect from {RequestUrl} to {RedirectUrl}", requestUrl, redirectUrl);
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Redirect failed");
        }
        await _next(context);
    }
}