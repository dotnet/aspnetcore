using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.Diagnostics.Elm.Views
{
    public class RequestPageModel
    {
        public Guid RequestID { get; set; }

        public ActivityContext Activity { get; set; }

        public ViewOptions Options { get; set; }
    }
}