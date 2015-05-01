// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;

namespace MvcSample.Web.Components
{
    [ViewComponent(Name = "Tags")]
    public class TagCloud : ViewComponent
    {
        private readonly string[] Tags =
            ("Lorem ipsum dolor sit amet consectetur adipisicing elit sed do eiusmod tempor incididunt ut labore et dolore magna aliqua" +
             "Ut enim ad minim veniam quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat Duis aute irure " +
             "dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur Excepteur sint occaecat cupidatat" +
             "non proident, sunt in culpa qui officia deserunt mollit anim id est laborum")
                .Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .OrderBy(s => Guid.NewGuid().ToString())
                .ToArray();

        public async Task<IViewComponentResult> InvokeAsync(int count, string returnAs)
        {
            var tags = await GetTagsAsync(count);

            if (returnAs.Equals("Content", StringComparison.OrdinalIgnoreCase))
            {
                return Content(string.Join(", ", tags));
            }
            if (returnAs.Equals("Json", StringComparison.OrdinalIgnoreCase))
            {
                return Json(tags);
            }

            return View(tags);
        }

        public IViewComponentResult Invoke(int count)
        {
            var tags = GetTags(count);
            return View(tags);
        }

        private Task<string[]> GetTagsAsync(int count)
        {
            return Task.FromResult(GetTags(count));
        }

        private string[] GetTags(int count)
        {
            @ViewBag.Title = count.ToString() + " Tags:";
            return Tags.Take(count).ToArray();
        }
    }
}