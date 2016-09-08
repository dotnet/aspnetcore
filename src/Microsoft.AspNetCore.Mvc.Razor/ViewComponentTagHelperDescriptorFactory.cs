// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Razor.Host;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.AspNetCore.Razor.Compilation.TagHelpers;
using Microsoft.AspNetCore.Razor.Runtime.TagHelpers;

namespace Microsoft.AspNetCore.Mvc.Razor
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

            foreach (var parameter in methodParameters)
            {
                var lowerKebabName = TagHelperDescriptorFactory.ToHtmlCase(parameter.Name);
                var descriptor = new TagHelperAttributeDescriptor
                {
                    Name = lowerKebabName,
                    PropertyName = parameter.Name,
                    TypeName = parameter.ParameterType.FullName
                };

                descriptor.IsEnum = parameter.ParameterType.GetTypeInfo().IsEnum;
                descriptor.IsIndexer = false;

                attributeDescriptors.Add(descriptor);
            }

            tagHelperDescriptor.Attributes = attributeDescriptors;
            tagHelperDescriptor.RequiredAttributes = tagHelperDescriptor.Attributes.Select(
                attribute => new TagHelperRequiredAttributeDescriptor
                {
                    Name = attribute.Name
                });
        }

        private string GetTagName(ViewComponentDescriptor descriptor) =>
            $"vc:{TagHelperDescriptorFactory.ToHtmlCase(descriptor.ShortName)}";
    }
}
