using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Veal
{
    public static class Converter
    {
        public static HttpListenerResponse ToListenerResponse(this HttpResponder responder, HttpListenerResponse resp)
        {

            resp.StatusCode = responder.StatusCode;
            resp.StatusDescription = responder.StatusDescription;
            resp.KeepAlive = responder.KeepAlive;
            resp.ContentEncoding = responder.Encoding;
            resp.ContentType = responder.ContentType;
            resp.SendChunked = responder.SendChunked;

            return resp;

    }
    }
}
