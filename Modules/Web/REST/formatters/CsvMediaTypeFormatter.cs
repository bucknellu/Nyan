using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Nyan.Modules.Web.REST.formatters
{
    // Source: https://github.com/WebApiContrib/WebAPIContrib/blob/master/src/WebApiContrib/Formatting/CSVMediaTypeFormatter.cs

    public class CsvMediaTypeFormatter : MediaTypeFormatter
    {
        public CsvMediaTypeFormatter()
        {
            SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/csv"));

            SupportedEncodings.Add(new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            SupportedEncodings.Add(Encoding.GetEncoding("iso-8859-1"));
        }

        public CsvMediaTypeFormatter(MediaTypeMapping mediaTypeMapping) : this()
        {
            MediaTypeMappings.Add(mediaTypeMapping);
        }

        public CsvMediaTypeFormatter(IEnumerable<MediaTypeMapping> mediaTypeMappings) : this()
        {
            foreach (var mediaTypeMapping in mediaTypeMappings) MediaTypeMappings.Add(mediaTypeMapping);
        }

        public override bool CanReadType(Type type)
        {
            return false;
        }

        public override bool CanWriteType(Type type)
        {
            if (type == null) throw new ArgumentNullException("type");

            return IsTypeOfIEnumerable(type);
        }

        public override Task WriteToStreamAsync(Type type, object value, Stream stream, HttpContent content,
            TransportContext transportContext)
        {
            return Task.Factory.StartNew(() => WriteStream(type, value, stream, content));
        }

        //private utils

        private static void WriteStream(Type type, object value, Stream stream, HttpContent content)
        {
            //NOTE: We have check the type inside CanWriteType method
            //If request comes this far, the type is IEnumerable. We are safe.

            var itemType = type.GetGenericArguments()[0];

            using (var stringWriter = new StringWriter())
            {
                stringWriter.WriteLine(
                    string.Join<string>(
                        ",", itemType.GetProperties().Select(x => x.Name)
                    )
                );

                foreach (var obj in (IEnumerable<object>) value)
                {
                    var vals = obj.GetType().GetProperties().Select(
                        pi => new
                        {
                            Value = pi.GetValue(obj, null)
                        }
                    );

                    var valueLine = string.Empty;

                    foreach (var val in vals)
                        if (val.Value != null)
                        {
                            var _val = val.Value.ToString();

                            //Check if the value contans a comma and place it in quotes if so
                            if (_val.Contains(",")) _val = string.Concat("\"", _val, "\"");

                            //Replace any \r or \n special characters from a new line with a space
                            if (_val.Contains("\r")) _val = _val.Replace("\r", " ");
                            if (_val.Contains("\n")) _val = _val.Replace("\n", " ");

                            valueLine = string.Concat(valueLine, _val, ",");
                        }
                        else
                        {
                            valueLine = string.Concat(valueLine, ",");
                        }

                    stringWriter.WriteLine(valueLine.TrimEnd(','));
                }

                using (var streamWriter = new StreamWriter(stream)) { streamWriter.Write(stringWriter.ToString()); }
            }
        }

        private static bool IsTypeOfIEnumerable(Type type)
        {
            foreach (var interfaceType in type.GetInterfaces()) if (interfaceType == typeof(IEnumerable)) return true;

            return false;
        }
    }
}