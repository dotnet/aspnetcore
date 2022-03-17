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
#endif

namespace Company.WebApplication1.Pages;

#if (GenerateApiOrGraph)
[AuthorizeForScopes(ScopeKeySection = "DownstreamApi:Scopes")]
#endif
public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;

#if (GenerateApi)
    private readonly IDownstreamWebApi _downstreamWebApi;

    public IndexModel(ILogger<IndexModel> logger,
                        IDownstreamWebApi downstreamWebApi)
    {
            _logger = logger;
        _downstreamWebApi = downstreamWebApi;
    }

    public async Task OnGet()
    {
        using var response = await _downstreamWebApi.CallWebApiForUserAsync("DownstreamApi").ConfigureAwait(false);
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
    private readonly GraphServiceClient _graphServiceClient;

    public IndexModel(ILogger<IndexModel> logger,
                        GraphServiceClient graphServiceClient)
    {
        _logger = logger;
        _graphServiceClient = graphServiceClient;
    }

    public async Task OnGet()
    {
        var user = await _graphServiceClient.Me.Request().GetAsync();

        ViewData["ApiResult"] = user.DisplayName;
    }
#else
    public IndexModel(ILogger<IndexModel> logger)
    {
        _logger = logger;
    }

    public void OnGet()
    {

    }
#endif
}
