using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Filters;
using Nyan.Core.Extensions;
using Nyan.Core.Modules.Identity;
using Nyan.Core.Modules.Log;
using Nyan.Core.Settings;

namespace Nyan.Modules.Web.REST.auth
{
    // ReSharper disable once InconsistentNaming
    public interface IRESTAuthenticationFilter : IAuthenticationFilter, IAuthorizationProvider {}

    public class NyanAuthenticationFilter : IAuthenticationFilter
    {
        private static IRESTAuthenticationFilter CurrentFilter
        {
            get { return Current.Authorization as IRESTAuthenticationFilter; }
        }

        private static readonly NyanPrincipal NyanPrincipalInstance = new NyanPrincipal();

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

            Current.Log.Add(CurrentFilter.GetType().Name + ": AuthenticateAsync", Message.EContentType.MoreInfo);
            return CurrentFilter.AuthenticateAsync(context, cancellationToken);
        }

        public Task ChallengeAsync(HttpAuthenticationChallengeContext context, CancellationToken cancellationToken)
        {
            if (CurrentFilter == null) return Task.FromResult(0);

            Current.Log.Add(CurrentFilter.GetType().Name + ": ChallengeAsync", Message.EContentType.MoreInfo);
            return CurrentFilter.ChallengeAsync(context, cancellationToken);
        }

        public bool AllowMultiple
        {
            get
            {
                if (CurrentFilter == null) return false;

                Current.Log.Add(CurrentFilter.GetType().Name + ": AllowMultiple", Message.EContentType.MoreInfo);
                return CurrentFilter.AllowMultiple;
            }
        }

        public virtual bool CheckPermission(string pCode)
        {
            Current.Log.Add(Current.Authorization.GetType().Name + ": CheckPermission [{0}]".format(pCode), Message.EContentType.MoreInfo);
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
                get
                {
                    return CurrentFilter == null ? null : CurrentFilter.Identity;
                }
            }
        }
    }
}