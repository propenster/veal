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

        public static HttpResponder Ok(object value = null)
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
        public static HttpResponder Unauthorized(object value = null)
        {
            return new HttpResponder
            {
                Value = value ?? string.Empty,
                ContentType = "application/json",
                ContentLength = 500,
                StatusCode = (int)HttpStatusCode.Unauthorized,
                StatusDescription = "401 Unauthorized"
            };
        }
        public static HttpResponder Forbidden(object value = null)
        {
            return new HttpResponder
            {
                Value = value,
                ContentType = "application/json",
                ContentLength = 500,
                StatusCode = (int)HttpStatusCode.Forbidden,
                StatusDescription = "403 Forbidden"
            };
        }
        public static HttpResponder BadRequest(object value = null)
        {
            return new HttpResponder
            {
                Value = value,
                ContentType = "application/json",
                ContentLength = 500,
                StatusCode = (int)HttpStatusCode.BadRequest,
                StatusDescription = "400 BadRequest"
            };
        }
        public override string ToString()
        {
            return Converter.SerializeObject(this);
        }
    }
}
