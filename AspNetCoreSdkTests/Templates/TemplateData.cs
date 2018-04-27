using System.Collections.Generic;

namespace AspNetCoreSdkTests.Templates
{
    public static class TemplateData
    {
        public static IEnumerable<Template> All { get; } = new Template[]
        {
            new ClassLibraryTemplate(),
            new ConsoleApplicationTemplate(),
            new WebTemplate(),
            new RazorTemplate(),
            new MvcTemplate(),
        };
    }
}
