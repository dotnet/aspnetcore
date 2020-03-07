// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;

namespace BasicViews.Components
{
    public class CurrentUser : ViewComponent
    {
        private static readonly string[] Names = { "Curly", "Curly Joe", "Joe", "Larry", "Moe", "Shemp" };
        private static int index = 0;

        public string Invoke()
        {
            index = index++ / Names.Length;
            return Names[index];
        }
    }
}
