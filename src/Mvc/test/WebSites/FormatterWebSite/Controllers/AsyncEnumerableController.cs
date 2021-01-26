// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace FormatterWebSite.Controllers
{
    [ApiController]
    [Route("{controller}/{action}")]
    public class AsyncEnumerableController : ControllerBase
    {
        [HttpGet]
        public IAsyncEnumerable<Project> GetAllProjects()
            => GetAllProjectsCore();

        [HttpGet]
        public async Task<IAsyncEnumerable<Project>> GetAllProjectsAsTask()
        {
            await Task.Yield();
            return GetAllProjectsCore();
        }

        [HttpGet]
        public IAsyncEnumerable<Project> GetAllProjectsWithError()
            => GetAllProjectsCore(true);

        public async IAsyncEnumerable<Project> GetAllProjectsCore(bool throwError = false)
        {
            await Task.Delay(5);
            for (var i = 0; i < 10; i++)
            {
                if (throwError && i == 5)
                {
                    throw new InvalidTimeZoneException();
                }

                yield return new Project
                {
                    Id = i,
                    Name = $"Project{i}",
                };
            }
        }
    }
}
