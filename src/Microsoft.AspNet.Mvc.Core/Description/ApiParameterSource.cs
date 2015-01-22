// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.AspNet.Mvc.Core;

namespace Microsoft.AspNet.Mvc.Description
{
    /// <summary>
    /// A metadata description of the source of an <see cref="ApiParameterDescription"/> for an HTTP request.
    /// </summary>
    [DebuggerDisplay("Source: {DisplayName}")]
    public class ApiParameterSource : IEquatable<ApiParameterSource>
    {
        /// <summary>
        /// An <see cref="ApiParameterSource"/> for the request body.
        /// </summary>
        public static readonly ApiParameterSource Body = new ApiParameterSource(
            "Body",
            Resources.ApiParameterSource_Body);

        /// <summary>
        /// An <see cref="ApiParameterSource"/> for a custom model binder (unknown data source).
        /// </summary>
        public static readonly ApiParameterSource Custom = new ApiParameterSource(
            "Custom", 
            Resources.ApiParameterSource_Custom);

        /// <summary>
        /// An <see cref="ApiParameterSource"/> for the request form-data.
        /// </summary>
        public static readonly ApiParameterSource Form = new ApiParameterSource(
            "Form",
            Resources.ApiParameterSource_Form);

        /// <summary>
        /// An <see cref="ApiParameterSource"/> for the request headers.
        /// </summary>
        public static readonly ApiParameterSource Header = new ApiParameterSource(
            "Header",
            Resources.ApiParameterSource_Header);

        /// <summary>
        /// An <see cref="ApiParameterSource"/> for a parameter that should be hidden. Used when
        /// a parameter cannot be set with user input.
        /// </summary>
        public static readonly ApiParameterSource Hidden = new ApiParameterSource(
            "Hidden",
            Resources.ApiParameterSource_Hidden);

        /// <summary>
        /// An <see cref="ApiParameterSource"/> for model binding. Includes form-data, query-string
        /// and headers from the request.
        /// </summary>
        public static readonly ApiParameterSource ModelBinding = new ApiParameterSource(
            "ModelBinding",
            Resources.ApiParameterSource_ModelBinding);

        /// <summary>
        /// An <see cref="ApiParameterSource"/> for the request url path.
        /// </summary>
        public static readonly ApiParameterSource Path = new ApiParameterSource(
            "Path",
            Resources.ApiParameterSource_Path);

        /// <summary>
        /// An <see cref="ApiParameterSource"/> for the request query-string.
        /// </summary>
        public static readonly ApiParameterSource Query = new ApiParameterSource(
            "Query",
            Resources.ApiParameterSource_Query);

        /// <summary>
        /// Creates a new <see cref="ApiParameterSource"/>.
        /// </summary>
        /// <param name="id">The id. Used for comparison.</param>
        /// <param name="displayName"> The display name.</param>
        public ApiParameterSource([NotNull] string id, string displayName)
        {
            Id = id;
            DisplayName = displayName;
        }

        /// <summary>
        /// Gets the display name.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Gets the id.
        /// </summary>
        public string Id { get; }

        /// <inheritdoc />
        public bool Equals(ApiParameterSource other)
        {
            return other == null ? false : string.Equals(other.Id, Id, StringComparison.Ordinal);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return Equals(obj as ApiParameterSource);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        /// <inheritdoc />
        public static bool operator ==(ApiParameterSource s1, ApiParameterSource s2)
        {
            if (object.ReferenceEquals(s1, null))
            {
                return object.ReferenceEquals(s2, null); ;
            }

            return s1.Equals(s2);
        }

        /// <inheritdoc />
        public static bool operator !=(ApiParameterSource s1, ApiParameterSource s2)
        {
            return !(s1 == s2);
        }
    }
}