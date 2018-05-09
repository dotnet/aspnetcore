using System.Collections.Generic;
using System.Linq;

namespace AspNetCoreSdkTests.Templates
{
    public class WebApiTemplate : WebTemplate
    {
        public WebApiTemplate() { }

        public override string Name => "webapi";

        public override string RelativeUrl => "/api/values";

        public override IEnumerable<string> ExpectedFilesAfterPublish =>
            base.ExpectedFilesAfterPublish
            .Concat(new[]
            {
                "appsettings.Development.json",
                "appsettings.json",
            });
    }
}
