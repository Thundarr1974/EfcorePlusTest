using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
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

        public static string ToJson(this object target, bool ignoreDefaults = true, Type asType = null, bool format = true)
        {
            if (target == null) return null;

            var sb = new StringBuilder();
            using var writer = new StringWriter(sb);
            using var jw = new JsonTextWriter(writer);

            var serializer = GetSerializer(asType ?? target.GetType(), format ? Formatting.Indented : Formatting.None);

            serializer.DefaultValueHandling = ignoreDefaults ? DefaultValueHandling.Ignore : DefaultValueHandling.Include;
            serializer.Serialize(jw, target);

            writer.Flush();
            return sb.ToString();
        }

        public static T FromJson<T>(this string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return default;

            using var reader = new StringReader(json);
            using var jsonReader = new JsonTextReader(reader);

            try
            {
                return GetSerializer(typeof(T)).Deserialize<T>(jsonReader);
            }
            catch (JsonReaderException ex)
            {
                throw new JsonSerializationException($"Invalid json: {json}", ex);
            }
        }

        static JsonSerializer GetSerializer(Type type, Formatting formatting = Formatting.Indented)
        {
            return new JsonSerializer
            {
                //ContractResolver = new StrictTypeContractResolver(type),
                DateParseHandling = DateParseHandling.DateTimeOffset,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore,
                Formatting = formatting,
                Converters = { new StringEnumConverter(), new IsoDateTimeConverter() }
            };
        }
    }

    public static class ValueConversionExtensions
    {
        public static PropertyBuilder<T> HasJsonConversion<T>(this PropertyBuilder<T> propertyBuilder) where T : class, new()
        {
            var converter = new ValueConverter<T, string>
            (
                v => v.ToJson(true, null, false),
                v => v.FromJson<T>()
            );

            var comparer = new ValueComparer<T>
            (
                (l, r) => IsEqual(l, r),
                i => GetHashCode(i),
                i => GetSnapshot(i)
            );

            propertyBuilder
                .HasConversion(converter)
                .Metadata.SetValueComparer(comparer);

            return propertyBuilder;
        }

        private static T GetSnapshot<T>(T instance)
        {
            return instance is ICloneable cloneable
                ? (T)cloneable.Clone()
                : instance.ToJson(true, null).FromJson<T>();
        }

        static int GetHashCode<T>(T instance)
        {
            return instance is IEquatable<T>
                ? instance.GetHashCode()
                : instance.ToJson().GetHashCode();
        }

        static bool IsEqual<T>(T left, T right)
        {
            return left is IEquatable<T> equatable
                ? equatable.Equals(right)
                : left.ToJson() == right.ToJson();
        }
    }

    public static class DbSetExt
    {
        public static async Task InBatches<T>(this IOrderedQueryable<T> target, int batchSize, CancellationToken cancellationToken, Func<T[], Task> fxOnBatch)
        {
            if (fxOnBatch == null) throw new ArgumentNullException(nameof(fxOnBatch));

            var I = 0;
            T[] rows;

            while ((rows = await target.Skip(I).Take(batchSize).ToArrayAsync(cancellationToken)).Any())
            {
                if (cancellationToken.IsCancellationRequested) break;

                await fxOnBatch.Invoke(rows);
                I += batchSize;
            }
        }

        public static Task InBatchRows<T>(this IOrderedQueryable<T> target, int batchSize, CancellationToken cancellationToken, Func<T, Task> fxOnRow)
        {
            if (fxOnRow == null) throw new ArgumentNullException(nameof(fxOnRow));

            return InBatches(target, batchSize, cancellationToken, rows => Task.WhenAll(rows.Select(row => InvokeAsync(fxOnRow, row, cancellationToken))));
        }

        static Task InvokeAsync<T>(this Func<T, Task> target, T arg, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) return Task.CompletedTask;
            return target.Invoke(arg);
        }
    }
}
