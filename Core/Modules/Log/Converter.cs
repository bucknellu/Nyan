using System;
using System.Diagnostics;
using Nyan.Core.Extensions;
using Nyan.Core.Factories;

namespace Nyan.Core.Modules.Log
{
    public static class Converter
    {
        public static Message ToMessage(string content, Message.EContentType type = Message.EContentType.Generic)
        {
            var payload = new Message {Content = content, Subject = type.ToString(), Type = type};
            return payload;
        }

        public static Message ToMessage(Exception e)
        {
            return ToMessage(e, null, null);
        }

        public static Message ToMessage(Exception e, string message, string token = null)
        {
            if (token == null) token = Identifier.MiniGuid();

            message = message == null ? e.ToSummary() : $"{message} ({e.ToSummary()})";

            var ctx = $"{token} : {message}";

            return ToMessage(ctx, Message.EContentType.Exception);
        }
    }
}