using Microsoft.EntityFrameworkCore;
using System;
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

            modelBuilder.Entity<LocalEnum>().HasNoKey().ToView("_no_table");
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
    }
}
