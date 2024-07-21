using System;
using System.Collections.Generic;
using System.Text;

namespace Veal
{
    public interface ICoreFilter { }
    public interface IAuthorizationFilter : ICoreFilter
    {
        HttpResponder OnAuthentication(ActionExecutingContext context, string scheme);

    }
}
