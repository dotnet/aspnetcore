// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;

namespace MvcSample.Web.Components
{
    [ViewComponent(Name="Tags")]
    public class TagCloud : ViewComponent
    {
        private readonly string[] Tags =
            ("Lorem ipsum dolor sit amet consectetur adipisicing elit sed do eiusmod tempor incididunt ut labore et dolore magna aliqua" +
             "Ut enim ad minim veniam quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat Duis aute irure " +
             "dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur Excepteur sint occaecat cupidatat" +
             "non proident, sunt in culpa qui officia deserunt mollit anim id est laborum")
                .Split(new char[] {' '}, StringSplitOptions.RemoveEmptyEntries)
                .OrderBy(s => Guid.NewGuid().ToString())
                .ToArray();

        public async Task<IViewComponentResult> InvokeAsync(int count)
        {
            var tags = await GetTagsAsync(count);
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