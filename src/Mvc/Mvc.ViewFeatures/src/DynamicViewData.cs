// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Dynamic;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures
{
    internal class DynamicViewData : DynamicObject
    {
        private readonly Func<ViewDataDictionary> _viewDataFunc;

        public DynamicViewData(Func<ViewDataDictionary> viewDataFunc)
        {
            if (viewDataFunc == null)
            {
                throw new ArgumentNullException(nameof(viewDataFunc));
            }

            _viewDataFunc = viewDataFunc;
        }

        private ViewDataDictionary ViewData
        {
            get
            {
                var viewData = _viewDataFunc();
                if (viewData == null)
                {
                    throw new InvalidOperationException(Resources.DynamicViewData_ViewDataNull);
                }

                return viewData;
            }
        }

        // Implementing this function extends the ViewBag contract, supporting or improving some scenarios. For example
        // having this method improves the debugging experience as it provides the debugger with the list of all
        // properties currently defined on the object.
        public override IEnumerable<string> GetDynamicMemberNames()
        {
            return ViewData.Keys;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            if (binder == null)
            {
                throw new ArgumentNullException(nameof(binder));
            }

            result = ViewData[binder.Name];

            // ViewDataDictionary[key] will never throw a KeyNotFoundException.
            // Similarly, return true so caller does not throw.
            return true;
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            if (binder == null)
            {
                throw new ArgumentNullException(nameof(binder));
            }

            ViewData[binder.Name] = value;

            // Can always add / update a ViewDataDictionary value.
            return true;
        }
    }
}
