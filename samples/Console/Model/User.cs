﻿using Nyan.Core.Modules.Data;
using System;

namespace Nyan.Samples.Console.Model
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
}
