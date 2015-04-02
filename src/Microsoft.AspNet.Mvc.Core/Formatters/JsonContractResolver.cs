// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// The default <see cref="IContractResolver"/> for <see cref="JsonInputFormatter"/>.
    /// It determines if a value type member has <see cref="RequiredAttribute"/> and sets the appropriate 
    /// JsonProperty settings.
    /// </summary>
    public class JsonContractResolver : DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var property =  base.CreateProperty(member, memberSerialization);

            var required = member.GetCustomAttribute(typeof(RequiredAttribute), inherit: true);
            if (required != null)
            {
                var propertyType = ((PropertyInfo)member).PropertyType;

                // DefaultObjectValidator does required attribute validation on properties based on the property
                // value being null. Since this is not possible in case of value types, we depend on the formatters
                // to handle value type validation.
                // With the following settings here, if a value is not present on the wire for value types
                // like primitive, struct etc., Json.net's serializer would throw exception which we catch
                // and add it to model state.
                if (propertyType.IsValueType() && !propertyType.IsNullableValueType())
                {
                    property.Required = Required.AllowNull;
                }
            }

            return property;
        }
    }
}