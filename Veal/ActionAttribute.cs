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
    //[AttributeUsage(AttributeTargets.All, Inherited = true, AllowMultiple = false)]
    public class GetAttribute : ActionAttribute
    {
        private string _Route;
        private string _Name;

        public GetAttribute(string route, string name) : base(route, name) { }
        //{
        //    _Route = route;
        //    _Name = name;
        //}
        // property to get name
        public string Name
        {
            get { return _Name; }
        }

        // property to get description
        public string Route
        {
            get { return _Route; }
        }
    }
}
