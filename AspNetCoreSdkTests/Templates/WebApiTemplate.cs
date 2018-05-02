using System.Collections.Generic;
using System.Linq;

namespace AspNetCoreSdkTests.Templates
{
    public class WebApiTemplate : WebTemplate
    {
        public WebApiTemplate() { }

        public override string Name => "webapi";

        public override string RelativeUrl => "/api/values";

        public override IEnumerable<string> ExpectedFilesAfterPublish => Enumerable.Concat(base.ExpectedFilesAfterPublish, new[]
        {
            "appsettings.Development.json",
            "appsettings.json",
        });
    }
}
