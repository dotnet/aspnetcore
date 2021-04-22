// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RazorPagesWebSite
{
    [BindProperties]
    public class BindFormFile : PageModel
    {
        public string Property1 { get; set; }

        public IFormFile Form3 { get; set; }

        public FormFiles Forms { get; set; }

        public IActionResult OnPost()
        {
            if (string.IsNullOrEmpty(Property1))
            {
                throw new Exception($"{nameof(Property1)} is not bound.");
            }

            if (string.IsNullOrEmpty(Form3.Name) || Form3.Length == 0)
            {
                throw new Exception($"{nameof(Form3)} is not bound.");
            }

            if (string.IsNullOrEmpty(Forms.Form1.Name) || Forms.Form1.Length == 0)
            {
                throw new Exception($"{nameof(Forms.Form1)} is not bound.");
            }

            if (Forms.Form2 != null)
            {
                throw new Exception($"{nameof(Forms.Form2)} is bound.");
            }

            return new OkResult();
        }
    }

    public class FormFiles
    {
        public IFormFile Form1 { get; set; }

        public IFormFile Form2 { get; set; }
    }
}
