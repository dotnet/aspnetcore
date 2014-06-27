// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;

namespace Microsoft.AspNet.Mvc
{
    internal class PropertyActivator<TContext>
    {
        private readonly Func<TContext, object> _valueAccessor;
        private readonly Action<object, object> _fastPropertySetter;

        public PropertyActivator(PropertyInfo propertyInfo,
                                 Func<TContext, object> valueAccessor)
        {
            PropertyInfo = propertyInfo;
            _valueAccessor = valueAccessor;
            _fastPropertySetter = PropertyHelper.MakeFastPropertySetter(propertyInfo);
        }

        public PropertyInfo PropertyInfo { get; private set; }

        public object Activate(object view, TContext context)
        {
            var value = _valueAccessor(context);
            _fastPropertySetter(view, value);
            return value;
        }

        /// <summary>
        /// Returns a list of properties on a type that are decorated with
        /// the specified activateAttributeType and have setters.
        /// </summary>
        public static PropertyActivator<TContext>[] GetPropertiesToActivate(
            Type type,
            Type activateAttributeType,
            Func<PropertyInfo, PropertyActivator<TContext>> createActivateInfo)
        {
            return type.GetRuntimeProperties()
                       .Where(property =>
                              property.IsDefined(activateAttributeType) &&
                              property.GetIndexParameters().Length == 0 &&
                              property.SetMethod != null &&
                              !property.SetMethod.IsStatic)
                       .Select(createActivateInfo)
                       .ToArray();
        }
    }
}