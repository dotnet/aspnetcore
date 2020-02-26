// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using TestSite;

internal class StartupHook
{
    public static void Initialize()
    {
        Startup.StartupHookCalled = true;
    }
}
