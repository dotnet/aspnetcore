
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Rewrite.Internal.CodeRules
{
    public class RewriteToHttpsRule : Rule
    {

        public bool stopProcessing { get; set; }
        public int? SSLPort { get; set; }
        public override RuleResult ApplyRule(RewriteContext context)
        {
            // TODO this only does http to https, add more features in the future. 
            if (!context.HttpContext.Request.IsHttps)
            {
                var host = context.HttpContext.Request.Host;
                if (SSLPort.HasValue && SSLPort.Value > 0)
                {
                    // a specific SSL port is specified
                    host = new HostString(host.Host, SSLPort.Value);
                }
                else
                {
                    // clear the port
                    host = new HostString(host.Host);
                }

                context.HttpContext.Request.Scheme = "https";
                context.HttpContext.Request.Host = host;
                if (stopProcessing)
                {
                    return RuleResult.StopRules;
                }
                else
                {
                    return RuleResult.Continue;
                }
            }
            return RuleResult.Continue;
        }
    }
}
