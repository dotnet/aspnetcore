// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Cli.FunctionalTests.Templates
{
    public static class RazorUtil
    {
        public static IEnumerable<string> GetExpectedObjFilesAfterBuild(Template template) => new[]
         {
            // Added between 2.1.300-rc1 and 2.1.300-rtm (https://github.com/aspnet/Razor/pull/2316)
            $"{template.Name}.csproj.CopyComplete",
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
