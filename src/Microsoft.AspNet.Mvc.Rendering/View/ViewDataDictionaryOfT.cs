using System;
using Microsoft.AspNet.Mvc.ModelBinding;

namespace Microsoft.AspNet.Mvc.Rendering
{
    public class ViewDataDictionary<TModel> : ViewDataDictionary
    {
        // Fallback ModelMetadata based on TModel. Used when Model is null and base ViewDataDictionary class is unable
        // to determine the correct metadata.
        private readonly ModelMetadata _defaultModelMetadata;

        public ViewDataDictionary([NotNull] IModelMetadataProvider metadataProvider)
            : base(metadataProvider)
        {
            _defaultModelMetadata = MetadataProvider.GetMetadataForType(null, typeof(TModel));
        }

        public ViewDataDictionary([NotNull] IModelMetadataProvider metadataProvider,
            [NotNull] ModelStateDictionary modelState)
            : base(metadataProvider, modelState)
        {
        }

        /// <inheritdoc />
        public ViewDataDictionary([NotNull] ViewDataDictionary source)
            : this(source, source.Model)
        {
        }

        /// <inheritdoc />
        public ViewDataDictionary([NotNull] ViewDataDictionary source, object model)
            : base(source, model)
        {
            var original = source as ViewDataDictionary<TModel>;
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
            var castWillSucceed = typeof(TModel).IsCompatibleWith(value);

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
