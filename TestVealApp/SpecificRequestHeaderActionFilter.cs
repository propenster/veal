using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Veal;

namespace TestVealApp
{
    internal class SpecificRequestHeaderActionFilter : IActionFilter
    {
        public HttpResponder OnActionExecution(ActionExecutingContext context)
        {
            context.Request.Headers.TryGetValue("MySpecificHeader", out var authHeader);

            if (string.IsNullOrWhiteSpace(authHeader))
            {
                var errorResponse = new ErrorResponse {ErrorCode = "E01238", Message = $"Invalid headers. Header Key 'MySpecificHeader' must be passed for every request to this endpoint {context.Request.Uri}" };
                context.Result = HttpResponder.Unauthorized(errorResponse);
                //or badRequest... as you may like
                //context.Result = HttpResponder.BadRequest(errorResponse);
                return context.Result;
            }
            return context.Result;

        }
    }
}
