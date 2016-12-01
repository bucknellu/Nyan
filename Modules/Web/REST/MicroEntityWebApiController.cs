using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;
using Nyan.Core.Extensions;
using Nyan.Core.Modules.Data;
using Nyan.Core.Modules.Log;
using Nyan.Core.Settings;
using Nyan.Core.Wrappers;

namespace Nyan.Modules.Web.REST
{
    [RoutePrefix("api2/entity")]
    public class MicroEntityWebApiController<T> : ApiController where T : MicroEntity<T>
    {
        //Reference resolution cache
        private Dictionary<string, WebApiMicroEntityReferenceAttribute> _entityReferenceAttribute;
        public Dictionary<string, object> Metadata = new Dictionary<string, object>();
        // ReSharper disable once InconsistentNaming
        public Type RESTHeaderClassType = null;
        // ReSharper disable once InconsistentNaming
        public string RESTHeaderQuery = null;

        private Dictionary<string, WebApiMicroEntityReferenceAttribute> EntityReferenceAttribute
        {
            get
            {
                if (_entityReferenceAttribute != null) return _entityReferenceAttribute;

                _entityReferenceAttribute = new Dictionary<string, WebApiMicroEntityReferenceAttribute>();

                //var probe = GetType().GetMethods();

                var probe =
                    Attribute.GetCustomAttributes(GetType(), typeof(WebApiMicroEntityReferenceAttribute)).ToList();

                foreach (var attProbe in probe.Cast<WebApiMicroEntityReferenceAttribute>()) _entityReferenceAttribute.Add(attProbe.MetaName, attProbe);

                return _entityReferenceAttribute;
            }
        }

        public EndpointSecurityAttribute ClassSecurity
        {
            get
            {
                return
                    (EndpointSecurityAttribute)
                    Attribute.GetCustomAttribute(GetType(), typeof(EndpointSecurityAttribute));
            }
        }

        public virtual string SearchResultMoniker
        {
            get { return MicroEntity<T>.Statements.Label; }
        }

        public virtual bool AuthorizeAction(RequestType pRequestType, AccessType pAccessType, string pidentifier,
            ref T pObject, string pContext)
        {
            return true;
        }

        public virtual void PostAction(RequestType pRequestType, AccessType pAccessType, string pidentifier = null,
            T pObject = null, string pContext = null) {}

        private void EvaluateAuthorization(EndpointSecurityAttribute attr, RequestType requestType,
            AccessType accessType, string parm = null, T parm2 = null, string parm3 = null)
        {
            var ret = false;
            var iden = parm ?? (parm2 != null ? parm2.GetEntityIdentifier() : "");
            ;

            if (attr != null)
            {
                var targetPermSet = "";

                switch (accessType)
                {
                    case AccessType.Read:
                        targetPermSet = attr.ReadPermission;
                        break;
                    case AccessType.Write:
                        targetPermSet = attr.WritePermission;
                        break;
                    case AccessType.Remove:
                        targetPermSet = attr.RemovePermission;
                        break;
                }

                if (targetPermSet != null) ret = Current.Authorization.CheckPermission(targetPermSet);
            }

            if (!ret)
                try {
                    ret = AuthorizeAction(requestType, accessType, parm, ref parm2, parm3);
                }
                catch (Exception e) // User may throw a custom error, and that's fine: let's just log it.
                {
                    Current.Log.Add(
                        "AUTH " + typeof(T).FullName + " DENIED " + requestType + "(" + accessType + ") [" + iden +
                        "]. Reason: " + e.Message, Message.EContentType.Warning);
                    throw new UnauthorizedAccessException("Not authorized: " + e.Message);
                }

            if (ret) return;

            Current.Log.Add(
                "Auth " + typeof(T).FullName + " DENIED " + requestType + "(" + accessType + ") [" + iden + "]",
                Message.EContentType.Warning);
            throw new UnauthorizedAccessException("Not authorized.");
        }

