using Kayak.Http;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Veal
{
    public class ActionExecutingContext
    {
        public HttpAppServer App { get; set; }
        public HttpRequestHead Request { get; set; }
        public string Body { get; set; } = string.Empty;
        public HttpResponder Result { get; set; } = null;

    }
    public interface IActionFilter : ICoreFilter
    {
        HttpResponder OnActionExecution(ActionExecutingContext context /**, ActionExecutionDelegate next**/);
    }
}
