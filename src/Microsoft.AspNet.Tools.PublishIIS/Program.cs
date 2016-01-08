// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Extensions.CommandLineUtils;

namespace Microsoft.AspNet.Tools.PublishIIS
{
    public class Program
    {
        public static int Main(string[] args)
        {
            var app = new CommandLineApplication
            {
                // TODO: This needs to be updated once we know how this is going to be called
                Name = "dotnet(????) publish-iis",
                FullName = "Asp.Net IIS Publisher",
                Description = "IIS Publisher for the Asp.Net web applications",
            };
            app.HelpOption("-h|--help");

            var publishFolderOption = app.Option("--publish-folder", "The path to the publish output folder", CommandOptionType.SingleValue);
            var appNameOption = app.Option("--application-name", "The name of the application", CommandOptionType.SingleValue);

            app.OnExecute(() =>
            {
                var publishFolder = publishFolderOption.Value();
                var appName = appNameOption.Value();

                if (publishFolder == null || appName == null)
                {
                    app.ShowHelp();
                    return 2;
                }

                XDocument webConfigXml = null;
                var webConfigPath = Path.Combine(publishFolder, "wwwroot", "web.config");
                if (File.Exists(webConfigPath))
                {
                    try
                    {
                        webConfigXml = XDocument.Load(webConfigPath);
                    }
                    catch (XmlException) { }
                }

                var transformedConfig = WebConfigTransform.Transform(webConfigXml, appName);

                using (var f = new FileStream(webConfigPath, FileMode.Create))
                {
                    transformedConfig.Save(f);
                }

                return 0;
            });

            try
            {
                return app.Execute(args);
            }
            catch (Exception e)
            {
#if DEBUG
                Console.Error.WriteLine(e);
#else
                Console.Error.WriteLine(e.Message);
#endif
            }

            return 1;
        }
    }
}
