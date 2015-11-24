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
        public string Username { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string ZipCode { get; set; }
        public string Biography { get; set; }
        public string Company { get; set; }
        public bool isAdmin { get; set; }
        public DateTime? BirthDate { get; set; }

        public User()
        {
            BirthDate = DateTime.Now;
        }
    }

    [RoutePrefix("data/users")]
    public class UserController : MicroEntityWebApiController<User>
    {
        public override bool AuthorizeAction(RequestType pRequestType, AccessType pAccessType, string pidentifier, User pObject, string pContext)
        {
            if (pAccessType != AccessType.Write) return true;

            if (!pObject.isAdmin)
                throw new Exception("User isn't an Admin");

            return true;
        }

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
                    Email = Faker.Internet.Email(),
                    Username = Faker.Internet.UserName(),
                    Address = Faker.Address.StreetAddress(),
                    City = Faker.Address.City(),
                    State = Faker.Address.UsStateAbbr(),
                    ZipCode = Faker.Address.ZipCode(),
                    isAdmin = (Faker.RandomNumber.Next(0, 2) == 0),
                    BirthDate = RandomDay(),
                    Biography = Faker.Company.BS(),
                    Company = Faker.Company.Name()
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
