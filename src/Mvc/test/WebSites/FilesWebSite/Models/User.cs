// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace FilesWebSite.Models;

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
