// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net.Http;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace RepoTasks
{
    public class DownloadFile : Task
    {
        [Required]
        public string Source { get; set; }

        [Required]
        public string Destination { get; set; }

        public override bool Execute()
        {
            ExecuteAsync().GetAwaiter().GetResult();
            return true;
        }

        private async System.Threading.Tasks.Task ExecuteAsync()
        {
            using (var client = new HttpClient())
            {
                using (var stream = await client.GetStreamAsync(Source))
                using (var output = File.OpenWrite(Destination))
                {
                    await stream.CopyToAsync(output);
                }
            }
        }
    }
}
