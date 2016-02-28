// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using FilesWebSite.Models;
using Microsoft.AspNetCore.Mvc;

namespace FilesWebSite.Controllers
{
    public class UploadFilesController : Controller
    {
        [HttpPost("UploadFiles")]
        public async Task<IActionResult> Post(User user)
        {
            var resultUser = new
            {
                Name = user.Name,
                Age = user.Age,
                Biography = await user.ReadBiography()
            };

            return Json(resultUser);
        }
    }
}
