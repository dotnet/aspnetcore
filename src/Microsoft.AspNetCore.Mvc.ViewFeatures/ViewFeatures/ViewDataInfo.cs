// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures
{
    public class ViewDataInfo
    {
        private object _value;
        private Func<object> _valueAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="ViewDataInfo"/> class with info about a
        /// <see cref="ViewDataDictionary"/> lookup which has already been evaluated.
        /// </summary>
        public ViewDataInfo(object container, object value)
        {
            Container = container;
            _value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ViewDataInfo"/> class with info about a
        /// <see cref="ViewDataDictionary"/> lookup which is evaluated when <see cref="Value"/> is read.
        /// </summary>
        public ViewDataInfo(object container, PropertyInfo propertyInfo, Func<object> valueAccessor)
        {
            Container = container;
            PropertyInfo = propertyInfo;
            _valueAccessor = valueAccessor;
        }

        public object Container { get; private set; }

        public PropertyInfo PropertyInfo { get; private set; }

        public object Value
        {
            get
            {
                if (_valueAccessor != null)
                {
                    _value = _valueAccessor();
                    _valueAccessor = null;
                }

                return _value;
            }
            set
            {
                _value = value;
                _valueAccessor = null;
            }
        }
    }
}
