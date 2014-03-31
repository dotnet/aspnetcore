// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.Abstractions.Security;
using Microsoft.AspNet.FeatureModel;
using Microsoft.AspNet.HttpFeature;
using Microsoft.AspNet.HttpFeature.Security;
using Microsoft.AspNet.PipelineCore.Infrastructure;

namespace Microsoft.AspNet.PipelineCore.Security
{
    public class DefaultAuthenticationManager : AuthenticationManager
    {
        private static DescriptionDelegate GetAuthenticationTypesDelegate = GetAuthenticationTypesCallback;

        private readonly DefaultHttpContext _context;
        private readonly IFeatureCollection _features;

        private readonly FeatureReference<IHttpAuthentication> _authentication = FeatureReference<IHttpAuthentication>.Default;
        private readonly FeatureReference<IHttpResponseInformation> _response = FeatureReference<IHttpResponseInformation>.Default;

        public DefaultAuthenticationManager(DefaultHttpContext context, IFeatureCollection features)
        {
            _context = context;
            _features = features;
        }

        private IHttpAuthentication HttpAuthentication
        {
            get { return _authentication.Fetch(_features) ?? _authentication.Update(_features, new DefaultHttpAuthentication()); }
        }

        public override HttpContext HttpContext { get { return _context; } }

        private IHttpResponseInformation HttpResponseInformation
        {
            get { return _response.Fetch(_features); }
        }

        public override IEnumerable<AuthenticationDescription> GetAuthenticationTypes()
        {
            var descriptions = new List<AuthenticationDescription>();
            var handler = HttpAuthentication.Handler;
            if (handler != null)
            {
                handler.GetDescriptions(GetAuthenticationTypesDelegate, descriptions);
            }
            return descriptions;
        }

        private static void GetAuthenticationTypesCallback(IDictionary<string, object> description, object state)
        {
            var localDescriptions = (List<AuthenticationDescription>)state;
            localDescriptions.Add(new AuthenticationDescription(description));
        }

        public override AuthenticationResult Authenticate(string authenticationType)
        {
            return Authenticate(new[] { authenticationType }).SingleOrDefault();
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
            IEnumerable<string> leftovers = authenticationTypes.Except(authenticateContext.Acked);
            if (leftovers.Any())
            {
                throw new InvalidOperationException("The following authentication types did not ack: " + string.Join(", ", leftovers));
            }

            return authenticateContext.Results;
        }

        public override async Task<AuthenticationResult> AuthenticateAsync(string authenticationType)
        {
            return (await AuthenticateAsync(new[] { authenticationType })).SingleOrDefault();
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
            IEnumerable<string> leftovers = authenticationTypes.Except(authenticateContext.Acked);
            if (leftovers.Any())
            {
                throw new InvalidOperationException("The following authentication types did not ack: " + string.Join(", ", leftovers));
            }

            return authenticateContext.Results;
        }

        public override void Challenge()
        {
            Challenge(new string[0]);
        }

        public override void Challenge(AuthenticationProperties properties)
        {
            Challenge(new string[0], properties);
        }

        public override void Challenge(string authenticationType)
        {
            Challenge(new[] { authenticationType });
        }

        public override void Challenge(string authenticationType, AuthenticationProperties properties)
        {
            Challenge(new[] { authenticationType }, properties);
        }

        public override void Challenge(IList<string> authenticationTypes)
        {
            Challenge(authenticationTypes, null);
        }

        public override void Challenge(IList<string> authenticationTypes, AuthenticationProperties properties)
        {
            if (authenticationTypes == null)
            {
                throw new ArgumentNullException();
            }
            HttpResponseInformation.StatusCode = 401;
            var handler = HttpAuthentication.Handler;
            if (handler == null)
            {
                throw new InvalidOperationException("No authentication handlers present.");
            }

            var challengeContext = new ChallengeContext(authenticationTypes, properties == null ? null : properties.Dictionary);
            handler.Challenge(challengeContext);

            // Verify all types ack'd
            IEnumerable<string> leftovers = authenticationTypes.Except(challengeContext.Acked);
            if (leftovers.Any())
            {
                throw new InvalidOperationException("The following authentication types did not ack: " + string.Join(", ", leftovers));
            }
        }

        public override void SignIn(ClaimsPrincipal user)
        {
            SignIn(user, null);
        }

        public override void SignIn(ClaimsPrincipal user, AuthenticationProperties properties)
        {
            if (user == null)
            {
                throw new ArgumentNullException();
            }
            var handler = HttpAuthentication.Handler;
            if (handler == null)
            {
                throw new InvalidOperationException("No authentication handlers present.");
            }

            var signInContext = new SignInContext(user, properties == null ? null : properties.Dictionary);
            handler.SignIn(signInContext);

            // Verify all types ack'd
            IEnumerable<string> leftovers = user.Identities.Select(identity => identity.AuthenticationType).Except(signInContext.Acked);
            if (leftovers.Any())
            {
                throw new InvalidOperationException("The following authentication types did not ack: " + string.Join(", ", leftovers));
            }
        }

        public override void SignOut()
        {
            SignOut(new string[0]);
        }

        public override void SignOut(string authenticationType)
        {
            SignOut(new[] { authenticationType });
        }

        public override void SignOut(IList<string> authenticationTypes)
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

            var signOutContext = new SignOutContext(authenticationTypes);
            handler.SignOut(signOutContext);

            // Verify all types ack'd
            IEnumerable<string> leftovers = authenticationTypes.Except(signOutContext.Acked);
            if (leftovers.Any())
            {
                throw new InvalidOperationException("The following authentication types did not ack: " + string.Join(", ", leftovers));
            }
        }
    }
}
