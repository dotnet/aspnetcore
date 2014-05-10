using System;
using System.Collections.Generic;
using Microsoft.AspNet.Mvc.ModelBinding;

namespace Microsoft.AspNet.Mvc.Rendering
{
    public class AngularHtmlHelper<TModel> : HtmlHelper<TModel>
    {
        public AngularHtmlHelper(IViewEngine viewEngine, IModelMetadataProvider metadataProvider, IUrlHelper urlHelper, AntiForgery antiForgeryInstance, IActionBindingContextProvider actionBindingContextProvider)
            : base(viewEngine, metadataProvider, urlHelper, antiForgeryInstance, actionBindingContextProvider)
        {
            
        }

        // TODO: These members are required to give helper extensions access to required protected members

        public IModelMetadataProvider ModelMetadataProvider
        {
            get
            {
                return MetadataProvider;
            }
        }

        public IEnumerable<ModelClientValidationRule> GetClientValidators(string name, ModelMetadata metadata)
        {
            return GetClientValidationRules(name, metadata);
        }

        public HtmlString GetFullHtmlFieldId(string expression)
        {
            return GenerateId(expression);
        }
    }
}