using System;
using System.Linq;
using System.Diagnostics;
using Nyan.Core.Extensions;

namespace Nyan.Samples.Console
{
    class Program
    {
        static void Main(string[] args)
        {

            var amountToTest = 512;

            var s = new Stopwatch();
            Core.Settings.Current.Log.Add("Fetching all records...");

            Core.Settings.Current.Log.Add(s.ElapsedMilliseconds + " ms");

            Core.Settings.Current.Log.Add("Creating " + amountToTest + " records...");

            s.Restart();

            for (int i = 0; i < amountToTest; i++)
            {
                new Model.User
                {
                    Name = Faker.Name.FullName(),
                    isAdmin = (Faker.RandomNumber.Next(0, 2) == 0),
                    BirthDate = RandomDay()
                }.Save();

            }
            s.Stop();
            Core.Settings.Current.Log.Add(s.ElapsedMilliseconds + " ms - {0}/s".format(amountToTest / ((double)s.ElapsedMilliseconds / 1000)));

            Core.Settings.Current.Log.Add("Fetching all records...");

            s.Restart();
            var a = Model.User.Get().ToList();
            s.Stop();
            Core.Settings.Current.Log.Add(s.ElapsedMilliseconds + " ms - {0}/s, {1} records fetched".format(a.Count / ((double)s.ElapsedMilliseconds / 1000), a.Count));

            Core.Settings.Current.Log.Add("Updating all records...");
            s.Restart();
            foreach (var item in a)
            {
                item.Name = Faker.Name.FullName();
                item.BirthDate = RandomDay();
                item.Save();
            }
            s.Stop();
            Core.Settings.Current.Log.Add(s.ElapsedMilliseconds + " ms - {0}/s".format(a.Count / ((double)s.ElapsedMilliseconds / 1000)));

            Core.Settings.Current.Log.Add("Selecting using predicates...");

            s.Restart();
            // var a = Model.User.GetAll().ToList();
            a = Model.User.Where(x => x.id > 3000).ToList();
            s.Stop();

            Core.Settings.Current.Log.Add("Removing records by iteration...");

            s.Restart();
            Model.User.RemoveAll();
            s.Stop();
            Core.Settings.Current.Log.Add(s.ElapsedMilliseconds + " ms");

            Core.Settings.Current.Log.Add("Done.");

            System.Console.ReadKey();
        }

        static DateTime RandomDay()
        {
            DateTime start = new DateTime(1950, 1, 1);
            Random gen = new Random();

            int range = (DateTime.Today - start).Days;
            return start.AddDays(gen.Next(range));
        }
    }
}
