using Nyan.Core.Modules.Data;
using Nyan.Modules.Web.REST;
using System;
using System.Web.Http;
using Nyan.Core.Modules.Log;

namespace Nyan.Samples.REST.Model
{
    [MicroEntitySetup(TableName = "Users")]
    public class User : MicroEntity<User>
    {
        [Key]
        public int id { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public bool isAdmin { get; set; }
        public DateTime? BirthDate { get; set; }

        public User()
        {
            BirthDate = DateTime.Now;
        }
    }


    [RoutePrefix("api/users")]
    public class UserController : MicroEntityWebApiController<User> {

        [Route("wipe")]
        [HttpGet]
        public void Wipe()
        {
            Core.Settings.Current.Log.Add("Wiping all User data", Message.EContentType.Maintenance);
            Model.User.RemoveAll();
        }

        [Route("make/{count}")]
        [HttpGet]
        public void Make(long count)
        {
            Core.Settings.Current.Log.Add("Creating " + count + " new records", Message.EContentType.Maintenance);

            for (int i = 0; i < count; i++)
                {
                    new User
                    {
                        Name = Faker.Name.First(),
                        Surname = Faker.Name.Last(),
                        isAdmin = (Faker.RandomNumber.Next(0, 2) == 0),
                        BirthDate = RandomDay()
                    }.Save();
                }
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
