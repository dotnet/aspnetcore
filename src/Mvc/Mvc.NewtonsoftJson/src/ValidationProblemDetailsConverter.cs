// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.Mvc.NewtonsoftJson
{
    /// <summary>
    /// A RFC 7807 compliant <see cref="JsonConverter"/> for <see cref="ValidationProblemDetails"/>.
    /// </summary>
    public sealed class ValidationProblemDetailsConverter : JsonConverter
    {
        /// <inheritdoc />
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(ValidationProblemDetails);
        }

        /// <inheritdoc />
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var annotatedProblemDetails = serializer.Deserialize<AnnotatedValidationProblemDetails>(reader);
            if (annotatedProblemDetails == null)
            {
                return null;
            }

            var problemDetails = (ValidationProblemDetails)existingValue ?? new ValidationProblemDetails();
            annotatedProblemDetails.CopyTo(problemDetails);

            return problemDetails;
        }

        /// <inheritdoc />
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            var problemDetails = (ValidationProblemDetails)value;
            var annotatedProblemDetails = new AnnotatedValidationProblemDetails(problemDetails);

            serializer.Serialize(writer, annotatedProblemDetails);
        }

        private class AnnotatedValidationProblemDetails : AnnotatedProblemDetails
        {
            /// <remarks>
            /// Required for JSON.NET deserialization.
            /// </remarks>
            public AnnotatedValidationProblemDetails() { }

            public AnnotatedValidationProblemDetails(ValidationProblemDetails problemDetails)
                : base(problemDetails)
            {
                foreach (var kvp in problemDetails.Errors)
                {
                    Errors[kvp.Key] = kvp.Value;
                }
            }

            [JsonProperty(PropertyName = "errors")]
            public IDictionary<string, string[]> Errors { get; } = new Dictionary<string, string[]>(StringComparer.Ordinal);

            public void CopyTo(ValidationProblemDetails problemDetails)
            {
                base.CopyTo(problemDetails);

                foreach (var kvp in Errors)
                {
                    problemDetails.Errors[kvp.Key] = kvp.Value;
                }
            }
        }
    }
}
