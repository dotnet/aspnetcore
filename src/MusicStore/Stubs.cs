using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MvcMusicStore
{
    public static class ListExtensions
    {
        public static void ForEach<T>(this List<T> list, Action<T> each)
        {
            foreach (var item in list)
            {
                each(item);
            }
        }
    }

    public static class ListPretendingToBeDbContextExtensions
    {
        // Mock DbSet (List<T>)
        public static T Find<T>(this List<T> list, params object[] keys)
        {
            return default(T);
        }

        public static IEnumerable<T> Include<T>(this IEnumerable<T> list, string include)
        {
            return list;
        }

        public static IEnumerable<T> Include<T, A>(this IEnumerable<T> list, Func<T, A> projection)
        {
            return list;
        }
    }
}
