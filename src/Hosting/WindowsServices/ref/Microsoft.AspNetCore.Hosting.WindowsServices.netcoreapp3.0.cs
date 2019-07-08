// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Hosting.WindowsServices
{
    [System.ComponentModel.DesignerCategoryAttribute("Code")]
    public partial class WebHostService : System.ServiceProcess.ServiceBase
    {
        public WebHostService(Microsoft.AspNetCore.Hosting.IWebHost host) { }
        protected sealed override void OnStart(string[] args) { }
        protected virtual void OnStarted() { }
        protected virtual void OnStarting(string[] args) { }
        protected sealed override void OnStop() { }
        protected virtual void OnStopped() { }
        protected virtual void OnStopping() { }
    }
    public static partial class WebHostWindowsServiceExtensions
    {
        public static void RunAsService(this Microsoft.AspNetCore.Hosting.IWebHost host) { }
    }
}
