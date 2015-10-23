using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(Nyan.WebSample.Startup))]
namespace Nyan.WebSample
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
        }
    }
}