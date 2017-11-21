// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.SecretManager.TestExtension
{
    [Guid("6afffd63-17b6-4ef2-b515-fee22d767631")]
    public class SecretManagerTestWindow : ToolWindowPane
    {
        public SecretManagerTestWindow()
            : base(null)
        {
            this.Caption = "SecretManager Test Window";
            this.Content = new SecretManagerTestControl();
        }

        protected override void Initialize()
        {
            base.Initialize();

            var component = (IComponentModel)GetService(typeof(SComponentModel));
            var projectService = component.GetService<IProjectServiceAccessor>().GetProjectService();
            ((SecretManagerTestControl)Content).DataContext = new SecretManagerViewModel(projectService);
        }
    }
}
