using System;
using System.Collections.Generic;
using System.Text;

namespace Veal
{
    //Dict...
    //key-> routeParameterName => typeof(string)
    //Value -> [DataTypeName, Value]  => typeof(object)
    public class RouteValue
    {
        public string Route { get; set; }
        public string DataTypeName { get; set; }
        public object Value { get; set; }
    }
    public class RouteValueModel
    {
        public string Route { get; set; }
        public Dictionary<string, RouteValue> RouteValues { get; set; } = new Dictionary<string, RouteValue>();
        public string TransformedRouteTemplate { get; set; }
    }
    
}
