using System;
using System.Linq;
using Nyan.Core.Settings;

namespace Nyan.Core.Modules.Identity
{
    public static class Helper
    {
        public static bool HasAnyPermissions(string perm)
        {
            if (perm == null) return true;
            if (perm == "") return true;

            var separator = ',';

            if (perm.IndexOf(";", StringComparison.Ordinal) != -1) separator = ';';
            if (perm.IndexOf(":", StringComparison.Ordinal) != -1) separator = ':';

            while (perm.IndexOf(" ", StringComparison.Ordinal) != -1) perm = perm.Replace(" ", ""); // Eliminates spaces

            var queryPerms = perm.Split(separator);

            return queryPerms.Any(s => Current.Authorization.CheckPermission(s));
        }
    }
}