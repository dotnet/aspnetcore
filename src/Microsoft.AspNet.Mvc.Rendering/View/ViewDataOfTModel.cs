using System;
using System.Globalization;
using Microsoft.AspNet.Mvc.ModelBinding.Internal;
using Microsoft.AspNet.Mvc.Rendering;

namespace Microsoft.AspNet.Mvc
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
            // IsCompatibleObject verifies if the value is either an instance of TModel or if value happens to be null that TModel is nullable type.
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
                    message = String.Format(CultureInfo.CurrentCulture, Resources.ViewData_ModelCannotBeNull, typeof(TModel));
                }
                else
                {
                    message = String.Format(CultureInfo.CurrentCulture, Resources.ViewData_WrongTModelType, value.GetType(), typeof(TModel));
                }
                throw new InvalidOperationException(message);
            }
        }
    }
}
