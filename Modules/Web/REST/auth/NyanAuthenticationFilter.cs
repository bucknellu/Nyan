using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Filters;
using Nyan.Core.Extensions;
using Nyan.Core.Settings;

namespace Nyan.Modules.Web.REST.auth
{
    public class NyanAuthenticationFilter : IAuthenticationFilter
    {
        public virtual Task AuthenticateAsync(HttpAuthenticationContext context, CancellationToken cancellationToken)
        {
            return Task.FromResult(0);
        }

        public virtual Task ChallengeAsync(HttpAuthenticationChallengeContext context, CancellationToken cancellationToken)
        {
            var challenge = new AuthenticationHeaderValue("Basic");
            context.Result = new AddChallengeOnUnauthorizedResult(challenge, context.Result);
            return Task.FromResult(0);
        }

        public bool AllowMultiple { get; private set; }

        public virtual bool CheckPermission(string pCode)
        {
            return true;
        }

        public virtual void Shutdown() {}
    }
}