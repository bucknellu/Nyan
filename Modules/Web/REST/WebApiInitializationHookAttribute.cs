using System;

namespace Nyan.Modules.Web.REST
{
    [AttributeUsage(AttributeTargets.Class)]
    public class WebApiInitializationHookAttribute : Attribute
    {
    }
}