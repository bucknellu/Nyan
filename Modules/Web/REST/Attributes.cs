using System;

namespace Nyan.Modules.Web.REST
{
    public enum RequestType
    {
        GetAll,
        New,
        Get,
        Post,
        Patch,
        Put,
        Delete,
        EntityReference
    }

    public enum AccessType
    {
        Read,
        Write,
        Remove
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class EndpointSecurityAttribute : Attribute
    {
        public string ReadPermission { get; set; }
        public string WritePermission { get; set; }
        public string RemovePermission { get; set; }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class EndpointBehaviorAttribute : Attribute
    {
        public Type SummaryType { get; set; }
        public bool MustPaginate { get; set; }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class WebApiMicroEntityReferenceAttribute : Attribute
    {
        public WebApiMicroEntityReferenceAttribute(string metaName, string foreignProperty, Type foreignType,
            string localProperty = null)
        {
            MetaName = metaName;
            ForeignProperty = foreignProperty;
            ForeignType = foreignType;
            LocalProperty = localProperty;
        }

        public string MetaName { get; set; }
        public string ForeignProperty { get; set; }
        public Type ForeignType { get; set; }
        public string LocalProperty { get; set; }
    }
}