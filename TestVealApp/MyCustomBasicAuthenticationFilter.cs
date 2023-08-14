using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Veal;

namespace TestVealApp
{
    internal class MyCustomBasicAuthenticationFilter : IAuthorizationFilter
    {
        public HttpResponder OnAuthentication(ActionExecutingContext context, string scheme)
        {
            throw new NotImplementedException();
        }
    }
}
