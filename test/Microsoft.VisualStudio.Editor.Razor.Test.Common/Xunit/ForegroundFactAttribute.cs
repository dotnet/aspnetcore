// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit.Sdk;

namespace Xunit
{
    // Similar to WpfFactAttribute https://github.com/xunit/samples.xunit/blob/969d9f7e887836f01a6c525324bf3db55658c28f/STAExamples/WpfFactAttribute.cs
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    [XunitTestCaseDiscoverer("Xunit.ForegroundFactDiscoverer", "Microsoft.VisualStudio.LanguageServices.Razor.Test")]
    internal class ForegroundFactAttribute : FactAttribute
    {
    }
}
