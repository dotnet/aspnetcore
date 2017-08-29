// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    internal class DefaultProjectSnapshotListener : ProjectSnapshotListener
    {
        public override event EventHandler<ProjectChangeEventArgs> ProjectChanged;

        internal void Notify(ProjectChangeEventArgs e)
        {
            var handler = ProjectChanged;
            if (handler != null)
            {
                handler(this, e);
            }
        }
    }
}
