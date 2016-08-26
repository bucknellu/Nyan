namespace Nyan.Core.Modules.Data
{
    public class MicroEntityParametrizedGet
    {
        public string OrderBy { get; set; }
        public long PageSize { get; set; }
        public long PageIndex { get; set; }
        public MicroEntityParametrizedGet() { PageIndex = -1; }
    }
}