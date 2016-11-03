// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Mvc.Razor.Host;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.AspNetCore.Razor.Compilation.TagHelpers;
using Microsoft.AspNetCore.Razor.Runtime.TagHelpers;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Mvc.Razor.Internal
{
    /// <summary>
    /// Provides methods to create tag helper representations of view components.
    /// </summary>
    public class ViewComponentTagHelperDescriptorFactory
    {
        private readonly IViewComponentDescriptorProvider _descriptorProvider;

        /// <summary>
        /// Creates a new <see cref="ViewComponentTagHelperDescriptorFactory"/>, 
        /// then creates <see cref="TagHelperDescriptor"/>s for <see cref="ViewComponent"/>s 
        /// in the given <see cref="IViewComponentDescriptorProvider"/>. 
        /// </summary>
        /// <param name="descriptorProvider">The provider of <see cref="ViewComponentDescriptor"/>s.</param>
        public ViewComponentTagHelperDescriptorFactory(IViewComponentDescriptorProvider descriptorProvider)
        {
            if (descriptorProvider == null)
            {
                throw new ArgumentNullException(nameof(descriptorProvider));
            }

            _descriptorProvider = descriptorProvider;
        }

        /// <summary>
        /// Creates <see cref="TagHelperDescriptor"/> representations of <see cref="ViewComponent"/>s
        /// in an <see href="Assembly"/> represented by the given <paramref name="assemblyName"/>.
        /// </summary>
        /// <param name="assemblyName">The name of the assembly containing 
        /// the <see cref="ViewComponent"/>s to translate.</param>
        /// <returns>A <see cref="IEnumerable{TagHelperDescriptor}"/>, 
        /// one for each <see cref="ViewComponent"/>.</returns>
        public IEnumerable<TagHelperDescriptor> CreateDescriptors(string assemblyName)
        {
            if (assemblyName == null)
            {
                throw new ArgumentNullException(nameof(assemblyName));
            }

            var viewComponentDescriptors = _descriptorProvider
                .GetViewComponents()
                .Where(viewComponent => string.Equals(assemblyName, viewComponent.TypeInfo.Assembly.GetName().Name,
                    StringComparison.Ordinal));

            var tagHelperDescriptors = viewComponentDescriptors
                .Where(d => !d.MethodInfo.ContainsGenericParameters)
                .Select(viewComponentDescriptor => CreateDescriptor(viewComponentDescriptor));

            return tagHelperDescriptors;
        }

        private TagHelperDescriptor CreateDescriptor(ViewComponentDescriptor viewComponentDescriptor)
        {
            var assemblyName = viewComponentDescriptor.TypeInfo.Assembly.GetName().Name;
            var tagName = GetTagName(viewComponentDescriptor);
            var typeName = $"__Generated__{viewComponentDescriptor.ShortName}ViewComponentTagHelper";

            var tagHelperDescriptor = new TagHelperDescriptor
            {
                TagName = tagName,
                TypeName = typeName,
                AssemblyName = assemblyName
            };

            SetAttributeDescriptors(viewComponentDescriptor, tagHelperDescriptor);

            tagHelperDescriptor.PropertyBag.Add(
                ViewComponentTagHelperDescriptorConventions.ViewComponentNameKey, viewComponentDescriptor.ShortName);

            return tagHelperDescriptor;
        }

        private void SetAttributeDescriptors(ViewComponentDescriptor viewComponentDescriptor,
            TagHelperDescriptor tagHelperDescriptor)
        {
            var methodParameters = viewComponentDescriptor.MethodInfo.GetParameters();
            var attributeDescriptors = new List<TagHelperAttributeDescriptor>();
            var indexerDescriptors = new List<TagHelperAttributeDescriptor>();
            var requiredAttributeDescriptors = new List<TagHelperRequiredAttributeDescriptor>();

            foreach (var parameter in methodParameters)
            {
                var lowerKebabName = TagHelperDescriptorFactory.ToHtmlCase(parameter.Name);
                var typeName = GetCSharpTypeName(parameter.ParameterType);
                var descriptor = new TagHelperAttributeDescriptor
                {
                    Name = lowerKebabName,
                    PropertyName = parameter.Name,
                    TypeName = typeName
                };

                descriptor.IsEnum = parameter.ParameterType.GetTypeInfo().IsEnum;
                descriptor.IsIndexer = false;

                attributeDescriptors.Add(descriptor);

                var indexerDescriptor = GetIndexerAttributeDescriptor(parameter, lowerKebabName);
                if (indexerDescriptor != null)
                {
                    indexerDescriptors.Add(indexerDescriptor);
                }
                else
                {
                    // Set required attributes only for non-indexer attributes. Indexer attributes can't be required attributes
                    // because there are two ways of setting values for the attribute.
                    requiredAttributeDescriptors.Add(new TagHelperRequiredAttributeDescriptor
                    {
                        Name = lowerKebabName
                    });
                }
            }

            attributeDescriptors.AddRange(indexerDescriptors);
            tagHelperDescriptor.Attributes = attributeDescriptors;
            tagHelperDescriptor.RequiredAttributes = requiredAttributeDescriptors;
        }

        private TagHelperAttributeDescriptor GetIndexerAttributeDescriptor(ParameterInfo parameter, string name)
        {
            var dictionaryTypeArguments = ClosedGenericMatcher.ExtractGenericInterface(
                parameter.ParameterType,
                typeof(IDictionary<,>))
                ?.GenericTypeArguments
                .Select(t => t.IsGenericParameter ? null : t)
                .ToArray();

            if (dictionaryTypeArguments?[0] != typeof(string))
            {
                return null;
            }

            var type = dictionaryTypeArguments[1];
            var descriptor = new TagHelperAttributeDescriptor
            {
                Name = name + "-",
                PropertyName = parameter.Name,
                TypeName = GetCSharpTypeName(type),
                IsEnum = type.GetTypeInfo().IsEnum,
                IsIndexer = true
            };

            return descriptor;
        }

        private string GetTagName(ViewComponentDescriptor descriptor)
        {
            return $"vc:{TagHelperDescriptorFactory.ToHtmlCase(descriptor.ShortName)}";
        }

        // Internal for testing.
        internal static string GetCSharpTypeName(Type type)
        {
            var outputBuilder = new StringBuilder();
            WriteCSharpTypeName(type, outputBuilder);

            var typeName = outputBuilder.ToString();

            // We don't want to add global:: to the top level type because Razor does that for us.
            return typeName.Substring("global::".Length);
        }

        private static void WriteCSharpTypeName(Type type, StringBuilder outputBuilder)
        {
            var typeInfo = type.GetTypeInfo();
            if (typeInfo.IsByRef)
            {
                WriteCSharpTypeName(typeInfo.GetElementType(), outputBuilder);
            }
            else if (typeInfo.IsNested)
            {
                WriteNestedTypes(type, outputBuilder);
            }
            else if (typeInfo.IsGenericType)
            {
                outputBuilder.Append("global::");
                var part = type.FullName.Substring(0, type.FullName.IndexOf('`'));
                outputBuilder.Append(part);

                var genericArguments = type.GenericTypeArguments;
                WriteGenericArguments(genericArguments, 0, genericArguments.Length, outputBuilder);
            }
            else
            {
                outputBuilder.Append("global::");
                outputBuilder.Append(type.FullName);
            }
        }

        private static void WriteNestedTypes(Type type, StringBuilder outputBuilder)
        {
            var nestedTypes = new List<Type>();
            var currentType = type;
            do
            {
                nestedTypes.Insert(0, currentType);
                currentType = currentType.DeclaringType;
            } while (currentType.IsNested);

            nestedTypes.Insert(0, currentType);

            outputBuilder.Append("global::");
            outputBuilder.Append(currentType.Namespace);
            outputBuilder.Append(".");

            var typeArgumentIndex = 0;
            for (var i = 0; i < nestedTypes.Count; i++)
            {
                var nestedType = nestedTypes[i];
                var arityIndex = nestedType.Name.IndexOf('`');
                if (arityIndex >= 0)
                {
                    var part = nestedType.Name.Substring(0, arityIndex);
                    outputBuilder.Append(part);

                    var genericArguments = type.GenericTypeArguments;
                    var typeArgumentCount = nestedType.IsConstructedGenericType ?
                        nestedType.GenericTypeArguments.Length : nestedType.GetTypeInfo().GenericTypeParameters.Length;

                    WriteGenericArguments(genericArguments, typeArgumentIndex, typeArgumentCount, outputBuilder);

                    typeArgumentIndex = typeArgumentCount;
                }
                else
                {
                    outputBuilder.Append(nestedType.Name);
                }

                if (i + 1 < nestedTypes.Count)
                {
                    outputBuilder.Append(".");
                }
            }
        }

        private static void WriteGenericArguments(Type[] genericArguments, int startIndex, int length, StringBuilder outputBuilder)
        {
            outputBuilder.Append("<");

            for (var i = startIndex; i < length; i++)
            {
                WriteCSharpTypeName(genericArguments[i], outputBuilder);
                if (i + 1 < length)
                {
                    outputBuilder.Append(", ");
                }
            }

            outputBuilder.Append(">");
        }
    }
}
