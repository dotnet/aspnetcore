// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Formatters.Xml;

public class PersonWrapperProviderFactory : IWrapperProviderFactory
{
    public IWrapperProvider GetProvider(WrapperProviderContext context)
    {
        if (context.DeclaredType == typeof(Person))
        {
            return new PersonWrapperProvider();
        }

        return null;
    }
}
