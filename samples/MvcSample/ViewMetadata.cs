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
