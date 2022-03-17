// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration.Test;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.Extensions.Configuration.KeyPerFile.Test;

public class ConfigurationProviderCommandLineTest : ConfigurationProviderTestBase
{
    protected override (IConfigurationProvider Provider, Action Initializer) LoadThroughProvider(
        TestSection testConfig)
    {
        var testFiles = new List<IFileInfo>();
        SectionToTestFiles(testFiles, "", testConfig);

        var provider = new KeyPerFileConfigurationProvider(
            new KeyPerFileConfigurationSource
            {
                Optional = true,
                FileProvider = new TestFileProvider(testFiles.ToArray())
            });

        return (provider, () => { });
    }

    private void SectionToTestFiles(List<IFileInfo> testFiles, string sectionName, TestSection section)
    {
        foreach (var tuple in section.Values.SelectMany(e => e.Value.Expand(e.Key)))
        {
            testFiles.Add(new TestFile(sectionName + tuple.Key, tuple.Value));
        }

        foreach (var tuple in section.Sections)
        {
            SectionToTestFiles(testFiles, sectionName + tuple.Key + "__", tuple.Section);
        }
    }
}
