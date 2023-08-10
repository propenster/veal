using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Text;

namespace Veal
{
    public class HttpResponder //<T> where T : class, new()
    {
        public int StatusCode { get; set; }
        public bool SendChunked { get; set; }
        public string StatusDescription { get; set; }
        public long ContentLength { get; set; }
        public string ContentType { get; set; }
        public Encoding Encoding { get; set; } = Encoding.UTF8;
        public string Language { get; set; } = "en-US";
        public string ContentDisposition { get; set; }
        public object Value { get; set; }
        public bool KeepAlive { get; set; }

        public static HttpResponder Ok(object value)
        {
            return new HttpResponder
            {
                Value = value,
                ContentType = "application/json",
                ContentLength = 500,
                StatusCode = (int)HttpStatusCode.OK,
                StatusDescription = "200 OK"
            };
        }
        public override string ToString()
        {
            return Converter.SerializeObject(this);
        }
    }
}
