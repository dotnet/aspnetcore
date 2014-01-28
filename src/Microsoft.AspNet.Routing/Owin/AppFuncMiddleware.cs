using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Routing.Owin
{
    internal class AppFuncMiddleware
    {
        public AppFuncMiddleware(Func<IDictionary<string, object>, Task> next, Func<IDictionary<string, object>, Task> appFunc)
        {
            this.Next = next;
            this.AppFunc = appFunc;
        }

        private Func<IDictionary<string, object>, Task> AppFunc
        {
            get;
            set;
        }

        private Func<IDictionary<string, object>, Task> Next
        {
            get;
            set;
        }

        public Task Invoke(IDictionary<string, object> context)
        {
            return this.AppFunc(context);
        }
    }
}
