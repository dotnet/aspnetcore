using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Security;

namespace Microsoft.AspNet.Security.Test 
{
    public class FakePolicy : IAuthorizationPolicy
    {
            
        public int Order { get; set; }

        public Task ApplyingAsync(AuthorizationPolicyContext context) 
        {
            if (ApplyingAsyncAction != null)
            {
                ApplyingAsyncAction(context);
            }

            return Task.FromResult(0);
        }
        
        public Task ApplyAsync(AuthorizationPolicyContext context) 
        {
            if (ApplyAsyncAction != null)
            {
                ApplyAsyncAction(context);
            }
            
            return Task.FromResult(0);

        }
        
        public Task AppliedAsync(AuthorizationPolicyContext context) 
        {
            if (AppliedAsyncAction != null)
            {
                AppliedAsyncAction(context);
            }
            
            return Task.FromResult(0);
        }

        public Action<AuthorizationPolicyContext> ApplyingAsyncAction { get; set;}

        public Action<AuthorizationPolicyContext> ApplyAsyncAction { get; set;}

        public Action<AuthorizationPolicyContext> AppliedAsyncAction { get; set;}
    }
}
