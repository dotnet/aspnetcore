// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text;
using Microsoft.AspNet.Mvc.Rendering;

namespace Microsoft.AspNet.Mvc
{
    public interface IActionResultHelper
    {
        ContentResult Content(string value, string contentType, Encoding contentEncoding);
        JsonResult Json(object value);
        ViewResult View(string view, ViewDataDictionary viewData);
    }
}
