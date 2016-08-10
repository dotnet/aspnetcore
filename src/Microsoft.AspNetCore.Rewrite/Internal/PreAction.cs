
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Rewrite.Internal.UrlRewrite;

namespace Microsoft.AspNetCore.Rewrite.Internal
{
    public abstract class PreAction
    {
        public abstract void ApplyAction(HttpContext context, MatchResults ruleMatch, MatchResults condMatch);
    }
}
