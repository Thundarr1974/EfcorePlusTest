using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Z.EntityFramework.Plus;

namespace ZEFPlusTest
{
    internal static class Extensions
    {
        const int PAGE_SIZE = 10;

        public static async Task<(int Count, T[] Items)> GetPaged<T, TSort>(this IQueryable<T> qry, int page, Expression<Func<T, TSort>> defaultSort, CancellationToken cancellationToken) where T : class
        {
            var skip = (page - 1) * PAGE_SIZE;

            var ftrCount = qry
                .DeferredCount()
                .FutureValue();

            var ftrItems = qry
                .OrderBy(defaultSort)
                .Skip(skip)
                .Take(PAGE_SIZE)
                .Future();

            var count = await ftrCount.ValueAsync(cancellationToken);
            var items = await ftrItems.ToArrayAsync(cancellationToken);

            return (count, items);
        }
    }
}
