<p align="center">
<img src='http://i.imgur.com/3lxWGRq.png' /></br>
A lightweight .NET data service stack
</p>



## Yet another ORM framework? Really?

Not quite. It provides not only ORM but also caching, encryption and RESTful endpoints out of the box with zero setup in some cases. Think of it like a drop-in assembly set that'll get your data flowing from persistent storage to REST endpoints in no time and with minimal effort.

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

Congratulations! You created a SQLite- and MemoryCache- backed ORM class. A SQLite database was created, together with a table to store entries.

## You mentioned something about REST

Oh, REST! Right. So, once you decide you want to expose your ORM class data through a REST endpoint, do this:

- Reference `Nyan.Modules.Web.REST`;
- Implement a class deriving from `MicroEntityWebApiController<>`, and assign a route prefix to it:
<img src='http://i.imgur.com/R1mpJn9.png' /></br>
- ...that's it.

Now run your project, and reach the endpoint you specified. If you're running the sample provided (`Nyan.Samples.REST`), you can try the following URLs:

 - `http://localhost/Nyan.Samples.REST/users`
 <img src='http://i.imgur.com/jLYcOxD.png' /></br>
 - `http://localhost/Nyan.Samples.REST/users/10`
 <img src='http://i.imgur.com/TVhCcCG.png' /></br>
 - `http://localhost/Nyan.Samples.REST/users/new`
 <img src='http://i.imgur.com/2EprMgn.png' /></br>

## Core dependencies

The MicroEntity module wraps around [Stack Exchange Dapper](https://github.com/StackExchange/dapper-dot-net), an abusively fast IDbConnection interface extender.

REST endpoints are provided via [Microsoft WebApi 2](http://www.asp.net/web-api/overview/releases/whats-new-in-aspnet-web-api-22).

## Contributing

1. Fork it!
2. Create your feature branch: `git checkout -b my-new-feature`
3. Commit your changes: `git commit -am 'Add some feature'`
4. Push to the branch: `git push origin my-new-feature`
5. Submit a pull request :D

## Nyan Development Team

####Current collaborators
- [Cigano Morrison Mendez](https://github.com/cigano) (http://pt.stackoverflow.com/users/2999/cigano-morrison-mendez)
- [Leo Botinelly](https://github.com/lbotinelly) (http://pt.stackoverflow.com/users/1897/onosendai)

####Original contributors from Enterprise Systems at [Bucknell University](https://www.bucknell.edu) 
[Dan Mancusi](dmancusi@bucknell.edu)  
Mark Minisce  
[Leo Botinelly](leo.botinelly@bucknell.edu)  

## License
GPL V3 - a copyleft license that requires anyone who distributes 
this code or a derivative work to make the source available under the same 
terms. It also restricts use in hardware that forbids software alterations.

Check the [LICENSE file](https://github.com/lbotinelly/Nyan/blob/master/LICENSE) for more details.
