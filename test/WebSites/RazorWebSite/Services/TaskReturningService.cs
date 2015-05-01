// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace RazorWebSite
{
    public class TaskReturningService
    {
        public async Task<string> GetValueAsync()
        {
            await Task.Delay(100);
            return "Value from TaskReturningString";
        }
    }
}