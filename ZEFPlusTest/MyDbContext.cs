using Microsoft.EntityFrameworkCore;

namespace ZEFPlusTest
{
    public class MyDbContext : DbContext
    {
        public MyDbContext(DbContextOptions<MyDbContext> options)
            : base(options)
        { }

        public DbSet<Profile> Profiles { get; set; }
        public DbSet<IntCount> IntCounts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<IntCount>((eb) => { eb.HasNoKey(); });

            modelBuilder.Entity<Profile>((eb) =>
            {
                eb.ToTable(@"Profile", @"dbo");
                eb.Property(x => x.Id).HasColumnName(@"Id").HasColumnType(@"int").IsRequired().ValueGeneratedOnAdd();
                eb.HasKey(x => x.Id);

            });

        }
    }
}
