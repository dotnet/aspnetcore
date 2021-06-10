// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    /// <summary>
    /// A metadata object representing a source of data for model binding.
    /// </summary>
    [DebuggerDisplay("Source: {DisplayName}")]
    public class BindingSource : IEquatable<BindingSource?>
    {
        /// <summary>
        /// A <see cref="BindingSource"/> for the request body.
        /// </summary>
        public static readonly BindingSource Body = new BindingSource(
            "Body",
            Resources.BindingSource_Body,
            isGreedy: true,
            isFromRequest: true);

        /// <summary>
        /// A <see cref="BindingSource"/> for a custom model binder (unknown data source).
        /// </summary>
        public static readonly BindingSource Custom = new BindingSource(
            "Custom",
            Resources.BindingSource_Custom,
            isGreedy: true,
            isFromRequest: true);

        /// <summary>
        /// A <see cref="BindingSource"/> for the request form-data.
        /// </summary>
        public static readonly BindingSource Form = new BindingSource(
            "Form",
            Resources.BindingSource_Form,
            isGreedy: false,
            isFromRequest: true);

        /// <summary>
        /// A <see cref="BindingSource"/> for the request headers.
        /// </summary>
        public static readonly BindingSource Header = new BindingSource(
            "Header",
            Resources.BindingSource_Header,
            isGreedy: true,
            isFromRequest: true);

        /// <summary>
        /// A <see cref="BindingSource"/> for model binding. Includes form-data, query-string
        /// and route data from the request.
        /// </summary>
        public static readonly BindingSource ModelBinding = new BindingSource(
            "ModelBinding",
            Resources.BindingSource_ModelBinding,
            isGreedy: false,
            isFromRequest: true);

        /// <summary>
        /// A <see cref="BindingSource"/> for the request url path.
        /// </summary>
        public static readonly BindingSource Path = new BindingSource(
            "Path",
            Resources.BindingSource_Path,
            isGreedy: false,
            isFromRequest: true);

        /// <summary>
        /// A <see cref="BindingSource"/> for the request query-string.
        /// </summary>
        public static readonly BindingSource Query = new BindingSource(
            "Query",
            Resources.BindingSource_Query,
            isGreedy: false,
            isFromRequest: true);

        /// <summary>
        /// A <see cref="BindingSource"/> for the request url path or query string.
        /// </summary>
        public static readonly BindingSource PathOrQuery = new BindingSource(
            "PathOrQuery",
            "PathOrQuery",
            isGreedy: false,
            isFromRequest: true);

        /// <summary>
        /// A <see cref="BindingSource"/> for request services.
        /// </summary>
        public static readonly BindingSource Services = new BindingSource(
            "Services",
            Resources.BindingSource_Services,
            isGreedy: true,
            isFromRequest: false);

        /// <summary>
        /// A <see cref="BindingSource"/> for special parameter types that are not user input.
        /// </summary>
        public static readonly BindingSource Special = new BindingSource(
            "Special",
            Resources.BindingSource_Special,
            isGreedy: true,
            isFromRequest: false);

        /// <summary>
        /// A <see cref="BindingSource"/> for <see cref="IFormFile"/>, <see cref="IFormCollection"/>, and <see cref="IFormFileCollection"/>.
        /// </summary>
        public static readonly BindingSource FormFile = new BindingSource(
            "FormFile",
            Resources.BindingSource_FormFile,
            isGreedy: true,
            isFromRequest: true);

        /// <summary>
        /// Creates a new <see cref="BindingSource"/>.
        /// </summary>
        /// <param name="id">The id, a unique identifier.</param>
        /// <param name="displayName">The display name.</param>
        /// <param name="isGreedy">A value indicating whether or not the source is greedy.</param>
        /// <param name="isFromRequest">
        /// A value indicating whether or not the data comes from the HTTP request.
        /// </param>
        public BindingSource(string id, string displayName, bool isGreedy, bool isFromRequest)
        {
            if (id == null)
            {
                throw new ArgumentNullException(nameof(id));
            }

            Id = id;
            DisplayName = displayName;
            IsGreedy = isGreedy;
            IsFromRequest = isFromRequest;
        }

        /// <summary>
        /// Gets the display name for the source.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Gets the unique identifier for the source. Sources are compared based on their Id.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Gets a value indicating whether or not a source is greedy. A greedy source will bind a model in
        /// a single operation, and will not decompose the model into sub-properties.
        /// </summary>
        /// <remarks>
        /// <para>
        /// For sources based on a <see cref="IValueProvider"/>, setting <see cref="IsGreedy"/> to <c>false</c>
        /// will most closely describe the behavior. This value is used inside the default model binders to
        /// determine whether or not to attempt to bind properties of a model.
        /// </para>
        /// <para>
        /// Set <see cref="IsGreedy"/> to <c>true</c> for most custom <see cref="IModelBinder"/> implementations.
        /// </para>
        /// <para>
        /// If a source represents an <see cref="IModelBinder"/> which will recursively traverse a model's properties
        /// and bind them individually using <see cref="IValueProvider"/>, then set <see cref="IsGreedy"/> to
        /// <c>true</c>.
        /// </para>
        /// </remarks>
        public bool IsGreedy { get; }

        /// <summary>
        /// Gets a value indicating whether or not the binding source uses input from the current HTTP request.
        /// </summary>
        /// <remarks>
        /// Some sources (like <see cref="BindingSource.Services"/>) are based on application state and not user
        /// input. These are excluded by default from ApiExplorer diagnostics.
        /// </remarks>
        public bool IsFromRequest { get; }

        /// <summary>
        /// Gets a value indicating whether or not the <see cref="BindingSource"/> can accept
        /// data from <paramref name="bindingSource"/>.
        /// </summary>
        /// <param name="bindingSource">The <see cref="BindingSource"/> to consider as input.</param>
        /// <returns><c>True</c> if the source is compatible, otherwise <c>false</c>.</returns>
        /// <remarks>
        /// When using this method, it is expected that the left-hand-side is metadata specified
        /// on a property or parameter for model binding, and the right hand side is a source of
        /// data used by a model binder or value provider.
        ///
        /// This distinction is important as the left-hand-side may be a composite, but the right
        /// may not.
        /// </remarks>
        public virtual bool CanAcceptDataFrom(BindingSource bindingSource)
        {
            if (bindingSource == null)
            {
                throw new ArgumentNullException(nameof(bindingSource));
            }

            if (bindingSource is CompositeBindingSource)
            {
                var message = Resources.FormatBindingSource_CannotBeComposite(
                    bindingSource.DisplayName,
                    nameof(CanAcceptDataFrom));
                throw new ArgumentException(message, nameof(bindingSource));
            }

            if (this == bindingSource)
            {
                return true;
            }

            if (this == ModelBinding)
            {
                return bindingSource == Form || bindingSource == Path || bindingSource == Query;
            }

            return false;
        }

        /// <inheritdoc />
        public bool Equals(BindingSource? other)
        {
            return string.Equals(other?.Id, Id, StringComparison.Ordinal);
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return Equals(obj as BindingSource);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        /// <inheritdoc />
        public static bool operator ==(BindingSource? s1, BindingSource? s2)
        {
            if (s1 is null)
            {
                return s2 is null;
            }

            return s1.Equals(s2);
        }

        /// <inheritdoc />
        public static bool operator !=(BindingSource? s1, BindingSource? s2)
        {
            return !(s1 == s2);
        }
    }
}
