using System;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace Nyan.Samples.REST
{
    public partial class Default : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            var a = Model.User.Get().ToList();

            if (a.Count < 5)
            {
                for (int i = 0; i < 16; i++)
                {
                    new Model.User
                    {
                        Name = Faker.Name.First(),
                        Surname = Faker.Name.Last(),
                        isAdmin = (Faker.RandomNumber.Next(0, 2) == 0),
                        BirthDate = RandomDay()
                    }.Save();
                }
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