using MongoDB.Bson;
using Nyan.Core.Modules.Data;

namespace Nyan.Modules.Data.MongoDB
{
    public static class Extensions
    {
        public static BsonDocument ToBsonQuery(this MicroEntityParametrizedGet parm, string extraParms = null)
        {
            string query = null;

            BsonDocument queryFilter;

            if (!string.IsNullOrEmpty(parm.QueryTerm)) query = $"$text:{{$search: \'{parm.QueryTerm.Replace("'", "\\'")}\',$caseSensitive: false,$diacriticSensitive: false}}";

            if (extraParms != null)
            {
                extraParms = extraParms.Trim();

                if (extraParms[0] == '{') extraParms = extraParms.Substring(1, extraParms.Length - 2);

                if (query != null) query += ",";
                query += extraParms;
            }

            if (!string.IsNullOrEmpty(parm.Filter))
            {
                if (parm.Filter[0] == '{') parm.Filter = parm.Filter.Substring(1, parm.Filter.Length - 2);
                if (query != null) query += ",";
                query += parm.Filter;
            }

            if (query != null)
            {
                if (query[0] != '{') query = "{" + query + "}";

                queryFilter = BsonDocument.Parse(query);
            }
            else { queryFilter = new BsonDocument(); }

            return queryFilter;
        }

        public static BsonDocument ToBsonFilter(this MicroEntityParametrizedGet parm)
        {
            var sortFilter = new BsonDocument();

            if (parm.OrderBy == null) return sortFilter;

            var sign = parm.OrderBy[0];
            var deSignedValue = parm.OrderBy.Substring(1);

            int dir;
            string field;

            switch (sign)
            {
                case '+':
                    field = deSignedValue;
                    dir = +1;
                    break;
                case '-':
                    field = deSignedValue;
                    dir = -1;
                    break;
                default:
                    field = parm.OrderBy;
                    dir = +1;
                    break;
            }

            sortFilter = new BsonDocument(field, dir);

            return sortFilter;
        }
    }
}