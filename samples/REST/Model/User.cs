using Nyan.Core.Modules.Data;
using Nyan.Modules.Web.REST;
using System;
using System.Web.Http;

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
    }


    [RoutePrefix("users")]
    public class UserController : MicroEntityWebApiController<User> { }
}
