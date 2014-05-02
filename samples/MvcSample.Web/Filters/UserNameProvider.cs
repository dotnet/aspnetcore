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

using Microsoft.AspNet.Mvc;

namespace MvcSample.Web.Filters
{
    public class UserNameProvider : IActionFilter
    {
        private readonly UserNameService _nameService;

        public UserNameProvider(UserNameService nameService)
        {
            _nameService = nameService;
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            object originalUserName = null;

            context.ActionArguments.TryGetValue("userName", out originalUserName);

            var userName = originalUserName as string;

            if (string.IsNullOrWhiteSpace(userName))
            {
                context.ActionArguments["userName"] = _nameService.GetName();
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
        }
    }
}
