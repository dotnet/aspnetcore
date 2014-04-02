using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.Abstractions.Security;
using Microsoft.AspNet.FeatureModel;
using Microsoft.AspNet.HttpFeature.Security;
using Microsoft.AspNet.PipelineCore.Infrastructure;
using Microsoft.AspNet.PipelineCore.Security;

namespace Microsoft.AspNet.PipelineCore
{
    public class DefaultHttpContext : HttpContext
    {
        private readonly HttpRequest _request;
        private readonly HttpResponse _response;

        private FeatureReference<ICanHasItems> _canHasItems;
        private FeatureReference<ICanHasServiceProviders> _canHasServiceProviders;
        private FeatureReference<IHttpAuthentication> _authentication;
        private IFeatureCollection _features;

        public DefaultHttpContext(IFeatureCollection features)
        {
            _features = features;
            _request = new DefaultHttpRequest(this, features);
            _response = new DefaultHttpResponse(this, features);

            _canHasItems = FeatureReference<ICanHasItems>.Default;
            _canHasServiceProviders = FeatureReference<ICanHasServiceProviders>.Default;
            _authentication = FeatureReference<IHttpAuthentication>.Default;
        }

        ICanHasItems CanHasItems
        {
            get { return _canHasItems.Fetch(_features) ?? _canHasItems.Update(_features, new DefaultCanHasItems()); }
        }

        ICanHasServiceProviders CanHasServiceProviders
        {
            get { return _canHasServiceProviders.Fetch(_features) ?? _canHasServiceProviders.Update(_features, new DefaultCanHasServiceProviders()); }
        }

        private IHttpAuthentication HttpAuthentication
        {
            get { return _authentication.Fetch(_features) ?? _authentication.Update(_features, new DefaultHttpAuthentication()); }
        }

        public override HttpRequest Request { get { return _request; } }

        public override HttpResponse Response { get { return _response; } }

        public override ClaimsPrincipal User
        {
            get { return HttpAuthentication.User; }
            set { HttpAuthentication.User = value; }
        }

        public override IDictionary<object, object> Items
        {
            get { return CanHasItems.Items; }
        }

        public override IServiceProvider ApplicationServices
        {
            get { return CanHasServiceProviders.ApplicationServices; }
            set { CanHasServiceProviders.ApplicationServices = value; }
        }

        public override IServiceProvider RequestServices
        {
            get { return CanHasServiceProviders.RequestServices; }
            set { CanHasServiceProviders.RequestServices = value; }
        }

        public int Revision { get { return _features.Revision; } }

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
            var handler = HttpAuthentication.Handler;
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
            var handler = HttpAuthentication.Handler;
            if (handler == null)
            {
                throw new InvalidOperationException("No authentication handlers present.");
            }

            var authenticateContext = new AuthenticateContext(authenticationTypes);
            handler.Authenticate(authenticateContext);

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
            var handler = HttpAuthentication.Handler;
            if (handler == null)
            {
                throw new InvalidOperationException("No authentication handlers present.");
            }

            var authenticateContext = new AuthenticateContext(authenticationTypes);
            await handler.AuthenticateAsync(authenticateContext);

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
