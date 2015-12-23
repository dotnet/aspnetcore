// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.ServiceProcess;

namespace Microsoft.AspNet.Hosting.WindowsServices
{
    /// <summary>
    ///     Extensions to <see cref="IWebApplication" for hosting inside a Windows service. />
    /// </summary>
    public static class WebApplicationExtensions
    {
        /// <summary>
        ///     Runs the specified web application inside a Windows service and blocks until the service is stopped.
        /// </summary>
        /// <param name="application">An instance of the <see cref="IWebApplication"/> to host in the Windows service.</param>
        /// <example>
        ///     This example shows how to use <see cref="WebApplicationService.Run"/>.
        ///     <code>
        ///         public class Program
        ///         {
        ///             public static void Main(string[] args)
        ///             {
        ///                 var config = WebApplicationConfiguration.GetDefault(args);
        ///                 
        ///                 var application = new WebApplicationBuilder()
        ///                     .UseConfiguration(config)
        ///                     .Build();
        ///          
        ///                 // This call will block until the service is stopped.
        ///                 application.RunAsService();
        ///             }
        ///         }
        ///     </code>
        /// </example>
        public static void RunAsService(this IWebApplication application)
        {
            var webApplicationService = new WebApplicationService(application);
            ServiceBase.Run(webApplicationService);
        }
    }
}
