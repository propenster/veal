using Jil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace Veal
{
    public static class Converter
    {
        public static string SerializeObject(object value)
        {
                      
            using (var output = new StringWriter())
            {
                JSON.Serialize(
                   value,
                    output,
                    options: new Options(dateFormat: DateTimeFormat.ISO8601)

                );
                return output.ToString();
            }
        }
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
