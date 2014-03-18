using System;
using Microsoft.AspNet.Mvc.ModelBinding.Internal;

namespace Microsoft.AspNet.Mvc.Rendering
{
    public class ViewData<TModel> : ViewData
    {
        public ViewData()
            : base()
        {
        }

        public ViewData(ViewData source) :
            base(source)
        {
        }

        public new TModel Model
        {
            get { return (TModel)base.Model; }
            set { SetModel(value); }
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
