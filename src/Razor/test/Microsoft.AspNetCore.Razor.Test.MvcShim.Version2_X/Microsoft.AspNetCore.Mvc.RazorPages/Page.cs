// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Mvc.Razor;

namespace Microsoft.AspNetCore.Mvc.RazorPages
{
    public abstract class Page : RazorPageBase
    {
        public PageContext PageContext { get; set; }

        public virtual RedirectResult Redirect(string url) => throw new NotImplementedException();

        public override void EnsureRenderedBodyOrSections()
        {

        }

        public override void BeginContext(int position, int length, bool isLiteral)
        {
        }

        public override void EndContext()
        {
        }

        public virtual bool TryValidateModel(object model) => throw new NotImplementedException();

        public virtual bool TryValidateModel(object model, string prefix) => throw new NotImplementedException();
    }
}
