// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace FormatterWebSite
{
    // A System.Security.Principal.SecurityIdentifier like type that works on xplat
    public class RecursiveIdentifier : IValidatableObject
    {
        public RecursiveIdentifier(string identifier)
        {
            Value = identifier;
        }

        public string Value { get; }

        public RecursiveIdentifier AccountIdentifier => new RecursiveIdentifier(Value);

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            return Enumerable.Empty<ValidationResult>();
        }
    }
}