using System;
using System.Diagnostics;
using System.Linq;
using Faker;
using Nyan.Core.Extensions;
using Nyan.Core.Settings;
using Nyan.Samples.Console.Model;

namespace Nyan.Samples.Console
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            const int amountToTest = 16;

            var s = new Stopwatch();
            Current.Log.Add("Fetching all records...");

            try { var allUsers = User.Get(); } catch (Exception e )
            {
                
            }


            Current.Log.Add(s.ElapsedMilliseconds + " ms");

            Current.Log.Add("Creating " + amountToTest + " records...");

            s.Restart();

            for (var i = 0; i < amountToTest; i++)
            {
                new User
                {
                    Name = Name.First()
                    ,
                    Surname = Name.Last(),
                    isAdmin = (RandomNumber.Next(0, 2) == 0),
                    BirthDate = RandomDay()
                }.Save();
            }
            s.Stop();
            Current.Log.Add(s.ElapsedMilliseconds + " ms - {0}/s".format(amountToTest / ((double)s.ElapsedMilliseconds / 1000)));

            Current.Log.Add("Fetching all records...");

            s.Restart();
            var a = User.Get().ToList();
            s.Stop();
            Current.Log.Add(s.ElapsedMilliseconds + " ms - {0}/s, {1} records fetched".format(a.Count / ((double)s.ElapsedMilliseconds / 1000), a.Count));

            Current.Log.Add("Updating all records...");
            s.Restart();
            foreach (var item in a)
            {
                item.Name = Name.FullName();
                item.BirthDate = RandomDay();
                item.Save();
            }
            s.Stop();
            Current.Log.Add(s.ElapsedMilliseconds + " ms - {0}/s".format(a.Count / ((double)s.ElapsedMilliseconds / 1000)));

            Current.Log.Add("Selecting using predicates...");

            s.Restart();
            // var a = Model.User.GetAll().ToList();
            a = User.Where(x => x.id > 3000).ToList();
            s.Stop();

            Current.Log.Add("Removing records by iteration...");

            s.Restart();
            User.RemoveAll();
            s.Stop();
            Current.Log.Add(s.ElapsedMilliseconds + " ms");

            Current.Log.Add("Done.");

            System.Console.ReadKey();
        }

        private static DateTime RandomDay()
        {
            var start = new DateTime(1950, 1, 1);
            var gen = new Random();

            var range = (DateTime.Today - start).Days;
            return start.AddDays(gen.Next(range));
        }
    }
}