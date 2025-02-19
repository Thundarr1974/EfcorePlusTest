﻿using Microsoft.EntityFrameworkCore;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
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
                                      .FromSqlInterpolated($@"SELECT *
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

            try
            {
                var qry = from p in context.Profiles
                          join pid in context.FromEnum<ProfileId>() on (int)p.Id equals pid.Value
                          where p.Id > 0
                          select new ProfileView
                          {
                              Id = p.Id,
                              Name = pid.Name
                          };


                var (count, items) = await qry
                    .AsNoTracking()
                    .GetPaged(1, p => p.Id, CancellationToken.None);

                Debug.Assert(count > 0);
                Debug.Assert(items.Length > 0);
            }
            catch (Exception ex)
            {
                Console.Write(ex);
            }
        }

        private static string GetDbPath()
        {
            var exePath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            Regex appPathMatcher = new Regex(@"(?<!fil)[A-Za-z]:\\+[\S\s]*?(?=\\+bin)");
            return Path.Combine(appPathMatcher.Match(exePath).Value, "App_Data");
        }
    }

    public class ProfileView
    {
        public ProfileId Id { get; set; }

        public string Name { get; set; }
    }
}
