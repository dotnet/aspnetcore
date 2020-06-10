using System;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate
{
    internal class IntermediateTokenWithDeferredContentAllocation : IntermediateToken
    {
        public Func<string> ContentGetter { get; set; }

        public override string Content
        {
            get
            {
                if (base.Content == null && ContentGetter != null)
                {
                    Content = ContentGetter();
                    ContentGetter = null;
                }

                return base.Content;
            }
        }
    }
}
