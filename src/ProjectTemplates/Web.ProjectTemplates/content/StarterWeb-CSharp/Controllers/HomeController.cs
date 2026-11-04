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
#if (GenerateApi)
public class HomeController(IDownstreamApi downstreamApi) : Controller
#elseif (GenerateGraph)
public class HomeController(GraphServiceClient graphServiceClient) : Controller
#else
public class HomeController : Controller
#endif
{
#if (GenerateApi)
    [AuthorizeForScopes(ScopeKeySection = "DownstreamApi:Scopes")]
    public async Task<IActionResult> Index()
    {
        using var response = await downstreamApi.CallApiForUserAsync("DownstreamApi").ConfigureAwait(false);
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
    [AuthorizeForScopes(ScopeKeySection = "DownstreamApi:Scopes")]
    public async Task<IActionResult> Index()
    {
        var user = await graphServiceClient.Me.GetAsync();
        ViewData["ApiResult"] = user?.DisplayName;

        return View();
    }
#else
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
