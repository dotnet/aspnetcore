// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if VIEWMETADATA
using System;
using System.Collections.Generic;

public class ViewMetadata
{
    public static Dictionary<string, Type> Metadata
    {
        get
        {
            return new Dictionary<string, Type>
            {
                {
                    "~/Views/Home/MyView.cshtml",
                    typeof(MvcSample.Views.MyView)
                },
                {
                    "~/Views/Shared/_Layout.cshtml",
                    typeof(MvcSample.Views.Layout)
                }
            };
        }
    }
}
#endif