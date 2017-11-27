using System.Web.Http;

namespace Nyan.Modules.Web.REST.CORS
{
    public static class Setup
    {
        public static void AddCustomCorsFactory(this HttpConfiguration config)
        {
            config.SetCorsPolicyProviderFactory(new ConfigBasedPolicyProviderFactory());
            config.EnableCors();
        }
    }
}