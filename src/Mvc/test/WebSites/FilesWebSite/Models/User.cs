// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace FilesWebSite.Models
{
    public class User
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public IFormFile Biography { get; set; }

        public async Task<string> ReadBiography()
        {
            if (Biography != null)
            {
                using (var reader = new StreamReader(Biography.OpenReadStream()))
                {
                    return await reader.ReadToEndAsync();
                }
            }

            return null;
        }
    }
}
