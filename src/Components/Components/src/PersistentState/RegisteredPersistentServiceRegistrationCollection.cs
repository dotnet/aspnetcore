// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Infrastructure;

internal class RegisteredPersistentServiceRegistrationCollection(IEnumerable<IPersistentServiceRegistration> registrations)
{
    private readonly IEnumerable<IPersistentServiceRegistration> _registrations =
        PersistentServicesRegistry.ResolveRegistrations(registrations);

    public IEnumerable<IPersistentServiceRegistration> Registrations => _registrations;
}
