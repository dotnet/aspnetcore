using System.Collections.Generic;

namespace Microsoft.AspNetCore.Authentication.OAuth
{
    public class OAuthChallengeProperties : AuthenticationProperties
    {
        /// <summary>
        /// The parameter key for the "scope" argument being used for a challenge request.
        /// </summary>
        public static readonly string ScopeKey = "scope";

        public OAuthChallengeProperties()
        { }

        public OAuthChallengeProperties(IDictionary<string, string> items)
            : base(items)
        { }

        public OAuthChallengeProperties(IDictionary<string, string> items, IDictionary<string, object> parameters)
            : base(items, parameters)
        { }

        /// <summary>
        /// The "scope" parameter value being used for a challenge request.
        /// </summary>
        public ICollection<string> Scope
        {
            get => GetParameter<ICollection<string>>(ScopeKey);
            set => SetParameter(ScopeKey, value);
        }

        /// <summary>
        /// Set the "scope" parameter value.
        /// </summary>
        /// <param name="scopes">List of scopes.</param>
        public virtual void SetScope(params string[] scopes)
        {
            Scope = scopes;
        }
    }
}
