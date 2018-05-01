using System.Collections.Generic;
using System.Linq;

namespace AspNetCoreSdkTests.Templates
{
    public class WebApiTemplate : WebTemplate
    {
        public new static WebApiTemplate Instance { get; } = new WebApiTemplate();

        protected WebApiTemplate() { }

        public override string Name => "webapi";

        public override string RelativeUrl => "/api/values";

        public override IEnumerable<string> ExpectedFilesAfterPublish => Enumerable.Concat(base.ExpectedFilesAfterPublish, new[]
        {
            "appsettings.Development.json",
            "appsettings.json",
        });
    }
}
