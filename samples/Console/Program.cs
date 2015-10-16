using System;
using System.Linq;

namespace Console
{
    class Program
    {
        static void Main(string[] args)
        {
            var a = Model.User.GetAll();
            var b = new Model.User
            {
                Name = Faker.Name.FullName(),
                BirthDate = RandomDay()
            };

            b.Save();
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
