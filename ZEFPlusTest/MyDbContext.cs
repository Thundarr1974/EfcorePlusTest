using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace ZEFPlusTest
{
    public class MyDbContext : DbContext
    {
        public MyDbContext(DbContextOptions<MyDbContext> options)
            : base(options)
        { }

        public virtual DbSet<Profile> Profiles { get; set; }

        public virtual DbSet<IntCount> IntCounts { get; set; }

        public virtual DbSet<LocalEnum> LocalEnums { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<IntCount>((eb) => { eb.HasNoKey(); });

            modelBuilder.Entity<Profile>((eb) =>
            {
                eb.ToTable(@"Profile", @"dbo");
                eb.Property(x => x.Id).HasColumnName(@"Id").HasColumnType(@"int").IsRequired().ValueGeneratedOnAdd();
                eb.HasKey(x => x.Id);
            });

            modelBuilder.Entity<Profile>()
                .Property(p => p.Properties)
                .HasJsonConversion();

            modelBuilder.Entity<LocalEnum>().HasNoKey().ToView("_no_table");

            AddSoftDeleteFilter<Profile>(modelBuilder);
        }

        public IQueryable<LocalEnum> FromEnum<T>()
        {
            var typeT = typeof(T);

            var selects = Enum.GetValues(typeT)
                .Cast<T>()
                .Select(e => $"({Convert.ToInt32(e)}, '{e}')"); // Todo localize

            var sql = $"SELECT * FROM (VALUES {string.Join(", ", selects)}) AS {typeT.Name}([Value], [Name])";

            return LocalEnums.FromSqlRaw(sql);
        }

        protected static void AddSoftDeleteFilter<TEntity>(ModelBuilder modelBuilder) where TEntity : EntityBase
        {
            modelBuilder.Entity<TEntity>().HasQueryFilter(x => !x.SoftDeleted);
        }
    }

    public abstract class EntityBase
    {
        internal bool ForceDelete;

        [Required]
        public DateTimeOffset WhenCreated { get; set; }

        [Required]
        public DateTimeOffset WhenUpdated { get; set; }

        [Required]
        [Timestamp]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public byte[] RowVersion { get; set; }

        [Required()]
        public bool SoftDeleted { get; set; }

        public void DisableSoftDelete()
        {
            ForceDelete = true;
        }
    }
}
