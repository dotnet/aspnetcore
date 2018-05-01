using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AspNetCoreSdkTests.Templates
{
    public abstract class RazorBaseTemplate : ClassLibraryTemplate
    {
       public override IEnumerable<string> ExpectedObjFilesAfterBuild => Enumerable.Concat(base.ExpectedObjFilesAfterBuild, new[]
        {
            $"{Name}.RazorAssemblyInfo.cache",
            $"{Name}.RazorAssemblyInfo.cs",
            $"{Name}.RazorCoreGenerate.cache",
            $"{Name}.RazorTargetAssemblyInfo.cache",
            $"{Name}.RazorTargetAssemblyInfo.cs",
            $"{Name}.TagHelpers.input.cache",
            $"{Name}.TagHelpers.output.cache",
            $"{Name}.Views.dll",
            $"{Name}.Views.pdb",
        }.Select(p => Path.Combine(OutputPath, p)));

        public override IEnumerable<string> ExpectedBinFilesAfterBuild => Enumerable.Concat(base.ExpectedBinFilesAfterBuild, new[]
        {
            $"{Name}.Views.dll",
            $"{Name}.Views.pdb",
        }.Select(p => Path.Combine(OutputPath, p)));

        public override IEnumerable<string> ExpectedFilesAfterPublish => Enumerable.Concat(base.ExpectedFilesAfterPublish, new[]
        {
            $"{Name}.Views.dll",
            $"{Name}.Views.pdb",
        });
    }
}
