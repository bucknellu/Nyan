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
            var a = Model.User.GetAll().ToList();

            if (a.Count < 100)
            {
                for (int i = 0; i < 128; i++)
                {
                    new Model.User
                    {
                        Name = Faker.Name.FullName(),
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