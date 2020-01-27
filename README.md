<p align="center">
<img src='http://i.imgur.com/3lxWGRq.png' /></br>
A lightweight .NET data service stack
</p>

## Status
Nyan is no longer in active development, and is currently in maintenance mode. If you want the same features but for .NET Core, check out [Zen](https://github.com/lbotinelly/zen).

## Yet another ORM framework? Really?

Not quite. It provides not only ORM but also caching, encryption and RESTful endpoints out of the box with zero setup in some cases. Think of it like a drop-in package set that'll get your data flowing from/to persistent storage to REST endpoints in no time and with minimal effort in most cases.

## Installation

To have it working straight out of the box with no setup required, add a reference to `Nyan.Core.dll`, `Nyan.Modules.Cache.Memory.dll` and `Nyan.Modules.Data.SQLCompact.dll`. Compile from source, or check NuGet for these packages:

- [📦 Nyan.Core](https://www.nuget.org/packages/Nyan.Core/)
- [📦 Nyan.Modules.Cache.Memory](https://www.nuget.org/packages/Nyan.Modules.Cache.Memory/)
- [📦 Nyan.Modules.Data.SQLCompact](https://www.nuget.org/packages/Nyan.Modules.Data.SQLCompact/)

## Usage

Very complicated example steps aread, you better pay attention: 
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

Congratulations! You created a SQL Compact- and memory cache-backed ORM class. A default database was created, together with a table to store entries.

## You mentioned something about REST

Oh, REST! Right. So, once you decide you want to expose your ORM class data through a REST endpoint, do this:

- Reference `Nyan.Modules.Web.REST` ([NuGet](https://www.nuget.org/packages/Nyan.Modules.Web.REST/));  
- Implement a class deriving from `MicroEntityWebApiController<>`, and assign a route prefix to it:
```
   [RoutePrefix("users")]  
   public class UserController : MicroEntityWebApiController<User>  
   {
       public UserController() { }  
   }
```
- ...and that's it.

Now run your project, and reach the endpoint you specified. If you're running the sample provided (`Nyan.Samples.REST`), you can try the following URLs:

- **`http://localhost/Nyan.Samples.REST/users`**  
 ```
[{"id":1,"Name":"Maximus Howell III","Surname":null,"isAdmin":false,"BirthDate":"2002-05-13T00:00:00"},{"id":2,"Name":"Odie Yost","Surname":null,"isAdmin":false,"BirthDate":"1989-04-21T00:00:00"},{"id":3,"Name":"Vincent Pouros","Surname":null,"isAdmin":true,"BirthDate":"2002-02-23T00:00:00"},{"id":4,"Name":"Russel Fadel","Surname":null,(...)
```
- **`http://localhost/Nyan.Samples.REST/users/1`**  
 ```
{"id":1,"Name":"Maximus Howell III","Surname":null,"isAdmin":false,"BirthDate":"2002-05-13T00:00:00"}
```

- **`http://localhost/Nyan.Samples.REST/users/new`**  
 `{"id":0,"Name":null,"Surname":null,"isAdmin":false,"BirthDate":null}`  

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

### Current collaborators
- [Leo Botinelly](https://www.linkedin.com/in/lbotinelly) (http://pt.stackoverflow.com/users/1897/onosendai)

### Past collaborators
- [Cigano Morrison Mendez](https://github.com/cigano) (http://pt.stackoverflow.com/users/2999/cigano-morrison-mendez)

### Original contributors from Enterprise Systems at [Bucknell University](https://www.bucknell.edu)

- [Dan Mancusi](mailto:dmancusi@bucknell.edu)  
- Mark Minisce  
- [Leo Botinelly](mailto:leo.botinelly@bucknell.edu)  

## License
MIT - a permissive free software license originating at the Massachusetts Institute of Technology (MIT), it puts only very limited restriction on reuse and has, therefore, an excellent license compatibility. It permits reuse within proprietary software provided that all copies of the licensed software include a copy of the MIT License terms and the copyright notice.


Check the [LICENSE file](https://github.com/bucknellu/Nyan/blob/master/LICENSE) for more details.
