using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.Abstractions.Infrastructure;
using Microsoft.AspNet.Abstractions.Security;
using Microsoft.AspNet.FeatureModel;
using Microsoft.AspNet.HttpFeature;
using Microsoft.AspNet.HttpFeature.Security;
using Microsoft.AspNet.PipelineCore.Collections;
using Microsoft.AspNet.PipelineCore.Infrastructure;
using Microsoft.AspNet.PipelineCore.Security;

namespace Microsoft.AspNet.PipelineCore
{
    public class DefaultHttpResponse : HttpResponse
    {
        private readonly DefaultHttpContext _context;
        private readonly IFeatureCollection _features;
        private FeatureReference<IHttpResponseInformation> _response = FeatureReference<IHttpResponseInformation>.Default;
        private FeatureReference<ICanHasResponseCookies> _canHasCookies = FeatureReference<ICanHasResponseCookies>.Default;
        private FeatureReference<IHttpAuthentication> _authentication = FeatureReference<IHttpAuthentication>.Default;

        public DefaultHttpResponse(DefaultHttpContext context, IFeatureCollection features)
        {
            _context = context;
            _features = features;
        }

        private IHttpResponseInformation HttpResponseInformation
        {
            get { return _response.Fetch(_features); }
        }

        private ICanHasResponseCookies CanHasResponseCookies
        {
            get { return _canHasCookies.Fetch(_features) ?? _canHasCookies.Update(_features, new DefaultCanHasResponseCookies(_features)); }
        }

        private IHttpAuthentication HttpAuthentication
        {
            get { return _authentication.Fetch(_features) ?? _authentication.Update(_features, new DefaultHttpAuthentication()); }
        }

        public override HttpContext HttpContext { get { return _context; } }

        public override int StatusCode
        {
            get { return HttpResponseInformation.StatusCode; }
            set { HttpResponseInformation.StatusCode = value; }
        }

        public override IHeaderDictionary Headers
        {
            get { return new HeaderDictionary(HttpResponseInformation.Headers); }
        }

        public override Stream Body
        {
            get { return HttpResponseInformation.Body; }
            set { HttpResponseInformation.Body = value; }
        }

        public override long? ContentLength
        {
            get
            {
                return ParsingHelpers.GetContentLength(Headers);
            }
            set
            {
                ParsingHelpers.SetContentLength(Headers, value);
            }
        }

        public override string ContentType
        {
            get
            {
                var contentType = Headers[Constants.Headers.ContentType];
                return contentType;
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    HttpResponseInformation.Headers.Remove(Constants.Headers.ContentType);
                }
                else
                {
                    HttpResponseInformation.Headers[Constants.Headers.ContentType] = new[] { value };
                }
            }
        }

        public override IResponseCookies Cookies
        {
            get { return CanHasResponseCookies.Cookies; }
        }

        public override void OnSendingHeaders(Action<object> callback, object state)
        {
            HttpResponseInformation.OnSendingHeaders(callback, state);
        }

        public override void Redirect(string location)
        {
            HttpResponseInformation.StatusCode = 302;
            Headers.Set(Constants.Headers.Location, location);
        }

        public override Task WriteAsync(string data)
        {
            var bytes = Encoding.UTF8.GetBytes(data);
            return Body.WriteAsync(bytes, 0, bytes.Length);
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

        public override void SignIn(IList<ClaimsIdentity> identities, AuthenticationProperties properties)
        {
            if (identities == null)
            {
                throw new ArgumentNullException();
            }
            var handler = HttpAuthentication.Handler;
            if (handler == null)
            {
                throw new InvalidOperationException("No authentication handlers present.");
            }

            var signInContext = new SignInContext(identities, properties == null ? null : properties.Dictionary);
            handler.SignIn(signInContext);

            // Verify all types ack'd
            IEnumerable<string> leftovers = identities.Select(identity => identity.AuthenticationType).Except(signInContext.Acked);
            if (leftovers.Any())
            {
                throw new InvalidOperationException("The following authentication types did not ack: " + string.Join(", ", leftovers));
            }
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