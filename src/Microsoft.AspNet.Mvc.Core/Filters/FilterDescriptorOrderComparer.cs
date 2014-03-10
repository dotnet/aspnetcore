using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc
{
    public class FilterDescriptorOrderComparer : IComparer<FilterDescriptor>
    {
        private static readonly FilterDescriptorOrderComparer _comparer = new FilterDescriptorOrderComparer();

        public static FilterDescriptorOrderComparer Comparer { get { return _comparer; } }

        public int Compare([NotNull]FilterDescriptor x, [NotNull]FilterDescriptor y)
        {
            if (x.Filter.Order == y.Filter.Order)
            {
                return x.Origin.CompareTo(y.Origin);
            }
            else
            {
                return x.Filter.Order.CompareTo(y.Filter.Order);
            }
        }
    }
}
