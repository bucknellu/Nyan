using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.ServiceModel.Syndication;
using System.Threading.Tasks;
using System.Xml;

namespace Nyan.Modules.Web.REST.RSS
{
    public class SyndicationFeedFormatter : MediaTypeFormatter
    {
        private readonly string atom = "application/atom+xml";
        private readonly string rss = "application/rss+xml";

        private readonly Func<Type, bool> SupportedType = type =>
        {
            if (type == typeof(UrlRepository)) return true;
            return false;
        };

        public SyndicationFeedFormatter()
        {
            SupportedMediaTypes.Add(new MediaTypeHeaderValue(atom));
            SupportedMediaTypes.Add(new MediaTypeHeaderValue(rss));
        }

        public override bool CanReadType(Type type) { return SupportedType(type); }

        public override bool CanWriteType(Type type) { return SupportedType(type); }

        public override Task WriteToStreamAsync(Type type, object value, Stream writeStream, HttpContent content, TransportContext transportContext)
        {
            return Task.Factory.StartNew(() =>
            {
                if (type == typeof(UrlRepository)) BuildSyndicationFeed((UrlRepository)value, writeStream, content.Headers.ContentType.MediaType);
            });
        }

        private void BuildSyndicationFeed(UrlRepository model, Stream stream, string contenttype)
        {
            var models = model.Items;

            var items = new List<SyndicationItem>();
            var feed = new SyndicationFeed
            {
                Title = new TextSyndicationContent(model.Title)
            };

            foreach (var url in models) items.Add(BuildSyndicationItem(url));

            feed.Items = items;

            using (var writer = XmlWriter.Create(stream))
            {
                if (string.Equals(contenttype, atom))
                {
                    var atomformatter = new Atom10FeedFormatter(feed);
                    atomformatter.WriteTo(writer);
                }
                else
                {
                    var rssformatter = new Rss20FeedFormatter(feed);
                    rssformatter.WriteTo(writer);
                }
            }
        }

        private SyndicationItem BuildSyndicationItem(Url u)
        {
            var item = new SyndicationItem
            {
                Title = new TextSyndicationContent(u.Title),
                BaseUri = new Uri(u.Address),
                LastUpdatedTime = u.CreatedAt,
                Content = new TextSyndicationContent(u.Description)
            };
            item.Authors.Add(new SyndicationPerson { Name = u.CreatedBy });
            return item;
        }
    }
}