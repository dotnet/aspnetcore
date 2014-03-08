using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc
{
    public class FilterDescriptorComparer : IComparer<FilterDescriptor>
    {
        private static FilterDescriptorComparer _comparer = new FilterDescriptorComparer();

        public static FilterDescriptorComparer Comparer { get { return _comparer; } }

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
