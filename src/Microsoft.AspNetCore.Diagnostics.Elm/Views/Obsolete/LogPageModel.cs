using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Diagnostics.Elm.Views
{
    [Obsolete("This type is for internal use only and will be removed in a future version.")]
    public class LogPageModel
    {
        public IEnumerable<ActivityContext> Activities { get; set; }

        public ViewOptions Options { get; set; }

        public PathString Path { get; set; }
    }
}