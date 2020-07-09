using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Components.Web.Extensions
{
    public abstract class HeadElementBase : ComponentBase, IDisposable
    {
        [Inject]
        protected HeadManager HeadManager { get; set; } = default!;

        internal LinkedListNode<HeadElementBase> Node { get; }

        internal abstract object ElementKey { get; }

        protected HeadElementBase()
        {
            Node = new LinkedListNode<HeadElementBase>(this);
        }

        protected override void OnInitialized()
        {
            if (HeadManager == null)
            {
                throw new InvalidOperationException($"{GetType()} requires the {typeof(HeadManager)} service.");
            }
        }

        protected override async Task OnParametersSetAsync()
        {
            await HeadManager.NotifyChangedAsync(this);
        }

        public void Dispose()
        {
            HeadManager.NotifyDisposedAsync(this).ConfigureAwait(false); // TODO: Async problems here?
        }

        internal abstract ValueTask<object> GetInitialStateAsync();

        internal abstract ValueTask ResetInitialStateAsync(object initialState);

        internal abstract ValueTask ApplyAsync();
    }
}
