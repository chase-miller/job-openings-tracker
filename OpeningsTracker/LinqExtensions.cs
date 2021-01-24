using System;
using System.Collections.Generic;
using System.Linq;

namespace OpeningsTracker
{
    public static class LinqExtensions
    {
        /// <summary>
        /// Similar to MoreLinq's ExceptBy method but works on heterogeneous types.
        /// </summary>
        public static IEnumerable<TSource> ExceptBy<TSource, TOther, TKey>(this IEnumerable<TSource> sourceItems,
            IEnumerable<TOther> otherItems, Func<TSource, TKey> sourceKeyFunc, Func<TOther, TKey> otherKeyFunc)
        {
            return from sourceItem in sourceItems
                join otherItem in otherItems on sourceKeyFunc.Invoke(sourceItem) equals otherKeyFunc.Invoke(otherItem)
                    into gj
                from subSourceItem in gj.DefaultIfEmpty()        // left outer join
                where subSourceItem == null                      // only items on the left that don't match the set on the right
                select sourceItem;
        }
    }
}
