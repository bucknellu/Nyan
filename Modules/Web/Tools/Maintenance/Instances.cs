using System;
using System.Collections.Generic;
using Nyan.Core.Assembly;
using Nyan.Core.Extensions;

namespace Nyan.Modules.Web.Tools.Maintenance
{
    public static class Instances
    {
        internal static readonly IEnumerable<Type> RegisteredMaintenanceTaskTypes =
            Management.GetClassesByInterface<IMaintenanceTask>();

        internal static readonly Type MaintenanceEventHandlerType = Management.GetClassesByInterface<IMaintenanceEventHandler>()[0];

        private static IMaintenanceEventHandler _handler;

        internal static IMaintenanceEventHandler Handler
        {
            get
            {
                if (_handler != null) return _handler;

                _handler = MaintenanceEventHandlerType.CreateInstance<IMaintenanceEventHandler>();
                return _handler;
            }
        }
    }
}