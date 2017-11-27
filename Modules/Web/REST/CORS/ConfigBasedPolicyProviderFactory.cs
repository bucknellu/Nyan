using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Cors;
using System.Web.Http.Cors;
using Nyan.Core.Extensions;
using Nyan.Core.Modules.Log;
using Nyan.Core.Settings;

namespace Nyan.Modules.Web.REST.CORS
{
    public class ConfigBasedPolicyProviderFactory : ICorsPolicyProviderFactory
    {
        private readonly Dictionary<string, CorsPolicy> _cache = new Dictionary<string, CorsPolicy>();

        public ICorsPolicyProvider GetCorsPolicyProvider(HttpRequestMessage request)
        {
            var policy = GetPolicyForControllerAndOrigin(request.GetCorsRequestContext());
            return new CustomPolicyProvider(policy);
        }

        public bool IsValidOrigin(string origin)
        {
            foreach (var pattern in Current.WebApiCORSDomainMasks)
                if (origin.MatchWildcardPattern(pattern))
                {
                    Current.Log.Add($"CORS Domain match: [{origin}] => [{pattern}] ", Message.EContentType.StartupSequence);
                    return true;
                }

            Current.Log.Add($"CORS Domain match FAIL: [{origin}]", Message.EContentType.StartupSequence);
            return false;
        }

        private CorsPolicy GetPolicyForControllerAndOrigin(CorsRequestContext corsRequestContext)
        {
            if (_cache.ContainsKey(corsRequestContext.Origin)) return _cache[corsRequestContext.Origin];

            CorsPolicy policy = null;

            if (IsValidOrigin(corsRequestContext.Origin))
            {
                policy = new CorsPolicy();
                policy.Origins.Add(corsRequestContext.Origin);
                policy.Methods.Add("OPTIONS");
                policy.Methods.Add("GET");
                policy.Methods.Add("POST");
                policy.Methods.Add("PUT");
                policy.Methods.Add("DELETE");

                policy.AllowAnyHeader = true;

                policy.SupportsCredentials = true;
            }

            _cache[corsRequestContext.Origin] = policy;

            return policy;
        }
    }

    public class CustomPolicyProvider : ICorsPolicyProvider
    {
        private readonly CorsPolicy _policy;

        public CustomPolicyProvider(CorsPolicy policy) { _policy = policy; }

        public Task<CorsPolicy> GetCorsPolicyAsync(HttpRequestMessage request, CancellationToken cancellationToken) { return Task.FromResult(_policy); }
    }
}