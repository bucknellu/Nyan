using Nyan.Core.Modules.Data;
using System;

namespace Nyan.WebSample.Models
{
    [MicroEntitySetup(TableName = "Users")]
    public class User : MicroEntity<User>
    {
        [Key]
        public int id { get; set; }
        public string Name { get; set; }
        public DateTime? BirthDate { get; set; }
    }
}
