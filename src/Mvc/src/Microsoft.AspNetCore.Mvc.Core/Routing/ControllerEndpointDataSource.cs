// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Mvc.Routing
{
    internal class ControllerEndpointDataSource : EndpointDataSource, IEndpointConventionBuilder
    {
        private readonly ControllerEndpointFactory _factory;

        // We need the part manager because we want to add assemblies to the part manager
        // when we see them. While controller discovery uses the endpoint routing mechanism
        // for discovery, other MVC features still use the part manager.
        private readonly ApplicationPartManager _partManager;

        // The set of assemblies that we've 'seen' we just use this for tracking, all of the
        // actual discovery is based on the list of types.
        private readonly HashSet<Assembly> _assemblies;

        // This is the list of types we use for actual discovery.
        private readonly HashSet<TypeInfo> _types;

        // This is the list of conventional routes. These apply a cross-product over the
        // non-attribute-routed actions.
        private readonly List<ConventionalRouteEntry> _routes;

        // Conventions are applied at the end, which means that order of conventions matters, but the
        // order of conventions vs something like MapAssembly or MapController does not.
        private readonly List<Action<EndpointModel>> _conventions;

        // Lock used to protect WRITEs and the intialization sequence. Once we do the first round of
        // initialization we don't need to protect reads.
        private readonly object _lock;

        // Stateful things.
        private List<Endpoint> _endpoints;
        private CancellationTokenSource _cancellationTokenSource;
        private IChangeToken _changeToken;

        public ControllerEndpointDataSource(ApplicationPartManager partManager, ControllerEndpointFactory factory)
        {
            if (partManager == null)
            {
                throw new ArgumentNullException(nameof(partManager));
            }

            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            _factory = factory;
            _partManager = partManager;

            _assemblies = new HashSet<Assembly>();
            _types = new HashSet<TypeInfo>();
            _routes = new List<ConventionalRouteEntry>();
            _conventions = new List<Action<EndpointModel>>();
            _lock = new object();
        }

        // In traditional conventional routing setup, the routes defined by a user have a static order
        // defined by how they are added into the list. We would like to maintain the same order when building
        // up the endpoints too.
        //
        // Start with an order of '1' for conventional routes as attribute routes have a default order of '0'.
        // This is for scenarios dealing with migrating existing Router based code to Endpoint Routing world.
        public int NextConventionalRouteOrder => _routes.Count + 1;

        public override IReadOnlyList<Endpoint> Endpoints
        {
            get
            {
                Initialize();
                Debug.Assert(_changeToken != null);
                Debug.Assert(_endpoints != null);
                return _endpoints;
            }
        }

        public override IChangeToken GetChangeToken()
        {
            Initialize();
            Debug.Assert(_changeToken != null);
            Debug.Assert(_endpoints != null);
            return _changeToken;
        }

        private void Initialize()
        {
            if (_endpoints == null)
            {
                lock (_lock)
                {
                    if (_endpoints == null)
                    {
                        UpdateEndpoints();
                    }
                }
            }
        }

        public void Apply(Action<EndpointModel> convention)
        {
            if (convention == null)
            {
                throw new ArgumentNullException(nameof(convention));
            }

            lock (_lock)
            {
                _conventions.Add(convention);
            }
        }

        public IEndpointConventionBuilder AddApplicationAssemblies()
        {
            lock (_lock)
            {
                var parts = _partManager.ApplicationParts;
                for (var i = 0; i < parts.Count; i++)
                {
                    // For now we limit this to AssemblyPart and subclasses. The reason is
                    // that we need to know the assembly to keep track of it and de-dupe it with other
                    // assemblies. In theory we could support arbitrary parts here, but it's not clear
                    // that anyone needs that.
                    if (parts[i] is AssemblyPart assemblyPart)
                    {
                        // TODO use interface
                        if (_assemblies.Add(assemblyPart.Assembly))
                        {
                            foreach (var type in assemblyPart.Types)
                            {
                                _types.Add(type);
                            }
                        }
                    }
                }
            }

            // No filter needed since we want a convention builder that applies to everything.
            return this;
        }

        public IEndpointConventionBuilder AddAssembly(Assembly assembly)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            lock (_lock)
            {
                if (_assemblies.Add(assembly))
                {
                    // The features that use application part manager are expected/required to resolve
                    // conflicts. We don't need to check of the assembly is already registered.
                    //
                    // This is the boilerplate required to add an assembly and all of the related
                    // stuff to the part manager. 
                    var partFactory = ApplicationPartFactory.GetApplicationPartFactory(assembly);
                    foreach (var part in partFactory.GetApplicationParts(assembly))
                    {
                        _partManager.ApplicationParts.Add(part);
                    }

                    var relatedAssemblies = RelatedAssemblyAttribute.GetRelatedAssemblies(assembly, throwOnError: false);
                    foreach (var relatedAssembly in relatedAssemblies)
                    {
                        foreach (var part in partFactory.GetApplicationParts(assembly))
                        {
                            _partManager.ApplicationParts.Add(part);
                        }
                    }
                }

                // Note: we don't use application parts to FIND the controller types. That would enable
                // lots of things that wouldn't work with the convention builder returned from this method.
                //
                // If a user needs to use the part manager for extensibility of controller discovery,
                // the only supported way to do that is through MapApplication().
                foreach (var type in assembly.DefinedTypes)
                {
                    _types.Add(type);
                }
            }

            return new ControllerAssemblyEndpointConventionBuilder(this, assembly);
        }

        public IEndpointConventionBuilder AddType(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            lock (_lock)
            {
                var assembly = type.Assembly;
                if (_assemblies.Add(assembly))
                {
                    // The features that use application part manager are expected/required to resolve
                    // conflicts. We don't need to check of the assembly is already registered.
                    //
                    // This is the boilerplate required to add an assembly and all of the related
                    // stuff to the part manager. 
                    var partFactory = ApplicationPartFactory.GetApplicationPartFactory(assembly);
                    foreach (var part in partFactory.GetApplicationParts(assembly))
                    {
                        _partManager.ApplicationParts.Add(part);
                    }

                    var relatedAssemblies = RelatedAssemblyAttribute.GetRelatedAssemblies(assembly, throwOnError: false);
                    foreach (var relatedAssembly in relatedAssemblies)
                    {
                        foreach (var part in partFactory.GetApplicationParts(assembly))
                        {
                            _partManager.ApplicationParts.Add(part);
                        }
                    }
                }

                _types.Add(type.GetTypeInfo());
            }

            return new ControllerTypeEndpointConventionBuilder(this, type);
        }

        public void AddConventionalRoute(ConventionalRouteEntry route)
        {
            if (route == null)
            {
                throw new ArgumentNullException(nameof(route));
            }

            lock (_lock)
            {
                _routes.Add(route);
            }
        }
        
        private void UpdateEndpoints()
        {
            lock (_lock)
            {
                var endpoints = _factory.CreateEndpoints(_types, _routes, _conventions);

                // See comments in DefaultActionDescriptorCollectionProvider. These steps are done
                // in a specific order to ensure callers always see a consistent state.

                // Step 1 - capture old token
                var oldCancellationTokenSource = _cancellationTokenSource;

                // Step 2 - update endpoints
                _endpoints = endpoints;

                // Step 3 - create new change token
                _cancellationTokenSource = new CancellationTokenSource();
                _changeToken = new CancellationChangeToken(_cancellationTokenSource.Token);

                // Step 4 - trigger old token
                oldCancellationTokenSource?.Cancel();
            }
        }

        private class ControllerAssemblyEndpointConventionBuilder : IEndpointConventionBuilder
        {
            private readonly ControllerEndpointDataSource _dataSource;
            private readonly Assembly _assembly;

            public ControllerAssemblyEndpointConventionBuilder(ControllerEndpointDataSource dataSource, Assembly assembly)
            {
                _dataSource = dataSource;
                _assembly = assembly;
            }

            public void Apply(Action<EndpointModel> convention)
            {
                if (convention == null)
                {
                    throw new ArgumentNullException(nameof(convention));
                }

                _dataSource.Apply((model) =>
                {
                    if (model is ControllerActionEndpointModel actionModel && actionModel.ControllerType.Assembly == _assembly)
                    {
                        convention(model);
                    }
                });
            }
        }

        private class ControllerTypeEndpointConventionBuilder : IEndpointConventionBuilder
        {
            private readonly ControllerEndpointDataSource _dataSource;
            private readonly Type _type;

            public ControllerTypeEndpointConventionBuilder(ControllerEndpointDataSource dataSource, Type type)
            {
                _dataSource = dataSource;
                _type = type;
            }

            public void Apply(Action<EndpointModel> convention)
            {
                if (convention == null)
                {
                    throw new ArgumentNullException(nameof(convention));
                }

                _dataSource.Apply((model) =>
                {
                    if (model is ControllerActionEndpointModel actionModel && actionModel.ControllerType == _type)
                    {
                        convention(model);
                    }
                });
            }
        }
    }
}
