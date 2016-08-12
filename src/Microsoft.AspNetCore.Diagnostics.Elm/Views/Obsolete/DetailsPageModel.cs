using System;

namespace Microsoft.AspNetCore.Diagnostics.Elm.Views
{
    [Obsolete("This type is for internal use only and will be removed in a future version.")]
    public class DetailsPageModel
    {
        public ActivityContext Activity { get; set; }

        public ViewOptions Options { get; set; }
    }
}