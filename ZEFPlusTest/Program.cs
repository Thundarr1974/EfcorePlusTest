using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Z.EntityFramework.Plus;

namespace ZEFPlusTest
{
    class Program
    {
        static void Main(string[] args)
        {
            MainAsync().GetAwaiter().GetResult();
        }

        public static async Task MainAsync()
        {

            var optionsBuilder = new DbContextOptionsBuilder<MyDbContext>();
            optionsBuilder.UseSqlServer(@$"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename={GetDbPath()}\db.mdf;Integrated Security=True");

            using var context = new MyDbContext(optionsBuilder.Options);
            try
            {
                var futureCount = context.IntCounts
                                         .FromSqlInterpolated($@"SELECT Count(*) as [Count]
                                                                 FROM Profile p
                                                                 WHERE p.Id = {1}").FutureValue();

                var students = context.Profiles
                                      .FromSqlInterpolated($@"SELECT [Id]
                                                              FROM Profile p
                                                              WHERE p.Id > {1}
                                                              ORDER BY Id
                                                              OFFSET {0} ROWS
                                                              FETCH NEXT {2} ROWS ONLY").Future();

                var profiles = await students.ToListAsync();
                var count = await futureCount.ValueAsync();
            }
            catch (Exception ex)
            {

                Console.Write(ex);
            }


        }

        private static string GetDbPath()
        {
            var exePath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase);
            Regex appPathMatcher = new Regex(@"(?<!fil)[A-Za-z]:\\+[\S\s]*?(?=\\+bin)");
            return Path.Combine(appPathMatcher.Match(exePath).Value, "App_Data");
        }
    }
}
