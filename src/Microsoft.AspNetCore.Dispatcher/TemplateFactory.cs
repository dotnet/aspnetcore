// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Dispatcher
{
    public abstract class TemplateFactory
    {
        public Template GetTemplate(object values)
        {
            return GetTemplateFromKey(new DispatcherValueCollection(values));
        }

        public abstract Template GetTemplateFromKey<TKey>(TKey key) where TKey : class;
    }
}
