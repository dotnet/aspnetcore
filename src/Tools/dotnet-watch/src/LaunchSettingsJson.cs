// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.DotNet.Watcher.Tools
{
    public class LaunchSettingsJson
    {
        public Dictionary<string, LaunchSettingsProfile> Profiles { get; set; }
    }

    public class LaunchSettingsProfile
    {
        public string CommandName { get; set; }

        public bool LaunchBrowser { get; set; }

        public string LaunchUrl { get; set; }
    }
}