        [Route("")]
        [HttpGet]
        public virtual object WebApiGetAll()
        {
            var sw = new Stopwatch();
            var addCount = false;

            try
            {
                EvaluateAuthorization(ClassSecurity, RequestType.GetAll, AccessType.Read, null);

                sw.Start();

                object preRet;
                var parametrizedGet = new MicroEntityParametrizedGet();
                long tot = 0;

                if (RESTHeaderQuery == null)
                {
                    var mustUseParametrizedGet = false;

                    var queryString = Request.GetQueryNameValuePairs().ToDictionary(x => x.Key, x => x.Value);

                    if (queryString.ContainsKey("sort"))
                    {
                        mustUseParametrizedGet = true;
                        parametrizedGet.OrderBy = queryString["sort"];
                    }

                    if (queryString.ContainsKey("page"))
                    {
                        addCount = true;
                        mustUseParametrizedGet = true;
                        parametrizedGet.PageIndex = Convert.ToInt32(queryString["page"]);
                        parametrizedGet.PageSize = queryString.ContainsKey("limit")
                            ? Convert.ToInt32(queryString["limit"])
                            : 50;
                    }

                    if (queryString.ContainsKey("q"))
                    {
                        parametrizedGet.QueryTerm = queryString["q"].ToLower();
                        tot = MicroEntity<T>.Count(parametrizedGet.QueryTerm);
                    }
                    else
                    {
                        tot = MicroEntity<T>.Count();
                    }

                    preRet = mustUseParametrizedGet ? MicroEntity<T>.Get(parametrizedGet) : MicroEntity<T>.Get();
                }
                else
                {
                    if (RESTHeaderClassType != null)
                    {
                        var m = typeof(MicroEntity<T>).GetMethodExt("Query", RESTHeaderClassType);
                        var mg = m.MakeGenericMethod(RESTHeaderClassType);
                        preRet = mg.Invoke(null, new object[] {RESTHeaderQuery});
                    }
                    else
                    {
                        preRet = MicroEntity<T>.Query<object>(RESTHeaderQuery);
                    }
                }

                PostAction(RequestType.GetAll, AccessType.Read);

                sw.Stop();

                if (MicroEntity<T>.TableData.AuditAccess) AuditRequest("ACCESS", typeof(T).FullName + ":ALL");

                Current.Log.Add("GET " + typeof(T).FullName + " OK (" + sw.ElapsedMilliseconds + " ms)");

                var ret = RenderJsonResult(preRet);

                if (addCount)
                {
                    ret.Headers.Add("X-Total-Count", tot.ToString());
                    ret.Headers.Add("X-Total-Pages",
                        (Math.Truncate((double) tot/parametrizedGet.PageSize) + 1).ToString(CultureInfo.InvariantCulture));
                }

                return ret;
            }
            catch (Exception e)
            {
                sw.Stop();
                Current.Log.Add("GET " + typeof(T).FullName + " ERR (" + sw.ElapsedMilliseconds + " ms): " + e.Message,
                    e);
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, e);
            }
        }

