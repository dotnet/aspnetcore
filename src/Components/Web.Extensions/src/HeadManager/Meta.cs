using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Components.Web.Extensions
{
    public class Meta : HeadElementBase
    {
        private MetaElement _metaElement = default!;

        internal override object ElementKey => _metaElement;

        [Parameter]
        public string? Name { get; set; }

        [Parameter]
        public string? Content { get; set; }

        protected override async Task OnParametersSetAsync()
        {
            if (Name == null)
            {
                throw new InvalidOperationException($"{GetType()} requires a name to be specified.");
            }

            if (_metaElement == null)
            {
                _metaElement = new MetaElement
                {
                    Name = Name,
                    Content = Content ?? string.Empty,
                };
            }
            else if (!string.Equals(_metaElement.Name, Name))
            {
                await HeadManager.NotifyDisposedAsync(this);
            }

            await HeadManager.NotifyChangedAsync(this);
        }

        internal override async ValueTask ApplyAsync()
        {
            await HeadManager.SetMetaElementByNameAsync(_metaElement.Name, _metaElement);
        }

        internal override async ValueTask<object> GetInitialStateAsync()
        {
            return await HeadManager.GetMetaElementByNameAsync(_metaElement.Name);
        }

        internal override ValueTask ResetInitialStateAsync(object initialState)
        {
            return HeadManager.SetMetaElementByNameAsync(_metaElement.Name, initialState);
        }
    }
}
