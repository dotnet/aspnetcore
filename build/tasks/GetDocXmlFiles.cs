// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Build.Framework;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RepoTasks
{
    /// <summary>
    /// Filters a list of .xml files to only those that are .NET Xml docs files
    /// </summary>
    public class GetDocXmlFiles : Microsoft.Build.Utilities.Task
    {
        [Required]
        public ITaskItem[] Files { get; set; }

        [Output]
        public ITaskItem[] XmlDocFiles { get; set; }

        public override bool Execute()
        {
            var xmlDocs = new ConcurrentBag<ITaskItem>();
            Parallel.ForEach(Files, f =>
            {
                try
                {
                    using (var file = File.OpenRead(f.ItemSpec))
                    using (var reader = new StreamReader(file))
                    {
                        string line;
                        for (var i = 0; i < 2; i++)
                        {
                            line = reader.ReadLine();
                            if (i == 0 && line.StartsWith("<?xml", StringComparison.Ordinal))
                            {
                                line = line.Substring(line.IndexOf("?>") + 2);
                            }

                            if (line.StartsWith("<doc>", StringComparison.OrdinalIgnoreCase) || line.StartsWith("<doc xml:", StringComparison.OrdinalIgnoreCase))
                            {
                                xmlDocs.Add(f);
                                return;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.LogMessage(MessageImportance.Normal, $"Failed to read {f.ItemSpec}: {ex.ToString()}");
                }

                Log.LogMessage($"Did not detect {f.ItemSpec} as an xml doc file");
            });

            XmlDocFiles = xmlDocs.ToArray();
            Log.LogMessage($"Found {XmlDocFiles.Length} xml doc file(s)");
            return true;
        }
    }
}
