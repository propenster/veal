using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Veal
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    public class AuthorizeAttribute : Attribute, IAuthProperty
    {
        public string Roles { get; set; } = null;
        public string Scheme { get; set; } = null;
        public Type AuthHandlerType { get; set; }
        public AuthorizeAttribute()
        {
            Scheme = Defaults.JwtBearerAuthScheme;
            AuthHandlerType = typeof(DefaultAuthFilter);


        }
        public AuthorizeAttribute(string scheme = null)
        {
            Scheme = string.IsNullOrWhiteSpace(scheme) ? Defaults.JwtBearerAuthScheme : scheme;
            AuthHandlerType = (scheme == Defaults.JwtBearerAuthScheme || scheme == Defaults.BasicAuthScheme) ? typeof(DefaultAuthFilter) : AuthHandlerType;

        }

    }
}
