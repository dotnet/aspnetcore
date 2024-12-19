#if (GenerateApiOrGraph)
using System.Net;
#endif
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
#if (GenerateGraph)
using Microsoft.Graph;
#endif
#if (GenerateApiOrGraph)
using Microsoft.Identity.Web;
using Microsoft.Identity.Abstractions;
#endif

namespace Company.WebApplication1.Pages;

#if (GenerateApiOrGraph)
[AuthorizeForScopes(ScopeKeySection = "DownstreamApi:Scopes")]
#endif
#if (GenerateApi)
public class IndexModel(ILogger<IndexModel> logger, IDownstreamApi downstreamApi) : Controller
# elseif (GenerateGraph)
public class IndexModel(ILogger<IndexModel> logger, GraphServiceClient graphServiceClient) : ControllerBase
#else
public class IndexModel(ILogger<IndexModel> logger) : PageModel
#endif
{
#if (GenerateApi)
    public async Task OnGet()
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
    }
#elseif (GenerateGraph)
    public async Task OnGet()
    {
        var user = await graphServiceClient.Me.GetAsync();

        ViewData["ApiResult"] = user?.DisplayName;
    }
#else
    public void OnGet()
    {

    }
#endif
}
