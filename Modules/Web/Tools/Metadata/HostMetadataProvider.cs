using Nyan.Core;
using Nyan.Core.Shared;

namespace Nyan.Modules.Web.Tools.Metadata
{
    [Priority(Level = -99)]
    public class NyanMetadataProvider : MetadataProviderPrimitive
    {
        public const string StaticCode = "NYAN";
        public override string Code { get; set; } = StaticCode;
        public override string ContextLocator { get; } = "global";

        public override void Bootstrap()
        {
            Put("nyan.server.host", Configuration.Host, null, true);
            Put("nyan.server.paths.data", Configuration.DataDirectory, null, true);
            Put("nyan.server.paths.base", Configuration.BaseDirectory, null, true);
        }
    }
}