using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;

namespace Microsoft.AspNet.Mvc
{
    public class ActionExecutedContext : FilterContext
    {
        private Exception _exception;
        private ExceptionDispatchInfo _exceptionDispatchInfo;

        public ActionExecutedContext(
            [NotNull] ActionContext actionContext,
            [NotNull] IList<IFilter> filters)
            : base(actionContext, filters)
        {
        }

        public virtual bool Canceled { get; set; }

        public virtual Exception Exception
        {
            get
            {
                if (_exception == null && _exceptionDispatchInfo != null)
                {
                    return _exceptionDispatchInfo.SourceException;
                }
                else
                {
                    return _exception;
                }
            }

            set
            {
                _exceptionDispatchInfo = null;
                _exception = value;
            }
        }

        public virtual ExceptionDispatchInfo ExceptionDispatchInfo
        {
            get
            {
                return _exceptionDispatchInfo;
            }

            set
            {
                _exception = null;
                _exceptionDispatchInfo = value;
            }
        }

        public virtual bool ExceptionHandled { get; set; }

        public virtual IActionResult Result { get; set; }
    }
}
