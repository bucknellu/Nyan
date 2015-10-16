using Nyan.Core.Modules.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Console.Model
{
    [MicroEntitySetup(
        AutoGenerateMissingSchema = true,
        TableName = "Users",
        UseCaching = true,
        IsInsertableIdentifier = false)]
    public class User : MicroEntity<User>
    {
        [Key]
        public int id { get; set; }
        public string Name { get; set; }
        public DateTime? BirthDate { get; set; }
    }
}
