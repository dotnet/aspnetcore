using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Components.WebView.Wpf
{
    internal class WpfDispatcher : Dispatcher
    {
        public static Dispatcher Instance { get; } = new WpfDispatcher();

        public override bool CheckAccess()
        {
            throw new NotImplementedException();
        }

        public override Task InvokeAsync(Action workItem)
        {
            throw new NotImplementedException();
        }

        public override Task InvokeAsync(Func<Task> workItem)
        {
            throw new NotImplementedException();
        }

        public override Task<TResult> InvokeAsync<TResult>(Func<TResult> workItem)
        {
            throw new NotImplementedException();
        }

        public override Task<TResult> InvokeAsync<TResult>(Func<Task<TResult>> workItem)
        {
            throw new NotImplementedException();
        }
    }
}
