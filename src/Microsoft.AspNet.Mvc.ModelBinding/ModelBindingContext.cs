using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.Mvc.ModelBinding.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class ModelBindingContext
    {
        private string _modelName;
        private ModelStateDictionary _modelState;

        public ModelBindingContext()
        {
        }

        // copies certain values that won't change between parent and child objects,
        // e.g. ValueProvider, ModelState
        public ModelBindingContext(ModelBindingContext bindingContext)
        {
            if (bindingContext != null)
            {
                ModelState = bindingContext.ModelState;
                ValueProvider = bindingContext.ValueProvider;
                MetadataProvider = bindingContext.MetadataProvider;
                ModelBinder = bindingContext.ModelBinder;
                HttpContext = bindingContext.HttpContext;
            }
        }

        public object Model
        {
            get
            {
                EnsureModelMetadata();
                return ModelMetadata.Model;
            }
            set
            {
                EnsureModelMetadata();
                ModelMetadata.Model = value;
            }
        }

        public ModelMetadata ModelMetadata { get; set; }

        public string ModelName
        {
            get
            {
                if (_modelName == null)
                {
                    _modelName = String.Empty;
                }
                return _modelName;
            }
            set { _modelName = value; }
        }

        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "This is writeable to support unit testing")]
        public ModelStateDictionary ModelState
        {
            get
            {
                if (_modelState == null)
                {
                    _modelState = new ModelStateDictionary();
                }
                return _modelState;
            }
            set { _modelState = value; }
        }

        public Type ModelType
        {
            get
            {
                EnsureModelMetadata();
                return ModelMetadata.ModelType;
            }
        }

        public bool FallbackToEmptyPrefix { get; set; }

        public HttpContext HttpContext { get; set; }

        public IValueProvider ValueProvider
        {
            get;
            set;
        }

        public IModelBinder ModelBinder
        {
            get;
            set;
        }

        public IModelMetadataProvider MetadataProvider
        {
            get;
            set;
        }

        private void EnsureModelMetadata()
        {
            if (ModelMetadata == null)
            {
                throw new InvalidOperationException(Resources.ModelBindingContext_ModelMetadataMustBeSet);
            }
        }
    }
}
