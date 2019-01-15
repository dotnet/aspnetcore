using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.AspNetCore.Authentication.OpenIdConnect
{
    public class OpenIdConnectChallengeProperties : OAuthChallengeProperties
    {
        /// <summary>
        /// The parameter key for the "max_age" argument being used for a challenge request.
        /// </summary>
        public static readonly string MaxAgeKey = OpenIdConnectParameterNames.MaxAge;

        /// <summary>
        /// The parameter key for the "prompt" argument being used for a challenge request.
        /// </summary>
        public static readonly string PromptKey = OpenIdConnectParameterNames.Prompt;

        public OpenIdConnectChallengeProperties()
        { }

        public OpenIdConnectChallengeProperties(IDictionary<string, string> items)
            : base(items)
        { }

        public OpenIdConnectChallengeProperties(IDictionary<string, string> items, IDictionary<string, object> parameters)
            : base(items, parameters)
        { }

        /// <summary>
        /// The "max_age" parameter value being used for a challenge request.
        /// </summary>
        public TimeSpan? MaxAge
        {
            get => GetParameter<TimeSpan?>(MaxAgeKey);
            set => SetParameter(MaxAgeKey, value);
        }

        /// <summary>
        /// The "prompt" parameter value being used for a challenge request.
        /// </summary>
        public string Prompt
        {
            get => GetParameter<string>(PromptKey);
            set => SetParameter(PromptKey, value);
        }
    }
}
