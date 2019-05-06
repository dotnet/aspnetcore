// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Internal;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extensions methods to configure an <see cref="IServiceCollection"/> for <see cref="IHttpClientFactory"/>.
    /// </summary>
    public static class HttpClientFactoryServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the <see cref="IHttpClientFactory"/> and related services to the <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddHttpClient(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddLogging();
            services.AddOptions();

            //
            // Core abstractions
            //
            services.TryAddTransient<HttpMessageHandlerBuilder, DefaultHttpMessageHandlerBuilder>();
            services.TryAddSingleton<DefaultHttpClientFactory>();
            services.TryAddSingleton<IHttpClientFactory>(serviceProvider => serviceProvider.GetRequiredService<DefaultHttpClientFactory>());
            services.TryAddSingleton<IHttpMessageHandlerFactory>(serviceProvider => serviceProvider.GetRequiredService<DefaultHttpClientFactory>());

            //
            // Typed Clients
            //
            services.TryAdd(ServiceDescriptor.Transient(typeof(ITypedHttpClientFactory<>), typeof(DefaultTypedHttpClientFactory<>)));
            services.TryAdd(ServiceDescriptor.Transient(typeof(DefaultTypedHttpClientFactory<>.Cache), typeof(DefaultTypedHttpClientFactory<>.Cache)));

            //
            // Misc infrastructure
            //
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IHttpMessageHandlerBuilderFilter, LoggingHttpMessageHandlerBuilderFilter>());

            // This is used to track state and report errors **DURING** service registration. This has to be an instance
            // because we access it by reaching into the service collection.
            services.TryAddSingleton(new HttpClientMappingRegistry());

            return services;
        }

        /// <summary>
        /// Adds the <see cref="IHttpClientFactory"/> and related services to the <see cref="IServiceCollection"/> and configures
        /// a named <see cref="HttpClient"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="name">The logical name of the <see cref="HttpClient"/> to configure.</param>
        /// <returns>An <see cref="IHttpClientBuilder"/> that can be used to configure the client.</returns>
        /// <remarks>
        /// <para>
        /// <see cref="HttpClient"/> instances that apply the provided configuration can be retrieved using 
        /// <see cref="IHttpClientFactory.CreateClient(string)"/> and providing the matching name.
        /// </para>
        /// <para>
        /// Use <see cref="Options.Options.DefaultName"/> as the name to configure the default client.
        /// </para>
        /// </remarks>
        public static IHttpClientBuilder AddHttpClient(this IServiceCollection services, string name)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            AddHttpClient(services);

            return new DefaultHttpClientBuilder(services, name);
        }

        /// <summary>
        /// Adds the <see cref="IHttpClientFactory"/> and related services to the <see cref="IServiceCollection"/> and configures
        /// a named <see cref="HttpClient"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="name">The logical name of the <see cref="HttpClient"/> to configure.</param>
        /// <param name="configureClient">A delegate that is used to configure an <see cref="HttpClient"/>.</param>
        /// <returns>An <see cref="IHttpClientBuilder"/> that can be used to configure the client.</returns>
        /// <remarks>
        /// <para>
        /// <see cref="HttpClient"/> instances that apply the provided configuration can be retrieved using 
        /// <see cref="IHttpClientFactory.CreateClient(string)"/> and providing the matching name.
        /// </para>
        /// <para>
        /// Use <see cref="Options.Options.DefaultName"/> as the name to configure the default client.
        /// </para>
        /// </remarks>
        public static IHttpClientBuilder AddHttpClient(this IServiceCollection services, string name, Action<HttpClient> configureClient)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (configureClient == null)
            {
                throw new ArgumentNullException(nameof(configureClient));
            }

            AddHttpClient(services);

            var builder = new DefaultHttpClientBuilder(services, name);
            builder.ConfigureHttpClient(configureClient);
            return builder;
        }

        /// <summary>
        /// Adds the <see cref="IHttpClientFactory"/> and related services to the <see cref="IServiceCollection"/> and configures
        /// a named <see cref="HttpClient"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="name">The logical name of the <see cref="HttpClient"/> to configure.</param>
        /// <param name="configureClient">A delegate that is used to configure an <see cref="HttpClient"/>.</param>
        /// <returns>An <see cref="IHttpClientBuilder"/> that can be used to configure the client.</returns>
        /// <remarks>
        /// <para>
        /// <see cref="HttpClient"/> instances that apply the provided configuration can be retrieved using 
        /// <see cref="IHttpClientFactory.CreateClient(string)"/> and providing the matching name.
        /// </para>
        /// <para>
        /// Use <see cref="Options.Options.DefaultName"/> as the name to configure the default client.
        /// </para>
        /// </remarks>
        public static IHttpClientBuilder AddHttpClient(this IServiceCollection services, string name, Action<IServiceProvider, HttpClient> configureClient)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (configureClient == null)
            {
                throw new ArgumentNullException(nameof(configureClient));
            }

            AddHttpClient(services);

            var builder = new DefaultHttpClientBuilder(services, name);
            builder.ConfigureHttpClient(configureClient);
            return builder;
        }

        /// <summary>
        /// Adds the <see cref="IHttpClientFactory"/> and related services to the <see cref="IServiceCollection"/> and configures
        /// a binding between the <typeparamref name="TClient"/> type and a named <see cref="HttpClient"/>. The client name
        /// will be set to the full name of <typeparamref name="TClient"/>.
        /// </summary>
        /// <typeparam name="TClient">
        /// The type of the typed client. They type specified will be registered in the service collection as
        /// a transient service. See <see cref="ITypedHttpClientFactory{TClient}" /> for more details about authoring typed clients.
        /// </typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <returns>An <see cref="IHttpClientBuilder"/> that can be used to configure the client.</returns>
        /// <remarks>
        /// <para>
        /// <see cref="HttpClient"/> instances that apply the provided configuration can be retrieved using 
        /// <see cref="IHttpClientFactory.CreateClient(string)"/> and providing the matching name.
        /// </para>
        /// <para>
        /// <typeparamref name="TClient"/> instances constructed with the appropriate <see cref="HttpClient" />
        /// can be retrieved from <see cref="IServiceProvider.GetService(Type)" /> (and related methods) by providing
        /// <typeparamref name="TClient"/> as the service type. 
        /// </para>
        /// </remarks>
        public static IHttpClientBuilder AddHttpClient<TClient>(this IServiceCollection services)
            where TClient : class
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            AddHttpClient(services);

            var name = TypeNameHelper.GetTypeDisplayName(typeof(TClient), fullName: false);
            var builder = new DefaultHttpClientBuilder(services, name);
            builder.AddTypedClient<TClient>();
            return builder;
        }

        /// <summary>
        /// Adds the <see cref="IHttpClientFactory"/> and related services to the <see cref="IServiceCollection"/> and configures
        /// a binding between the <typeparamref name="TClient" /> type and a named <see cref="HttpClient"/>. The client name will
        /// be set to the type name of <typeparamref name="TClient"/>.
        /// </summary>
        /// <typeparam name="TClient">
        /// The type of the typed client. They type specified will be registered in the service collection as
        /// a transient service. See <see cref="ITypedHttpClientFactory{TClient}" /> for more details about authoring typed clients.
        /// </typeparam>
        /// <typeparam name="TImplementation">
        /// The implementation type of the typed client. They type specified will be instantiated by the
        /// <see cref="ITypedHttpClientFactory{TImplementation}"/>
        /// </typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <returns>An <see cref="IHttpClientBuilder"/> that can be used to configure the client.</returns>
        /// <remarks>
        /// <para>
        /// <see cref="HttpClient"/> instances that apply the provided configuration can be retrieved using 
        /// <see cref="IHttpClientFactory.CreateClient(string)"/> and providing the matching name.
        /// </para>
        /// <para>
        /// <typeparamref name="TClient"/> instances constructed with the appropriate <see cref="HttpClient" />
        /// can be retrieved from <see cref="IServiceProvider.GetService(Type)" /> (and related methods) by providing
        /// <typeparamref name="TClient"/> as the service type. 
        /// </para>
        /// </remarks>
        public static IHttpClientBuilder AddHttpClient<TClient, TImplementation>(this IServiceCollection services)
            where TClient : class
            where TImplementation : class, TClient
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            AddHttpClient(services);

            var name = TypeNameHelper.GetTypeDisplayName(typeof(TClient), fullName: false);
            var builder = new DefaultHttpClientBuilder(services, name);
            builder.AddTypedClient<TClient, TImplementation>();
            return builder;
        }

        /// <summary>
        /// Adds the <see cref="IHttpClientFactory"/> and related services to the <see cref="IServiceCollection"/> and configures
        /// a binding between the <typeparamref name="TClient"/> type and a named <see cref="HttpClient"/>.
        /// </summary>
        /// <typeparam name="TClient">
        /// The type of the typed client. They type specified will be registered in the service collection as
        /// a transient service. See <see cref="ITypedHttpClientFactory{TClient}" /> for more details about authoring typed clients.
        /// </typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="name">The logical name of the <see cref="HttpClient"/> to configure.</param>
        /// <returns>An <see cref="IHttpClientBuilder"/> that can be used to configure the client.</returns>
        /// <remarks>
        /// <para>
        /// <see cref="HttpClient"/> instances that apply the provided configuration can be retrieved using 
        /// <see cref="IHttpClientFactory.CreateClient(string)"/> and providing the matching name.
        /// </para>
        /// <para>
        /// <typeparamref name="TClient"/> instances constructed with the appropriate <see cref="HttpClient" />
        /// can be retrieved from <see cref="IServiceProvider.GetService(Type)" /> (and related methods) by providing
        /// <typeparamref name="TClient"/> as the service type. 
        /// </para>
        /// <para>
        /// Use <see cref="Options.Options.DefaultName"/> as the name to configure the default client.
        /// </para>
        /// </remarks>
        public static IHttpClientBuilder AddHttpClient<TClient>(this IServiceCollection services, string name)
            where TClient : class
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            AddHttpClient(services);

            var builder = new DefaultHttpClientBuilder(services, name);
            builder.AddTypedClient<TClient>();
            return builder;
        }

        /// <summary>
        /// Adds the <see cref="IHttpClientFactory"/> and related services to the <see cref="IServiceCollection"/> and configures
        /// a binding between the <typeparamref name="TClient" /> type and a named <see cref="HttpClient"/>. The client name will
        /// be set to the type name of <typeparamref name="TClient"/>.
        /// </summary>
        /// <typeparam name="TClient">
        /// The type of the typed client. They type specified will be registered in the service collection as
        /// a transient service. See <see cref="ITypedHttpClientFactory{TClient}" /> for more details about authoring typed clients.
        /// </typeparam>
        /// <typeparam name="TImplementation">
        /// The implementation type of the typed client. They type specified will be instantiated by the
        /// <see cref="ITypedHttpClientFactory{TImplementation}"/>
        /// </typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="name">The logical name of the <see cref="HttpClient"/> to configure.</param>
        /// <returns>An <see cref="IHttpClientBuilder"/> that can be used to configure the client.</returns>
        /// <remarks>
        /// <para>
        /// <see cref="HttpClient"/> instances that apply the provided configuration can be retrieved using 
        /// <see cref="IHttpClientFactory.CreateClient(string)"/> and providing the matching name.
        /// </para>
        /// <para>
        /// <typeparamref name="TClient"/> instances constructed with the appropriate <see cref="HttpClient" />
        /// can be retrieved from <see cref="IServiceProvider.GetService(Type)" /> (and related methods) by providing
        /// <typeparamref name="TClient"/> as the service type. 
        /// </para>
        /// <para>
        /// Use <see cref="Options.Options.DefaultName"/> as the name to configure the default client.
        /// </para>
        /// </remarks>
        public static IHttpClientBuilder AddHttpClient<TClient, TImplementation>(this IServiceCollection services, string name)
            where TClient : class
            where TImplementation : class, TClient
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            AddHttpClient(services);

            var builder = new DefaultHttpClientBuilder(services, name);
            builder.AddTypedClient<TClient, TImplementation>();
            return builder;
        }

        /// <summary>
        /// Adds the <see cref="IHttpClientFactory"/> and related services to the <see cref="IServiceCollection"/> and configures
        /// a binding between the <typeparamref name="TClient" /> type and a named <see cref="HttpClient"/>. The client name will
        /// be set to the type name of <typeparamref name="TClient"/>.
        /// </summary>
        /// <typeparam name="TClient">
        /// The type of the typed client. They type specified will be registered in the service collection as
        /// a transient service. See <see cref="ITypedHttpClientFactory{TClient}" /> for more details about authoring typed clients.
        /// </typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="configureClient">A delegate that is used to configure an <see cref="HttpClient"/>.</param>
        /// <returns>An <see cref="IHttpClientBuilder"/> that can be used to configure the client.</returns>
        /// <remarks>
        /// <para>
        /// <see cref="HttpClient"/> instances that apply the provided configuration can be retrieved using 
        /// <see cref="IHttpClientFactory.CreateClient(string)"/> and providing the matching name.
        /// </para>
        /// <para>
        /// <typeparamref name="TClient"/> instances constructed with the appropriate <see cref="HttpClient" />
        /// can be retrieved from <see cref="IServiceProvider.GetService(Type)" /> (and related methods) by providing
        /// <typeparamref name="TClient"/> as the service type. 
        /// </para>
        /// </remarks>
        public static IHttpClientBuilder AddHttpClient<TClient>(this IServiceCollection services, Action<HttpClient> configureClient)
            where TClient : class
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configureClient == null)
            {
                throw new ArgumentNullException(nameof(configureClient));
            }

            AddHttpClient(services);

            var name = TypeNameHelper.GetTypeDisplayName(typeof(TClient), fullName: false);
            var builder = new DefaultHttpClientBuilder(services, name);
            builder.ConfigureHttpClient(configureClient);
            builder.AddTypedClient<TClient>();
            return builder;
        }

        /// <summary>
        /// Adds the <see cref="IHttpClientFactory"/> and related services to the <see cref="IServiceCollection"/> and configures
        /// a binding between the <typeparamref name="TClient" /> type and a named <see cref="HttpClient"/>. The client name will
        /// be set to the type name of <typeparamref name="TClient"/>.
        /// </summary>
        /// <typeparam name="TClient">
        /// The type of the typed client. They type specified will be registered in the service collection as
        /// a transient service. See <see cref="ITypedHttpClientFactory{TClient}" /> for more details about authoring typed clients.
        /// </typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="configureClient">A delegate that is used to configure an <see cref="HttpClient"/>.</param>
        /// <returns>An <see cref="IHttpClientBuilder"/> that can be used to configure the client.</returns>
        /// <remarks>
        /// <para>
        /// <see cref="HttpClient"/> instances that apply the provided configuration can be retrieved using 
        /// <see cref="IHttpClientFactory.CreateClient(string)"/> and providing the matching name.
        /// </para>
        /// <para>
        /// <typeparamref name="TClient"/> instances constructed with the appropriate <see cref="HttpClient" />
        /// can be retrieved from <see cref="IServiceProvider.GetService(Type)" /> (and related methods) by providing
        /// <typeparamref name="TClient"/> as the service type. 
        /// </para>
        /// </remarks>
        public static IHttpClientBuilder AddHttpClient<TClient>(this IServiceCollection services, Action<IServiceProvider, HttpClient> configureClient)
            where TClient : class
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configureClient == null)
            {
                throw new ArgumentNullException(nameof(configureClient));
            }

            AddHttpClient(services);

            var name = TypeNameHelper.GetTypeDisplayName(typeof(TClient), fullName: false);
            var builder = new DefaultHttpClientBuilder(services, name);
            builder.ConfigureHttpClient(configureClient);
            builder.AddTypedClient<TClient>();
            return builder;
        }

        /// <summary>
        /// Adds the <see cref="IHttpClientFactory"/> and related services to the <see cref="IServiceCollection"/> and configures
        /// a binding between the <typeparamref name="TClient" /> type and a named <see cref="HttpClient"/>. The client name will
        /// be set to the type name of <typeparamref name="TClient"/>.
        /// </summary>
        /// <typeparam name="TClient">
        /// The type of the typed client. They type specified will be registered in the service collection as
        /// a transient service. See <see cref="ITypedHttpClientFactory{TClient}" /> for more details about authoring typed clients.
        /// </typeparam>
        /// <typeparam name="TImplementation">
        /// The implementation type of the typed client. They type specified will be instantiated by the
        /// <see cref="ITypedHttpClientFactory{TImplementation}"/>
        /// </typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="configureClient">A delegate that is used to configure an <see cref="HttpClient"/>.</param>
        /// <returns>An <see cref="IHttpClientBuilder"/> that can be used to configure the client.</returns>
        /// <remarks>
        /// <para>
        /// <see cref="HttpClient"/> instances that apply the provided configuration can be retrieved using 
        /// <see cref="IHttpClientFactory.CreateClient(string)"/> and providing the matching name.
        /// </para>
        /// <para>
        /// <typeparamref name="TClient"/> instances constructed with the appropriate <see cref="HttpClient" />
        /// can be retrieved from <see cref="IServiceProvider.GetService(Type)" /> (and related methods) by providing
        /// <typeparamref name="TClient"/> as the service type. 
        /// </para>
        /// </remarks>
        public static IHttpClientBuilder AddHttpClient<TClient, TImplementation>(this IServiceCollection services, Action<HttpClient> configureClient)
            where TClient : class
            where TImplementation : class, TClient
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configureClient == null)
            {
                throw new ArgumentNullException(nameof(configureClient));
            }

            AddHttpClient(services);

            var name = TypeNameHelper.GetTypeDisplayName(typeof(TClient), fullName: false);
            var builder = new DefaultHttpClientBuilder(services, name);
            builder.ConfigureHttpClient(configureClient);
            builder.AddTypedClient<TClient, TImplementation>();
            return builder;
        }

        /// <summary>
        /// Adds the <see cref="IHttpClientFactory"/> and related services to the <see cref="IServiceCollection"/> and configures
        /// a binding between the <typeparamref name="TClient" /> type and a named <see cref="HttpClient"/>. The client name will
        /// be set to the type name of <typeparamref name="TClient"/>.
        /// </summary>
        /// <typeparam name="TClient">
        /// The type of the typed client. They type specified will be registered in the service collection as
        /// a transient service. See <see cref="ITypedHttpClientFactory{TClient}" /> for more details about authoring typed clients.
        /// </typeparam>
        /// <typeparam name="TImplementation">
        /// The implementation type of the typed client. They type specified will be instantiated by the
        /// <see cref="ITypedHttpClientFactory{TImplementation}"/>
        /// </typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="configureClient">A delegate that is used to configure an <see cref="HttpClient"/>.</param>
        /// <returns>An <see cref="IHttpClientBuilder"/> that can be used to configure the client.</returns>
        /// <remarks>
        /// <para>
        /// <see cref="HttpClient"/> instances that apply the provided configuration can be retrieved using 
        /// <see cref="IHttpClientFactory.CreateClient(string)"/> and providing the matching name.
        /// </para>
        /// <para>
        /// <typeparamref name="TClient"/> instances constructed with the appropriate <see cref="HttpClient" />
        /// can be retrieved from <see cref="IServiceProvider.GetService(Type)" /> (and related methods) by providing
        /// <typeparamref name="TClient"/> as the service type. 
        /// </para>
        /// </remarks>
        public static IHttpClientBuilder AddHttpClient<TClient, TImplementation>(this IServiceCollection services, Action<IServiceProvider, HttpClient> configureClient)
            where TClient : class
            where TImplementation : class, TClient
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configureClient == null)
            {
                throw new ArgumentNullException(nameof(configureClient));
            }

            AddHttpClient(services);

            var name = TypeNameHelper.GetTypeDisplayName(typeof(TClient), fullName: false);
            var builder = new DefaultHttpClientBuilder(services, name);
            builder.ConfigureHttpClient(configureClient);
            builder.AddTypedClient<TClient, TImplementation>();
            return builder;
        }

        /// <summary>
        /// Adds the <see cref="IHttpClientFactory"/> and related services to the <see cref="IServiceCollection"/> and configures
        /// a binding between the <typeparamref name="TClient" /> type and a named <see cref="HttpClient"/>.
        /// </summary>
        /// <typeparam name="TClient">
        /// The type of the typed client. They type specified will be registered in the service collection as
        /// a transient service. See <see cref="ITypedHttpClientFactory{TClient}" /> for more details about authoring typed clients.
        /// </typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="name">The logical name of the <see cref="HttpClient"/> to configure.</param>
        /// <param name="configureClient">A delegate that is used to configure an <see cref="HttpClient"/>.</param>
        /// <returns>An <see cref="IHttpClientBuilder"/> that can be used to configure the client.</returns>
        /// <remarks>
        /// <para>
        /// <see cref="HttpClient"/> instances that apply the provided configuration can be retrieved using 
        /// <see cref="IHttpClientFactory.CreateClient(string)"/> and providing the matching name.
        /// </para>
        /// <para>
        /// <typeparamref name="TClient"/> instances constructed with the appropriate <see cref="HttpClient" />
        /// can be retrieved from <see cref="IServiceProvider.GetService(Type)" /> (and related methods) by providing
        /// <typeparamref name="TClient"/> as the service type. 
        /// </para>
        /// <para>
        /// Use <see cref="Options.Options.DefaultName"/> as the name to configure the default client.
        /// </para>
        /// </remarks>
        public static IHttpClientBuilder AddHttpClient<TClient>(this IServiceCollection services, string name, Action<HttpClient> configureClient)
            where TClient : class
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (configureClient == null)
            {
                throw new ArgumentNullException(nameof(configureClient));
            }

            AddHttpClient(services);

            var builder = new DefaultHttpClientBuilder(services, name);
            builder.ConfigureHttpClient(configureClient);
            builder.AddTypedClient<TClient>();
            return builder;
        }

        /// <summary>
        /// Adds the <see cref="IHttpClientFactory"/> and related services to the <see cref="IServiceCollection"/> and configures
        /// a binding between the <typeparamref name="TClient" /> type and a named <see cref="HttpClient"/>.
        /// </summary>
        /// <typeparam name="TClient">
        /// The type of the typed client. They type specified will be registered in the service collection as
        /// a transient service. See <see cref="ITypedHttpClientFactory{TClient}" /> for more details about authoring typed clients.
        /// </typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="name">The logical name of the <see cref="HttpClient"/> to configure.</param>
        /// <param name="configureClient">A delegate that is used to configure an <see cref="HttpClient"/>.</param>
        /// <returns>An <see cref="IHttpClientBuilder"/> that can be used to configure the client.</returns>
        /// <remarks>
        /// <para>
        /// <see cref="HttpClient"/> instances that apply the provided configuration can be retrieved using 
        /// <see cref="IHttpClientFactory.CreateClient(string)"/> and providing the matching name.
        /// </para>
        /// <para>
        /// <typeparamref name="TClient"/> instances constructed with the appropriate <see cref="HttpClient" />
        /// can be retrieved from <see cref="IServiceProvider.GetService(Type)" /> (and related methods) by providing
        /// <typeparamref name="TClient"/> as the service type. 
        /// </para>
        /// <para>
        /// Use <see cref="Options.Options.DefaultName"/> as the name to configure the default client.
        /// </para>
        /// </remarks>
        public static IHttpClientBuilder AddHttpClient<TClient>(this IServiceCollection services, string name, Action<IServiceProvider, HttpClient> configureClient)
            where TClient : class
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (configureClient == null)
            {
                throw new ArgumentNullException(nameof(configureClient));
            }

            AddHttpClient(services);

            var builder = new DefaultHttpClientBuilder(services, name);
            builder.ConfigureHttpClient(configureClient);
            builder.AddTypedClient<TClient>();
            return builder;
        }

        /// <summary>
        /// Adds the <see cref="IHttpClientFactory"/> and related services to the <see cref="IServiceCollection"/> and configures
        /// a binding between the <typeparamref name="TClient" /> type and a named <see cref="HttpClient"/>.
        /// </summary>
        /// <typeparam name="TClient">
        /// The type of the typed client. They type specified will be registered in the service collection as
        /// a transient service. See <see cref="ITypedHttpClientFactory{TClient}" /> for more details about authoring typed clients.
        /// </typeparam>
        /// <typeparam name="TImplementation">
        /// The implementation type of the typed client. They type specified will be instantiated by the
        /// <see cref="ITypedHttpClientFactory{TImplementation}"/>
        /// </typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="name">The logical name of the <see cref="HttpClient"/> to configure.</param>
        /// <param name="configureClient">A delegate that is used to configure an <see cref="HttpClient"/>.</param>
        /// <returns>An <see cref="IHttpClientBuilder"/> that can be used to configure the client.</returns>
        /// <remarks>
        /// <para>
        /// <see cref="HttpClient"/> instances that apply the provided configuration can be retrieved using 
        /// <see cref="IHttpClientFactory.CreateClient(string)"/> and providing the matching name.
        /// </para>
        /// <para>
        /// <typeparamref name="TClient"/> instances constructed with the appropriate <see cref="HttpClient" />
        /// can be retrieved from <see cref="IServiceProvider.GetService(Type)" /> (and related methods) by providing
        /// <typeparamref name="TClient"/> as the service type. 
        /// </para>
        /// <para>
        /// Use <see cref="Options.Options.DefaultName"/> as the name to configure the default client.
        /// </para>
        /// </remarks>
        public static IHttpClientBuilder AddHttpClient<TClient, TImplementation>(this IServiceCollection services, string name, Action<HttpClient> configureClient)
            where TClient : class
            where TImplementation : class, TClient
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (configureClient == null)
            {
                throw new ArgumentNullException(nameof(configureClient));
            }

            AddHttpClient(services);

            var builder = new DefaultHttpClientBuilder(services, name);
            builder.ConfigureHttpClient(configureClient);
            builder.AddTypedClient<TClient, TImplementation>();
            return builder;
        }

        /// <summary>
        /// Adds the <see cref="IHttpClientFactory"/> and related services to the <see cref="IServiceCollection"/> and configures
        /// a binding between the <typeparamref name="TClient" /> type and a named <see cref="HttpClient"/>.
        /// </summary>
        /// <typeparam name="TClient">
        /// The type of the typed client. They type specified will be registered in the service collection as
        /// a transient service. See <see cref="ITypedHttpClientFactory{TClient}" /> for more details about authoring typed clients.
        /// </typeparam>
        /// <typeparam name="TImplementation">
        /// The implementation type of the typed client. They type specified will be instantiated by the
        /// <see cref="ITypedHttpClientFactory{TImplementation}"/>
        /// </typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="name">The logical name of the <see cref="HttpClient"/> to configure.</param>
        /// <param name="configureClient">A delegate that is used to configure an <see cref="HttpClient"/>.</param>
        /// <returns>An <see cref="IHttpClientBuilder"/> that can be used to configure the client.</returns>
        /// <remarks>
        /// <para>
        /// <see cref="HttpClient"/> instances that apply the provided configuration can be retrieved using 
        /// <see cref="IHttpClientFactory.CreateClient(string)"/> and providing the matching name.
        /// </para>
        /// <para>
        /// <typeparamref name="TClient"/> instances constructed with the appropriate <see cref="HttpClient" />
        /// can be retrieved from <see cref="IServiceProvider.GetService(Type)" /> (and related methods) by providing
        /// <typeparamref name="TClient"/> as the service type. 
        /// </para>
        /// <para>
        /// Use <see cref="Options.Options.DefaultName"/> as the name to configure the default client.
        /// </para>
        /// </remarks>
        public static IHttpClientBuilder AddHttpClient<TClient, TImplementation>(this IServiceCollection services, string name, Action<IServiceProvider, HttpClient> configureClient)
            where TClient : class
            where TImplementation : class, TClient
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (configureClient == null)
            {
                throw new ArgumentNullException(nameof(configureClient));
            }

            AddHttpClient(services);

            var builder = new DefaultHttpClientBuilder(services, name);
            builder.ConfigureHttpClient(configureClient);
            builder.AddTypedClient<TClient, TImplementation>();
            return builder;
        }
    }
}
