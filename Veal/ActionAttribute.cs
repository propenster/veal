using System;
using System.Collections.Generic;
using System.Text;
using static System.Collections.Specialized.BitVector32;
using System.Xml.Linq;

namespace Veal
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Parameter, Inherited = true, AllowMultiple = false)]
    public class ServiceFilterAttribute : Attribute
    {
        public ServiceFilterAttribute(Type type) { }

    }

    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    public class ActionFilterAttribute : Attribute
    {
        public Type FilterType { get; }
        //public string ConstructorParameterValue { get; set; }

        public ActionFilterAttribute(Type filterType)
        {
            FilterType = filterType;
        }
    }
    public interface IAuthProperty
    {
        //string Policy { get; set; }
        
        string Roles { get; set; }
        //
        // Summary:
        //     Gets or sets a comma delimited list of schemes from which user information is
        //     constructed.
        string Scheme { get; set; }
        Type AuthHandlerType { get; set; }
    }
    


    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Parameter, Inherited = true, AllowMultiple = false)]
    public abstract class DataContentAttribute : Attribute
    {
        public  DataContentAttribute()
        {
        }
    }
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Parameter, Inherited = true, AllowMultiple = false)]
    public abstract class RouteParameterAttribute : Attribute
    {
        public RouteParameterAttribute()
        {
        }
    }
    public class QueryParameterAttribute : RouteParameterAttribute
    {
        public QueryParameterAttribute()
        {
        }
    }
    public class PathParameterAttribute : RouteParameterAttribute
    {
        public PathParameterAttribute() { }
    }

    public class JsonBodyAttribute : DataContentAttribute
    {
        public JsonBodyAttribute() : base()
        {
        }
    }
    [AttributeUsage(AttributeTargets.All, Inherited = true, AllowMultiple = false)]
    public abstract class ActionAttribute : Attribute
    {
        public string _Route;
        public string _Name;

        public ActionAttribute(string route, string name)
        {
            _Route = route;
            _Name = name;
        }
    }
    public class GetAttribute : ActionAttribute
    {     
        public GetAttribute(string route, string name) : base(route, name) { }
    }
    public class PostAttribute : ActionAttribute
    {
        public PostAttribute(string route, string name) : base(route, name) { }
    }
}
