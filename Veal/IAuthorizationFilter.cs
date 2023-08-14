using System;
using System.Collections.Generic;
using System.Text;

namespace Veal
{
    public interface IAuthorizationFilter
    {
        HttpResponder OnAuthentication(ActionExecutingContext context, string scheme);

    }
}
