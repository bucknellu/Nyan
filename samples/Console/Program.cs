using System;
using System.Linq;
using System.Diagnostics;
using Nyan.Core.Extensions;

namespace Console
{
    class Program
    {
        static void Main(string[] args)
        {

            var amountToTest = 128;

            var s = new Stopwatch();
            Nyan.Core.Settings.Current.Log.Add("Fetching all records...");

            s.Start();
            var a = Model.User.GetAll().ToList();
            s.Stop();

            Nyan.Core.Settings.Current.Log.Add(s.ElapsedMilliseconds + " ms");

            Nyan.Core.Settings.Current.Log.Add("Creating " + amountToTest + " records...");

            s.Restart();

            for (int i = 0; i < amountToTest; i++)
            {
                var b = new Model.User
                {
                    Name = Faker.Name.FullName(),
                    BirthDate = RandomDay()
                };

                b.Save();

            }
            s.Stop();
            Nyan.Core.Settings.Current.Log.Add(s.ElapsedMilliseconds + " ms - {0}/s".format(amountToTest / ((double)s.ElapsedMilliseconds / 1000)));

            Nyan.Core.Settings.Current.Log.Add("Fetching all records...");

            s.Restart();
            a = Model.User.GetAll().ToList();
            s.Stop();
            Nyan.Core.Settings.Current.Log.Add(s.ElapsedMilliseconds + " ms - {0}/s, {1} records fetched".format(a.Count / ((double)s.ElapsedMilliseconds / 1000), a.Count));

            Nyan.Core.Settings.Current.Log.Add("Updating all records...");
            s.Restart();
            foreach (var item in a)
            {
                item.Name = Faker.Name.FullName();
                item.BirthDate = RandomDay();
                item.Save();
            }
            s.Stop();
            Nyan.Core.Settings.Current.Log.Add(s.ElapsedMilliseconds + " ms - {0}/s".format(a.Count / ((double)s.ElapsedMilliseconds / 1000)));

            Nyan.Core.Settings.Current.Log.Add("Removing records by iteration...");

            s.Restart();
            Model.User.RemoveAll();
            s.Stop();
            Nyan.Core.Settings.Current.Log.Add(s.ElapsedMilliseconds + " ms");

            Nyan.Core.Settings.Current.Log.Add("Done.");

            System.Console.ReadKey();
        }

        static DateTime RandomDay()
        {
            DateTime start = new DateTime(1950, 1, 1);
            Random gen = new Random();

            int range = (DateTime.Today - start).Days;
            return start.AddDays(gen.Next(range));
        }

        /// <summary>
        ///     Function that generates aliens names. 
        /// </summary>
        /// <param name="length">Length of the alien name.</param>
        /// <returns>An alien name.</returns>
        static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
