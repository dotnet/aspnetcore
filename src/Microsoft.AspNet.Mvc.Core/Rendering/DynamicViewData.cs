// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using System.Collections.Generic;
using System.Dynamic;
using Microsoft.AspNet.Mvc.Core;

namespace Microsoft.AspNet.Mvc.Rendering
{
    public class DynamicViewData : DynamicObject
    {
        private readonly Func<ViewDataDictionary> _viewDataFunc;

        public DynamicViewData([NotNull] Func<ViewDataDictionary> viewDataFunc)
        {
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

        public override bool TryGetMember([NotNull] GetMemberBinder binder, out object result)
        {
            result = ViewData[binder.Name];

            // ViewDataDictionary[key] will never throw a KeyNotFoundException.
            // Similarly, return true so caller does not throw.
            return true;
        }

        public override bool TrySetMember([NotNull] SetMemberBinder binder, object value)
        {
            ViewData[binder.Name] = value;

            // Can always add / update a ViewDataDictionary value.
            return true;
        }
    }
}
