// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Dispatcher
{
    public class TemplateAddress : Address, ITemplateAddress
    {
        public TemplateAddress(string template, object values, params object[] metadata)
            : this(template, values, null, metadata)
        {
        }

        public TemplateAddress(string template, object values, string displayName, params object[] metadata)
        {
            if (template == null)
            {
                throw new ArgumentNullException(nameof(template));
            }

            if (metadata == null)
            {
                throw new ArgumentNullException(nameof(metadata));
            }

            Template = template;
            Defaults = new DispatcherValueCollection(values);
            DisplayName = displayName;
            Metadata = metadata.ToArray();
        }

        public override string DisplayName { get; }

        public override IReadOnlyList<object> Metadata { get; }

        public string Template { get; }

        public DispatcherValueCollection Defaults { get; }
    }
}
