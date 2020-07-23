using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
#if (GenerateApiOrGraph)
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Web;
using System.Net;
using System.Net.Http;
#endif
#if (GenerateGraph)
using Microsoft.Graph;
#endif
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace Company.WebApplication1.Pages
{
#if (GenerateApiOrGraph)
    [AuthorizeForScopes(ScopeKeySection = "CalledApi:CalledApiScopes")]
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
            ViewData["ApiResult"] = await _downstreamWebApi.CallWebApiAsync();
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
}
