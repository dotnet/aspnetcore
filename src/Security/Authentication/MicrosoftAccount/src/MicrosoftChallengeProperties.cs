using System.Collections.Generic;
using Microsoft.AspNetCore.Authentication.OAuth;

namespace Microsoft.AspNetCore.Authentication.MicrosoftAccount
{    
    /// <summary>
    /// See https://docs.microsoft.com/azure/active-directory/develop/v2-oauth2-auth-code-flow#request-an-authorization-code for reference
    /// </summary>
    public class MicrosoftChallengeProperties : OAuthChallengeProperties
    {
        /// <summary>
        /// The parameter key for the "response_mode" argument being used for a challenge request.
        /// </summary>
        public static readonly string ResponseModeKey = "response_mode";

        /// <summary>
        /// The parameter key for the "domain_hint" argument being used for a challenge request.
        /// </summary>
        public static readonly string DomainHintKey = "domain_hint";

        /// <summary>
        /// The parameter key for the "login_hint" argument being used for a challenge request.
        /// </summary>
        public static readonly string LoginHintKey = "login_hint";

        /// <summary>
        /// The parameter key for the "prompt" argument being used for a challenge request.
        /// </summary>
        public static readonly string PromptKey = "prompt";

        public MicrosoftChallengeProperties()
        { }

        public MicrosoftChallengeProperties(IDictionary<string, string> items)
            : base(items)
        { }

        public MicrosoftChallengeProperties(IDictionary<string, string> items, IDictionary<string, object> parameters)
            : base(items, parameters)
        { }

        /// <summary>
        /// The "response_mode" parameter value being used for a challenge request.
        /// </summary>
        public string ResponseMode
        {
            get => GetParameter<string>(ResponseModeKey);
            set => SetParameter(ResponseModeKey, value);
        }

        /// <summary>
        /// The "domain_hint" parameter value being used for a challenge request.
        /// </summary>
        public string DomainHint
        {
            get => GetParameter<string>(DomainHintKey);
            set => SetParameter(DomainHintKey, value);
        }

        /// <summary>
        /// The "login_hint" parameter value being used for a challenge request.
        /// </summary>
        public string LoginHint
        {
            get => GetParameter<string>(LoginHintKey);
            set => SetParameter(LoginHintKey, value);
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
