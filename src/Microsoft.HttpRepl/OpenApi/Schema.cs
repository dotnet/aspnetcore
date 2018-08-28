// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.HttpRepl.OpenApi
{
    public class Schema
    {
        public void PrepareForUsage(JToken document)
        {
            AdditionalProperties?.Option1?.PrepareForUsage(document);

            if (AllOf != null)
            {
                for (int i = 0; i < AllOf.Length; ++i)
                {
                    AllOf[i].PrepareForUsage(document);
                }
            }

            if (AnyOf != null)
            {
                for (int i = 0; i < AnyOf.Length; ++i)
                {
                    AnyOf[i].PrepareForUsage(document);
                }
            }

            if (OneOf != null)
            {
                for (int i = 0; i < OneOf.Length; ++i)
                {
                    OneOf[i].PrepareForUsage(document);
                }
            }

            if (Properties != null)
            {
                IReadOnlyList<string> keys = Properties.Keys.ToList();
                for (int i = 0; i < keys.Count; ++i)
                {
                    Properties[keys[i]]?.PrepareForUsage(document);
                }
            }

            Items?.PrepareForUsage(document);
            Not?.PrepareForUsage(document);

            if (Required?.Option1 != null)
            {
                if (Properties != null)
                {
                    foreach (string propertyName in Required.Option1)
                    {
                        if (Properties.TryGetValue(propertyName, out Schema value))
                        {
                            value.Required = true;
                        }
                    }
                }

                Required = false;
            }
        }

        [JsonConverter(typeof(EitherConverter<Schema, bool>))]
        public Either<Schema, bool> AdditionalProperties { get; set; }

        public Schema[] AllOf { get; set; }

        public Schema[] AnyOf { get; set; }

        public object Default { get; set; }

        public string Description { get; set; }

        public object[] Enum { get; set; }

        public object Example { get; set; }

        public bool ExclusiveMaximum { get; set; }

        public bool ExclusiveMinimum { get; set; }

        public string Format { get; set; }

        public Schema Items { get; set; }

        public double? Maximum { get; set; }

        public double? Minimum { get; set; }

        public int? MaxItems { get; set; }

        public int? MinItems { get; set; }

        public int? MaxLength { get; set; }

        public int? MinLength { get; set; }

        public int? MaxProperties { get; set; }

        public int? MinProperties { get; set; }

        public double? MultipleOf { get; set; }

        public Schema Not { get; set; }

        public Schema[] OneOf { get; set; }

        public string Pattern { get; set; }

        public Dictionary<string, Schema> Properties { get; set; }

        [JsonConverter(typeof(EitherConverter<string[], bool>))]
        public Either<string[], bool> Required { get; set; }

        public string Title { get; set; }

        public string Type { get; set; }

        public bool UniqueItems { get; set; }
    }
}
