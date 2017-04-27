// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Cli.CommandLine;
//using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Archive;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Microsoft.DotNet.Tools.Archive
{

    public partial class ArchiveCommand
    {
        public static int Main(string[] args)
        {
            //DebugHelper.HandleDebugSwitch(ref args);

            var app = new CommandLineApplication();
            app.Name = "archive";
            app.FullName = ".NET archiver";
            app.Description = "Archives and expands sets of files";
            app.HelpOption("-h|--help");

            var extract = app.Option("-x|--extract <outputDirectory>", "Directory to extract to", CommandOptionType.SingleValue);
            var archiveFile = app.Option("-a|--archive <file>", "Archive to operate on", CommandOptionType.SingleValue);
            var externals = app.Option("--external <external>...", "External files and directories to consider for extraction", CommandOptionType.MultipleValue);
            var sources = app.Argument("<sources>...", "Files & directory to include in the archive", multipleValues:true);

            app.OnExecute(() => {

                if (extract.HasValue() && sources.Values.Any())
                {
                    Console.WriteLine("Extract '-x' can only be specified when no '<sources>' are specified to add to the archive.");
                    return 1;
                }
                else if (!extract.HasValue() && !sources.Values.Any())
                {
                    Console.WriteLine("Either extract '-x' or '<sources>' must be specified.");
                    return 1;
                }

                if (!archiveFile.HasValue())
                {
                    Console.WriteLine("Archive '-a' must be specified.");
                    return 1;
                }

                var progress = new ConsoleProgressReport();

                var archive = new IndexedArchive();
                foreach (var external in externals.Values)
                {
                    if (Directory.Exists(external))
                    {
                        archive.AddExternalDirectory(external);
                    }
                    else
                    {
                        archive.AddExternalFile(external);
                    }
                }

                if (sources.Values.Any())
                {
                    foreach(var source in sources.Values)
                    {
                        if (Directory.Exists(source))
                        {
                            archive.AddDirectory(source, progress);
                        }
                        else
                        {
                            archive.AddFile(source, Path.GetFileName(source));
                        }
                    }

                    archive.Save(archiveFile.Value(), progress);
                }
                else  // sources not specified, extract must have been specified
                {
                    archive.Extract(archiveFile.Value(), extract.Value(), progress);

                }

                return 0;
            });

            return app.Execute(args);
        }        
    }
}