        [Route("new")]
        [HttpGet]
        public virtual HttpResponseMessage WebApiGetNew()
        {
            var sw = new Stopwatch();

            try
            {
                sw.Start();

                EvaluateAuthorization(ClassSecurity, RequestType.New, AccessType.Read, null);

                var preRet = (T) Activator.CreateInstance(typeof(T), new object[] {});
                sw.Stop();
                Current.Log.Add("NEW " + typeof(T).FullName + " OK (" + sw.ElapsedMilliseconds + " ms)");

                PostAction(RequestType.New, AccessType.Read);

                return RenderJsonResult(preRet);
            }
            catch (Exception e)
            {
                sw.Stop();
                Current.Log.Add("NEW " + typeof(T).FullName + " ERR (" + sw.ElapsedMilliseconds + " ms): " + e.Message,
                    e);
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, e);
            }
        }

        [Route("{id}")]
        [HttpGet]
        public virtual HttpResponseMessage WebApiGet(string id)
        {
            var sw = new Stopwatch();

            try
            {
                sw.Start();

                EvaluateAuthorization(ClassSecurity, RequestType.Get, AccessType.Read, id);

                var preRet = InternalGet(id);
                if (preRet == null) throw new HttpResponseException(HttpStatusCode.NotFound);

                if (MicroEntity<T>.TableData.AuditAccess) AuditRequest("ACCESS", typeof(T).FullName + ":" + id);

                Current.Log.Add("GET " + typeof(T).FullName + ":" + id + " OK (" + sw.ElapsedMilliseconds + " ms)");

                PostAction(RequestType.Get, AccessType.Read, id, preRet);

                return RenderJsonResult(preRet);
            }
            catch (Exception e)
            {
                sw.Stop();
                Current.Log.Add(
                    "GET " + id + " " + typeof(T).FullName + ":" + id + " ERR (" + sw.ElapsedMilliseconds + " ms): " +
                    e.Message, e);
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, e);
            }
        }

        public virtual T InternalGet(string id)
        {
            return MicroEntity<T>.Get(id);
        }

        [Route("subset")]
        [HttpPost]
        public virtual HttpResponseMessage WebApiGetSetByPost()
        {
            var idset = Request.Content.ReadAsStringAsync().Result;
            return RenderJsonResult(InternalGetSet(idset));
        }

        [Route("subset/{idset}")]
        [HttpGet]
        public virtual HttpResponseMessage WebApiGetSetByGet(string idset)
        {
            return RenderJsonResult(InternalGetSet(idset));
        }

        public virtual List<T> InternalGetSet(string idset)
        {
            if (idset == null) return null;

            var res = new List<T>();

            var sw = new Stopwatch();

            try
            {
                var set = new List<string>();

                if (idset.IndexOf(";", StringComparison.Ordinal) != -1) set = idset.Split(';').ToList();

                if (idset.IndexOf(",", StringComparison.Ordinal) != -1) set = idset.Split(',').ToList();

                if (set.Count == 0)
                {
                    if (idset == "") return res;
                    set = new List<string> {idset};
                }

                sw.Start();

                foreach (var i in set)
                {
                    var probe = InternalGet(i);

                    if (probe != null) res.Add(probe);
                }

                return res;
            }
            catch (Exception e)
            {
                sw.Stop();
                Current.Log.Add(
                    "InternalGetSet " + typeof(T).FullName + ":" + idset + " ERR (" + sw.ElapsedMilliseconds + " ms): " +
                    e.Message, e);
                RenderException(HttpStatusCode.InternalServerError, e.Message);
                return null;
            }
        }

        [Route("")]
        [HttpPost]
        public virtual HttpResponseMessage WebApiPost(T item)
        {
            var sw = new Stopwatch();

            var res = "";
            Request.Content.ReadAsStringAsync().ContinueWith(a => res = a.Result);

            try
            {
                sw.Start();

                EvaluateAuthorization(ClassSecurity, RequestType.Post, AccessType.Write, null, item);
                TryAgentImprinting(ref item);

                if (MicroEntity<T>.TableData.IsReadOnly) throw new HttpResponseException(HttpStatusCode.MethodNotAllowed);

                var preRet = MicroEntity<T>.Get(item.Save());

                if (MicroEntity<T>.TableData.AuditChange) AuditRequest("CHANGE", typeof(T).FullName + ":" + item.GetEntityIdentifier(), item.ToJson());

                Current.Log.Add("UPD " + typeof(T).FullName + ":" + item.GetEntityIdentifier() + " OK (" +
                                sw.ElapsedMilliseconds + " ms)");

                PostAction(RequestType.Post, AccessType.Write, preRet.GetEntityIdentifier(), preRet);

                return RenderJsonResult(preRet);
            }
            catch (Exception e)
            {
                sw.Stop();
                Current.Log.Add(
                    "POST " + typeof(T).FullName + " ERR (" + sw.ElapsedMilliseconds + " ms): " + e.Message, e);
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, e);
            }
        }

        [Route("{id}")]
        [HttpPatch]
        [HttpPut]
        public virtual HttpResponseMessage WebApiPatch(string id, [FromBody] Dictionary<string, object> patchList)
        {
            var sw = new Stopwatch();
            try
            {
                sw.Start();

                var item = MicroEntity<T>.Patch(MicroEntity<T>.Get(id), patchList);

                EvaluateAuthorization(ClassSecurity, RequestType.Patch, AccessType.Write, null, item);
                TryAgentImprinting(ref item);

                if (MicroEntity<T>.TableData.IsReadOnly) throw new HttpResponseException(HttpStatusCode.MethodNotAllowed);

                var preRet = MicroEntity<T>.Get(item.Save());

                if (MicroEntity<T>.TableData.AuditChange) AuditRequest("CHANGE", typeof(T).FullName + ":" + item.GetEntityIdentifier(), item.ToJson());

                Current.Log.Add("PATCH " + typeof(T).FullName + ":" + item.GetEntityIdentifier() + " OK (" +
                                sw.ElapsedMilliseconds + " ms)");

                PostAction(RequestType.Patch, AccessType.Write, preRet.GetEntityIdentifier(), preRet);

                return RenderJsonResult(preRet);
            }
            catch (Exception e)
            {
                sw.Stop();
                Current.Log.Add(
                    "PATCH " + typeof(T).FullName + " ERR (" + sw.ElapsedMilliseconds + " ms): " + e.Message, e);
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, e);
            }
        }

        private static void TryAgentImprinting(ref T item)
        {
            //TODO: Capture current user identifier and write to storage.

            var curPId = Convert.ToInt32(Current.Authorization.Id);

            Current.Log.Add("Imprinting Agent " + curPId);

            if (item == null)
            {
                Current.Log.Add("WARN Imprinting Agent " + curPId + ": No item.", Message.EContentType.Warning);
                return;
            }

            if (item.IsNew()) SetValue(item, "CreatorId", curPId);

            SetValue(item, "LastUpdaterId", curPId);
        }

        // http://stackoverflow.com/a/13270302/1845714
        public static void SetValue(object inputObject, string propertyName, object propertyVal)
        {
            var type = inputObject.GetType();
            var propertyInfo = type.GetProperty(propertyName);

            if (propertyInfo == null) return;

            var targetType = IsNullableType(propertyInfo.PropertyType)
                ? Nullable.GetUnderlyingType(propertyInfo.PropertyType)
                : propertyInfo.PropertyType;
            propertyVal = Convert.ChangeType(propertyVal, targetType);
            propertyInfo.SetValue(inputObject, propertyVal, null);
        }

        private static bool IsNullableType(Type type)
        {
            return type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(Nullable<>));
        }


        private static void AuditRequest(string verb, string target, string content = null)
        {
            //TODO: Create Audit entry here.
        }

        [HttpDelete]
        [Route("{id}")]
        public virtual HttpResponseMessage WebApiDelete(string id)
        {
            var sw = new Stopwatch();

            if (MicroEntity<T>.TableData.IsReadOnly)
                return Request.CreateErrorResponse(HttpStatusCode.MethodNotAllowed,
                    "This entity is market as read-only.");

            T probe = null;

            try
            {
                sw.Start();

                probe = MicroEntity<T>.Get(id);

                EvaluateAuthorization(ClassSecurity, RequestType.Delete, AccessType.Remove, id, probe);

                if (probe == null)
                {
                    sw.Stop();
                    return Request.CreateErrorResponse(HttpStatusCode.NotFound, "No Entity was found for ID " + id);
                }
                sw.Start();
                MicroEntity<T>.Remove(id);
                sw.Stop();
                Current.Log.Add("DEL " + typeof(T).FullName + ":" + id + " OK (" + sw.ElapsedMilliseconds + " ms)");

                if (MicroEntity<T>.TableData.AuditChange) AuditRequest("REMOVAL", typeof(T).FullName + ":" + id, probe.ToJson());

                PostAction(RequestType.Delete, AccessType.Remove, id, probe);

                return Request.CreateResponse(HttpStatusCode.NoContent, "Entity removed successfully.");
            }
            catch (Exception e)
            {
                Current.Log.Add(
                    "DEL " + typeof(T).FullName + ":" + probe.GetEntityIdentifier() + " ERR (" + sw.ElapsedMilliseconds +
                    " ms): " + e.Message, e);
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, e);
            }
        }

        [Route("{id}/reference/{entityReference:alpha}")]
        [HttpGet]
        public virtual HttpResponseMessage GetReference(string id, string entityReference)
        {
            EvaluateAuthorization(ClassSecurity, RequestType.EntityReference, AccessType.Read, id, null, entityReference);

            HttpResponseMessage ret;

            Current.Log.Add("Reference request: {0} for {1}".format(entityReference, id));

            if (!EntityReferenceAttribute.ContainsKey(entityReference))
                ret = Request.CreateErrorResponse(HttpStatusCode.NotFound,
                    "Referenced entity not found [" + entityReference + "].");
            else
                try
                {
                    object referenceCollection;

                    if (id.Equals("new")) referenceCollection = new List<object>();
                    else
                    {
                        var probe = EntityReferenceAttribute[entityReference];
                        var targetType = probe.ForeignType.BaseType.GenericTypeArguments[0];

                        var staMode = targetType.GetMethod("ReferenceQueryByField") == null;

                        Current.Log.Add("RefType: " + targetType);

                        if (staMode) //If the method is Static... (e.g. an Entity)
                        {
                            Current.Log.Add("STATIC " + targetType);
                            dynamic refType = new StaticMembersDynamicWrapper(targetType);
                            referenceCollection = refType.ReferenceQueryByStringField(probe.ForeignProperty, id);
                        }
                        else // we can just instantiate.
                        {
                            Current.Log.Add("DYNAMIC " + targetType);
                            dynamic refType = Activator.CreateInstance(targetType);
                            referenceCollection = refType.ReferenceQueryByStringField(probe.ForeignProperty, id);
                        }
                    }

                    PostAction(RequestType.EntityReference, AccessType.Read, id, null, entityReference);

                    ret = Request.CreateResponse(HttpStatusCode.OK, referenceCollection);
                }
                catch (Exception ex) {
                    ret = Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex);
                }
            return ret;
        }

        public virtual object ReferenceQueryByField(string field, string id)
        {
            var entbag = MicroEntity<T>.GetNewDynamicParameterBag();
            entbag.Add(field, id);

            //var statement = RESTHeaderQuery + " WHERE a." + b.SqlWhereClause;

            var statement = string.Format(
                MicroEntity<T>.Statements.SqlAllFieldsQueryTemplate,
                entbag.SqlWhereClause
            );

            var set = MicroEntity<T>.Query(statement, entbag);
            return set;
        }

        public HttpResponseMessage RenderJsonResult(object contents)
        {
            var ret = new HttpResponseMessage(HttpStatusCode.OK) {Content = new StringContent(contents.ToJson())};
            ret.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            return ret;
        }

        public void RenderException(HttpStatusCode eType, string message)
        {
            var httpError = new HttpError(message);
            var errorResponse = Request.CreateErrorResponse(eType, httpError);
            throw new HttpResponseException(errorResponse);
        }
    }
}