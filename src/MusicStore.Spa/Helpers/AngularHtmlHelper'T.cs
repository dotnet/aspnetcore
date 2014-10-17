using System.Collections.Generic;
using Microsoft.AspNet.Mvc.ModelBinding;

namespace Microsoft.AspNet.Mvc.Rendering
{
    public class AngularHtmlHelper<TModel> : HtmlHelper<TModel>
    {
        public AngularHtmlHelper(
            IHtmlGenerator generator,
            ICompositeViewEngine viewEngine,
            IModelMetadataProvider metadataProvider)
            : base(generator, viewEngine, metadataProvider)
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
            return GetClientValidationRules(metadata, name);
        }
    }
}