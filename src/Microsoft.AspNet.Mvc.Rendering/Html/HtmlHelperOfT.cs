using System;
using Microsoft.AspNet.Mvc.ModelBinding;

namespace Microsoft.AspNet.Mvc
{
    public class HtmlHelper<TModel> : HtmlHelper
    {
        public HtmlHelper(RequestContext requestContext, ViewData<TModel> viewData)
            : base(requestContext, viewData)
        {
            if (requestContext == null)
            {
                throw new ArgumentNullException("requestContext");
            }

            ViewData = viewData;
        }

        public new ViewData<TModel> ViewData
        {
            get; private set;
        }
    }
}
