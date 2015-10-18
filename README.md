# Nyan
A lightweight, no frills .NET service stack

It provides ORM, caching, encryption and RESTful endpoints out of the box with minimal to no setup.

# Nyan

A lightweight, no frills .NET service stack

## Installation

To have it working straight out of the box with no setup required, add a reference to `Nyan.Core.dll` and `Nyan.Portable.dll` (`Nyan.Modules.Cache.Memory.dll` and `Nyan.Modules.Data.SQLite.dll` are referenced by `Nyan.Portable`.)

## Usage

Very complicated steps aread: 
 - Create a class that inherits from the `MicroEntity<>` generic class
 - Add a `MicroEntitySetup` attribute, and assign a table name
 - Mark the property you want to use as a unique identifier with the [Key] attribute

C# example:

    using Nyan.Core.Modules.Data;
    using System;
    
    namespace Test
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

## W-wait, what have I just done?

Congratulations! You've just created a SQLite- and MemoryCache- backed ORM class.

## Methods

- *`Class.GetAll()`*: Returns all stored entries
- *`Class.Get(id)`*: Returns the entry associated with `id`, or null if it doesn't exist.
- *`Class.Save()`*: Creates (if new) or updates the database entry associated with the class instance
- *`Class.SaveAndGetId()`*: Same as above, but also returns the ID. Useful when creating a new record.
- *`Class.Remove()`*: Deletes the database entry.

## Contributing

1. Fork it!
2. Create your feature branch: `git checkout -b my-new-feature`
3. Commit your changes: `git commit -am 'Add some feature'`
4. Push to the branch: `git push origin my-new-feature`
5. Submit a pull request :D

## Credits

Nyan Development Team
- Leonel Sanches (http://pt.stackoverflow.com/users/2999/cigano-morrison-mendez)
- Leo Botinelly (http://pt.stackoverflow.com/users/1897/onosendai)
- 
Original code
- Leo Botinelly (http://pt.stackoverflow.com/users/1897/onosendai)

## License
GPL V3 - a copyleft license that requires anyone who distributes 
this code or a derivative work to make the source available under the same 
terms. It also restricts use in hardware that forbids software alterations.
