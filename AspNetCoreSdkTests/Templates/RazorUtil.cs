using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AspNetCoreSdkTests.Templates
{
    public static class RazorUtil
    {
        public static IEnumerable<string> GetExpectedObjFilesAfterBuild(Template template) => new[]
         {
            $"{template.Name}.RazorAssemblyInfo.cache",
            $"{template.Name}.RazorAssemblyInfo.cs",
            $"{template.Name}.RazorCoreGenerate.cache",
            $"{template.Name}.RazorTargetAssemblyInfo.cache",
            $"{template.Name}.RazorTargetAssemblyInfo.cs",
            $"{template.Name}.TagHelpers.input.cache",
            $"{template.Name}.TagHelpers.output.cache",
            $"{template.Name}.Views.dll",
            $"{template.Name}.Views.pdb",
        }.Select(p => Path.Combine(template.OutputPath, p));

        public static IEnumerable<string> GetExpectedBinFilesAfterBuild(Template template) => new[]
        {
            $"{template.Name}.Views.dll",
            $"{template.Name}.Views.pdb",
        }.Select(p => Path.Combine(template.OutputPath, p));

        public static IEnumerable<string> GetExpectedFilesAfterPublish(Template template) => new[]
        {
            $"{template.Name}.Views.dll",
            $"{template.Name}.Views.pdb",
        };
    }
}
