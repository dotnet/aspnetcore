using System;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate
{
    internal class LazyIntermediateToken : IntermediateToken
    {
        public Func<string> ContentFactory { get; set; }

        public override string Content
        {
            get
            {
                if (base.Content == null && ContentFactory != null)
                {
                    Content = ContentFactory();
                    ContentFactory = null;
                }

                return base.Content;
            }
        }
    }
}
