// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using Microsoft.AspNetCore.Shared;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures;

[DebuggerDisplay("Count = {ViewData.Count}")]
[DebuggerTypeProxy(typeof(DynamicViewDataDebugView))]
internal sealed class DynamicViewData : DynamicObject
{
    private readonly Func<ViewDataDictionary> _viewDataFunc;

    public DynamicViewData(Func<ViewDataDictionary> viewDataFunc)
    {
        ArgumentNullException.ThrowIfNull(viewDataFunc);

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
        ArgumentNullException.ThrowIfNull(binder);

        result = ViewData[binder.Name];

        // ViewDataDictionary[key] will never throw a KeyNotFoundException.
        // Similarly, return true so caller does not throw.
        return true;
    }

    public override bool TrySetMember(SetMemberBinder binder, object value)
    {
        ArgumentNullException.ThrowIfNull(binder);

        ViewData[binder.Name] = value;

        // Can always add / update a ViewDataDictionary value.
        return true;
    }

    private sealed class DynamicViewDataDebugView(DynamicViewData dictionary)
    {
        private readonly ViewDataDictionary _dictionary = dictionary.ViewData;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public DictionaryItemDebugView<string, object>[] Items => _dictionary.Select(pair => new DictionaryItemDebugView<string, object>(pair)).ToArray();
    }
}
