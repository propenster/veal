using System;
using System.Collections.Generic;
using System.Text;

namespace Veal
{
    internal class PortUnavailableException : Exception
    {
        public PortUnavailableException(string message, Exception innerEx) : base(string.Format("Port binding already in use on machine {0}", message), innerEx)
        {
            
        }
        public PortUnavailableException(Exception innerEx) : base(string.Format("Port binding already in use on machine {0}", innerEx.Message), innerEx)
        {

        }
    }
    internal class PipelineException : Exception
    {
        public PipelineException(string message, Exception innerEx) : base(string.Format("An error occurred while treating the request {0}", message), innerEx)
        {

        }
        public PipelineException(Exception innerEx) : base(string.Format("An error occurred while treating the request {0}", innerEx.Message), innerEx)
        {

        }
    }
}
