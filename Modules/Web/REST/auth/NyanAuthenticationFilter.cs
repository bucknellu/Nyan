using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Filters;
using Nyan.Core.Modules.Identity;
using Nyan.Core.Settings;

namespace Nyan.Modules.Web.REST.auth
{
    // ReSharper disable once InconsistentNaming
    public interface IRESTAuthenticationFilter : IAuthenticationFilter, IAuthorizationProvider { }

    public class NyanAuthenticationFilter : IAuthenticationFilter
    {
        private static readonly NyanPrincipal NyanPrincipalInstance = new NyanPrincipal();

        private static IRESTAuthenticationFilter CurrentFilter
        {
            get { return Current.Authorization as IRESTAuthenticationFilter; }
        }

        public IPrincipal Principal
        {
            get { return NyanPrincipalInstance; }
        }

        public IIdentity Identity
        {
            get { return Current.Authorization.Identity; }
        }

        public Task AuthenticateAsync(HttpAuthenticationContext context, CancellationToken cancellationToken)
        {
            if (CurrentFilter == null) return Task.FromResult(0);

            context.Principal = Principal;
            return CurrentFilter.AuthenticateAsync(context, cancellationToken);
        }

        public Task ChallengeAsync(HttpAuthenticationChallengeContext context, CancellationToken cancellationToken)
        {
            return CurrentFilter == null ? Task.FromResult(0) : CurrentFilter.ChallengeAsync(context, cancellationToken);
        }

        public bool AllowMultiple
        {
            get { return CurrentFilter != null && CurrentFilter.AllowMultiple; }
        }

        public virtual bool CheckPermission(string pCode)
        {
            return Current.Authorization.CheckPermission(pCode);
        }

        public class NyanPrincipal : IPrincipal
        {
            public bool IsInRole(string role)
            {
                return Current.Authorization.CheckPermission(role);
            }

            public IIdentity Identity
            {
                get { return CurrentFilter == null ? null : CurrentFilter.Identity; }
            }
        }
    }
}