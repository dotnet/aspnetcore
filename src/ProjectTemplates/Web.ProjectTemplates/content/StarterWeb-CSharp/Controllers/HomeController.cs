using System.Diagnostics;
#if (GenerateApiOrGraph)
using System.Net;
#endif
#if (OrganizationalAuth)
using Microsoft.AspNetCore.Authorization;
#endif
using Microsoft.AspNetCore.Mvc;
#if (GenerateGraph)
using Microsoft.Graph;
#endif
#if (GenerateApiOrGraph)
using Microsoft.Identity.Web;
using Microsoft.Identity.Abstractions;
#endif
using Company.WebApplication1.Models;

namespace Company.WebApplication1.Controllers;

#if (OrganizationalAuth)
[Authorize]
#endif
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

#if (GenerateApi)
    private readonly IDownstreamApi _downstreamApi;

    public HomeController(ILogger<HomeController> logger,
                            IDownstreamApi downstreamApi)
    {
            _logger = logger;
        _downstreamApi = downstreamApi;
    }

    [AuthorizeForScopes(ScopeKeySection = "DownstreamApi:Scopes")]
    public async Task<IActionResult> Index()
    {
        using var response = await _downstreamApi.CallApiForUserAsync("DownstreamApi").ConfigureAwait(false);
        if (response.StatusCode == System.Net.HttpStatusCode.OK)
        {
            var apiResult = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            ViewData["ApiResult"] = apiResult;
        }
        else
        {
            var error = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            throw new HttpRequestException($"Invalid status code in the HttpResponseMessage: {response.StatusCode}: {error}");
        }
        return View();
    }
#elseif (GenerateGraph)
    private readonly GraphServiceClient _graphServiceClient;

    public HomeController(ILogger<HomeController> logger,
                        GraphServiceClient graphServiceClient)
    {
            _logger = logger;
        _graphServiceClient = graphServiceClient;
    }

    [AuthorizeForScopes(ScopeKeySection = "DownstreamApi:Scopes")]
    public async Task<IActionResult> Index()
    {
        var user = await _graphServiceClient.Me.GetAsync();
        ViewData["ApiResult"] = user?.DisplayName;

        return View();
    }
#else
    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }

#endif
    public IActionResult Privacy()
    {
        return View();
    }

#if (OrganizationalAuth)
    [AllowAnonymous]
#endif
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
