using System;
using System.Collections.Generic;
using System.Text;
using static System.Collections.Specialized.BitVector32;
using System.Xml.Linq;

namespace Veal
{
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
}
