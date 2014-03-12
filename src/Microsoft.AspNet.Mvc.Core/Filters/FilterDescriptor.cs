namespace Microsoft.AspNet.Mvc
{
    public class FilterDescriptor
    {
        public FilterDescriptor([NotNull]IFilter filter, int filterScope)
        {
            Filter = filter;
            Scope = filterScope;

            var orderedFilter = Filter as IOrderedFilter;

            if (orderedFilter != null)
            {
                Order = orderedFilter.Order;
            }
        }

        public IFilter Filter { get; private set; }

        public int Order { get; private set; }

        public int Scope { get; private set; }
    }
}
