using System;
using Microsoft.AspNet.Mvc.ModelBinding;

namespace Microsoft.AspNet.Mvc.Rendering
{
    public class ViewData<TModel> : ViewData
    {
        // Fallback ModelMetadata based on TModel. Used when Model is null and base ViewData class is unable to
        // determine the correct metadata.
        private readonly ModelMetadata _defaultModelMetadata;

        public ViewData([NotNull] IModelMetadataProvider metadataProvider)
            : base(metadataProvider)
        {
            _defaultModelMetadata = MetadataProvider.GetMetadataForType(null, typeof(TModel));
        }

        public ViewData(ViewData source)
            : base(source)
        {
            var original = source as ViewData<TModel>;
            if (original != null)
            {
                _defaultModelMetadata = original._defaultModelMetadata;
            }
            else
            {
                _defaultModelMetadata = MetadataProvider.GetMetadataForType(null, typeof(TModel));
            }
        }

        public new TModel Model
        {
            get { return (TModel)base.Model; }
            set { SetModel(value); }
        }

        public override ModelMetadata ModelMetadata
        {
            get
            {
                return base.ModelMetadata ?? _defaultModelMetadata;
            }
        }

        protected override void SetModel(object value)
        {
            // IsCompatibleObject verifies if the value is either an instance of TModel or (if value is null) that
            // TModel is a nullable type.
            bool castWillSucceed = typeof(TModel).IsCompatibleWith(value);

            if (castWillSucceed)
            {
                base.SetModel(value);
            }
            else
            {
                string message;
                if (value == null)
                {
                    message = Resources.FormatViewData_ModelCannotBeNull(typeof(TModel));
                }
                else
                {
                    message = Resources.FormatViewData_WrongTModelType(value.GetType(), typeof(TModel));
                }

                throw new InvalidOperationException(message);
            }
        }
    }
}
