// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.AspNetCore.Components.Build
{
    public class PrintCoolMessage : Task
    {
        public override bool Execute()
        {
            Log.LogMessage(MessageImportance.High, "*** YES IT IS WORKING ***");
            return true;
        }
    }
}
