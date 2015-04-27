// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc
{
    public class ResultExecutedContext : FilterContext
    {
        private Exception _exception;
        private ExceptionDispatchInfo _exceptionDispatchInfo;

        public ResultExecutedContext(
            [NotNull] ActionContext actionContext,
            [NotNull] IList<IFilter> filters,
            [NotNull] IActionResult result,
            object controller)
            : base(actionContext, filters)
        {
            Result = result;
            Controller = controller;
        }

        public virtual bool Canceled { get; set; }

        public virtual object Controller { get; }

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

        public virtual IActionResult Result { get; private set; }
    }
}
