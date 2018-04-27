using System.Collections.Generic;

namespace AspNetCoreSdkTests.Templates
{
    public class WebTemplate : ConsoleApplicationTemplate
    {
        public override string Name => "web";
        public override TemplateType Type => TemplateType.Application;

        public override IEnumerable<string> ExpectedObjFilesAfterRestore => throw new System.NotImplementedException();

        public override IEnumerable<string> ExpectedObjFilesAfterBuild => throw new System.NotImplementedException();

        public override IEnumerable<string> ExpectedBinFilesAfterBuild => throw new System.NotImplementedException();
    }
}
