using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Authentication;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;

namespace IntegratedAuthSample
{
    public class NtAuthWrapper
    {
        private readonly object _instance;
        private readonly MethodInfo _getOutgoingBlob;
        private readonly MethodInfo _isCompleted;
        private readonly MethodInfo _getIdentity;

        public NtAuthWrapper()
        {
            var ntAuthType = typeof(AuthenticationException).Assembly.GetType("System.Net.NTAuthentication");
            var constructor = ntAuthType.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance).First();
            // internal NTAuthentication(bool isServer, string package, NetworkCredential credential, string spn, ContextFlagsPal requestedContextFlags, ChannelBinding channelBinding)
            // string spn = "HTTP/chrross-coredev.redmond.corp.microsoft.com";
            var credential = CredentialCache.DefaultCredentials;
            _instance = constructor.Invoke(new object[] { true, "Negotiate", credential, null, 0, null });
            _getOutgoingBlob = ntAuthType.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).Where(info =>
                info.Name.Equals("GetOutgoingBlob") && info.GetParameters().Count() == 2).Single();
            _isCompleted = ntAuthType.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).Where(info =>
                info.Name.Equals("get_IsCompleted")).Single();

            var negoStreamPalType = typeof(AuthenticationException).Assembly.GetType("System.Net.Security.NegotiateStreamPal");
            _getIdentity = negoStreamPalType.GetMethods(BindingFlags.NonPublic | BindingFlags.Static).Where(info =>
                info.Name.Equals("GetIdentity")).Single();
        }

        // Copied rather than reflected to remove the IsCompleted -> CloseContext check.
        internal string GetOutgoingBlob(string incomingBlob)
        {
            byte[] decodedIncomingBlob = null;
            if (incomingBlob != null && incomingBlob.Length > 0)
            {
                decodedIncomingBlob = Convert.FromBase64String(incomingBlob);
            }
            byte[] decodedOutgoingBlob = GetOutgoingBlob(decodedIncomingBlob, true);

            string outgoingBlob = null;
            if (decodedOutgoingBlob != null && decodedOutgoingBlob.Length > 0)
            {
                outgoingBlob = Convert.ToBase64String(decodedOutgoingBlob);
            }

            return outgoingBlob;
        }

        private byte[] GetOutgoingBlob(byte[] incomingBlob, bool thrownOnError)
        {
            return (byte[])_getOutgoingBlob.Invoke(_instance, new object[] { incomingBlob, thrownOnError });
        }

        internal bool IsCompleted
        {
            get => (bool)_isCompleted.Invoke(_instance, Array.Empty<object>());
        }

        internal ClaimsPrincipal GetPrincipal()
        {
            return new WindowsPrincipal((WindowsIdentity)_getIdentity.Invoke(obj: null, parameters: new object[] { _instance }));
        }
    }
}
