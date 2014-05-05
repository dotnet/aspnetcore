// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Security;
using Microsoft.AspNet.FeatureModel;
using Microsoft.AspNet.HttpFeature;
using Microsoft.AspNet.HttpFeature.Security;
using Microsoft.AspNet.PipelineCore.Infrastructure;
using Microsoft.AspNet.PipelineCore.Security;

namespace Microsoft.AspNet.PipelineCore
{
    public class DefaultHttpContext : HttpContext
    {
        private readonly HttpRequest _request;
        private readonly HttpResponse _response;

        private FeatureReference<IItemsFeature> _items;
        private FeatureReference<IServiceProvidersFeature> _serviceProviders;
        private FeatureReference<IHttpAuthenticationFeature> _authentication;
        private FeatureReference<IHttpRequestLifetimeFeature> _lifetime;
        private IFeatureCollection _features;

        public DefaultHttpContext(IFeatureCollection features)
        {
            _features = features;
            _request = new DefaultHttpRequest(this, features);
            _response = new DefaultHttpResponse(this, features);

            _items = FeatureReference<IItemsFeature>.Default;
            _serviceProviders = FeatureReference<IServiceProvidersFeature>.Default;
            _authentication = FeatureReference<IHttpAuthenticationFeature>.Default;
        }

        IItemsFeature ItemsFeature
        {
            get { return _items.Fetch(_features) ?? _items.Update(_features, new ItemsFeature()); }
        }

        IServiceProvidersFeature ServiceProvidersFeature
        {
            get { return _serviceProviders.Fetch(_features) ?? _serviceProviders.Update(_features, new ServiceProvidersFeature()); }
        }

        private IHttpAuthenticationFeature HttpAuthenticationFeature
        {
            get { return _authentication.Fetch(_features) ?? _authentication.Update(_features, new HttpAuthenticationFeature()); }
        }

        private IHttpRequestLifetimeFeature LifetimeFeature
        {
            get { return _lifetime.Fetch(_features); }
        }

        public override HttpRequest Request { get { return _request; } }

        public override HttpResponse Response { get { return _response; } }

        public override ClaimsPrincipal User
        {
            get
            {
                var user = HttpAuthenticationFeature.User;
                if (user == null)
                {
                    user = new ClaimsPrincipal(new ClaimsIdentity());
                    HttpAuthenticationFeature.User = user;
                }
                return user;
            }
            set { HttpAuthenticationFeature.User = value; }
        }

        public override IDictionary<object, object> Items
        {
            get { return ItemsFeature.Items; }
        }

        public override IServiceProvider ApplicationServices
        {
            get { return ServiceProvidersFeature.ApplicationServices; }
            set { ServiceProvidersFeature.ApplicationServices = value; }
        }

        public override IServiceProvider RequestServices
        {
            get { return ServiceProvidersFeature.RequestServices; }
            set { ServiceProvidersFeature.RequestServices = value; }
        }

        public int Revision { get { return _features.Revision; } }

        public override CancellationToken OnRequestAborted
        {
            get
            {
                var lifetime = LifetimeFeature;
                if (lifetime != null)
                {
                    return lifetime.OnRequestAborted;
                }
                return CancellationToken.None;
            }
        }

        public override void Abort()
        {
            var lifetime = LifetimeFeature;
            if (lifetime != null)
            {
                lifetime.Abort();
            }
        }

        public override void Dispose()
        {
            // REVIEW: is this necessary? is the environment "owned" by the context?
            _features.Dispose();
        }

        public override object GetFeature(Type type)
        {
            object value;
            return _features.TryGetValue(type, out value) ? value : null;
        }

        public override void SetFeature(Type type, object instance)
        {
            _features[type] = instance;
        }

        public override IEnumerable<AuthenticationDescription> GetAuthenticationTypes()
        {
            var handler = HttpAuthenticationFeature.Handler;
            if (handler == null)
            {
                return new AuthenticationDescription[0];
            }

            var authTypeContext = new AuthTypeContext();
            handler.GetDescriptions(authTypeContext);
            return authTypeContext.Results;
        }

        public override IEnumerable<AuthenticationResult> Authenticate(IList<string> authenticationTypes)
        {
            if (authenticationTypes == null)
            {
                throw new ArgumentNullException();
            }
            var handler = HttpAuthenticationFeature.Handler;

            var authenticateContext = new AuthenticateContext(authenticationTypes);
            if (handler != null)
            {
                handler.Authenticate(authenticateContext);
            }

            // Verify all types ack'd
            IEnumerable<string> leftovers = authenticationTypes.Except(authenticateContext.Accepted);
            if (leftovers.Any())
            {
                throw new InvalidOperationException("The following authentication types were not accepted: " + string.Join(", ", leftovers));
            }

            return authenticateContext.Results;
        }

        public override async Task<IEnumerable<AuthenticationResult>> AuthenticateAsync(IList<string> authenticationTypes)
        {
            if (authenticationTypes == null)
            {
                throw new ArgumentNullException();
            }
            var handler = HttpAuthenticationFeature.Handler;

            var authenticateContext = new AuthenticateContext(authenticationTypes);
            if (handler != null)
            {
                await handler.AuthenticateAsync(authenticateContext);
            }

            // Verify all types ack'd
            IEnumerable<string> leftovers = authenticationTypes.Except(authenticateContext.Accepted);
            if (leftovers.Any())
            {
                throw new InvalidOperationException("The following authentication types were not accepted: " + string.Join(", ", leftovers));
            }

            return authenticateContext.Results;
        }
    }
}
