using System;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Components.Web.Extensions
{
    public class Meta : HeadElementBase
    {
        private MetaElementState _state = default!;

        internal override object ElementKey => _state.Key;

        [Parameter]
        public string? Name { get; set; }

        [Parameter]
        public string? HttpEquiv { get; set; }

        [Parameter]
        public string? Charset { get; set; }

        [Parameter]
        public string? Content { get; set; }

        protected override async Task OnParametersSetAsync()
        {
            var key = GetMetaElementKey();

            if (_state == null)
            {
                _state = new MetaElementState();
            }
            else if (!_state.Key.Equals(key))
            {
                await HeadManager.NotifyDisposedAsync(this);
            }

            _state.Key = key;
            _state.Content = Content;

            await HeadManager.NotifyChangedAsync(this);
        }

        internal override async ValueTask ApplyAsync()
        {
            await HeadManager.SetMetaElementAsync(_state.Key, _state);
        }

        internal override async ValueTask<object> GetInitialStateAsync()
        {
            return await HeadManager.GetMetaElementAsync(_state.Key);
        }

        internal override ValueTask ResetInitialStateAsync(object initialState)
        {
            return HeadManager.SetMetaElementAsync(_state.Key, initialState);
        }

        // TODO: There can only be one charset, doesn't matter what value is.
        private MetaElementKey GetMetaElementKey()
        {
            try
            {
                var (id, name) = new (string? id, MetaElementKeyName type)[]
                {
                    (Name, MetaElementKeyName.Name),
                    (HttpEquiv, MetaElementKeyName.HttpEquiv),
                    (Charset, MetaElementKeyName.Charset)
                }
                .Where(t => t.id != null)
                .Single();

                return new MetaElementKey(name, id!);
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidOperationException(
                    $"{GetType()} must contain exactly one of {nameof(Name)}, {nameof(HttpEquiv)}, " +
                    $"or {nameof(Charset)}.",
                    ex);
            }
        }
    }
}
