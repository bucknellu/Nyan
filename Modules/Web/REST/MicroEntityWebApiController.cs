using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Web;
using System.Web.Http;
using Nyan.Core.Extensions;
using Nyan.Core.Modules.Data;
using Nyan.Core.Modules.Identity;
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

        public EndpointSecurityAttribute ClassSecurity => (EndpointSecurityAttribute) Attribute.GetCustomAttribute(GetType(), typeof(EndpointSecurityAttribute));

        public EndpointBehaviorAttribute ClassBehavior => (EndpointBehaviorAttribute) Attribute.GetCustomAttribute(GetType(), typeof(EndpointBehaviorAttribute));

        public virtual string SearchResultMoniker => MicroEntity<T>.Statements.Label;

        public virtual bool AuthorizeAction(RequestType pRequestType, AccessType pAccessType, string pidentifier, ref T pObject, string pContext) { return true; }

        public virtual void PostAction(RequestType pRequestType, AccessType pAccessType, string pidentifier = null, T pObject = null, string pContext = null) { }

        private void EvaluateAuthorization(EndpointSecurityAttribute attr, RequestType requestType, AccessType accessType, string parm = null, T parm2 = null, string parm3 = null)
        {
            var ret = true;
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

                ret = Helper.HasAnyPermissions(targetPermSet);
            }

            if (ret)
                try { ret = AuthorizeAction(requestType, accessType, parm, ref parm2, parm3); } catch (Exception e) // User may throw a custom error, and that's fine: let's just log it.
                {
                    Current.Log.Add($"AUTH {typeof(T).FullName} DENIED {requestType}({accessType}) [{iden}]. Reason: {e.Message}", Message.EContentType.Warning);
                    throw new UnauthorizedAccessException("Not authorized: " + e.Message);
                }

            if (ret) return;

            Current.Log.Add("Auth " + typeof(T).FullName + " DENIED " + requestType + "(" + accessType + ") [" + iden + "]", Message.EContentType.Warning);
            throw new UnauthorizedAccessException("Not authorized.");
        }

        [Route(""), HttpGet]
        public virtual object WebApiGetAll() { return WebApiGetAll(null); }

        public virtual object WebApiGetAll(string extraParms)
        {
            try
            {
                var mRet = InternalGetAll(extraParms);

                var ret = ControllerHelper.RenderJsonResult(mRet.Content);

                ControllerHelper.ProcessPipelineHeaders<T>(ret.Headers);
                ControllerHelper.HandleHeaders(ret.Headers, GetAccessHeadersByPermission());

                if (!mRet.HasExtendedProperties) return ret;

                ret.Headers.Add("X-Total-Count", mRet.TotalCount.ToString());
                ret.Headers.Add("X-Total-Pages", mRet.TotalPages.ToString());

                return ret;
            } catch (Exception e)
            {
                Current.Log.Add(e);
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, e);
            }
        }

        private Dictionary<string, object> GetAccessHeadersByPermission()
        {
            var ret = new Dictionary<string, object>
            {
                {"read", ClassSecurity == null || Helper.HasAnyPermissions(ClassSecurity.ReadPermission)},
                {"write", ClassSecurity == null || Helper.HasAnyPermissions(ClassSecurity.WritePermission)},
                {"remove", ClassSecurity == null || Helper.HasAnyPermissions(ClassSecurity.RemovePermission)}
            };

            return new Dictionary<string, object> {{"x-baf-access", ret}};
        }

        internal InternalGetAllPayload InternalGetAll(string extraParms = null) { return InternalGetAll<T>(extraParms); }

        public virtual InternalGetAllPayload InternalGetAll<TU>(string extraParms = null)
        {
            var ret = new InternalGetAllPayload();

            var sw = new Stopwatch();

            try
            {
                EvaluateAuthorization(ClassSecurity, RequestType.GetAll, AccessType.Read, null);

                sw.Start();

                var parametrizedGet = new MicroEntityParametrizedGet();
                long tot = 0;

                var mustUseParametrizedGet = false;

                var queryString = Request?.GetQueryNameValuePairs().ToDictionary(x => x.Key, x => x.Value) ?? new Dictionary<string, string>();

                if (queryString.ContainsKey("sort"))
                {
                    mustUseParametrizedGet = true;
                    parametrizedGet.OrderBy = queryString["sort"];
                }

                if (queryString.ContainsKey("page"))
                {
                    ret.HasExtendedProperties = true;
                    mustUseParametrizedGet = true;
                    parametrizedGet.PageIndex = Convert.ToInt32(queryString["page"]);
                    parametrizedGet.PageSize = queryString.ContainsKey("limit") ? Convert.ToInt32(queryString["limit"]) : 50;
                }

                if (queryString.ContainsKey("filter"))
                {
                    mustUseParametrizedGet = true;
                    parametrizedGet.Filter = queryString["filter"];
                }

                if (queryString.ContainsKey("q"))
                {
                    mustUseParametrizedGet = true;
                    parametrizedGet.QueryTerm = queryString["q"].ToLower();
                }

                tot = mustUseParametrizedGet ? MicroEntity<T>.Count(parametrizedGet, extraParms) : MicroEntity<T>.Count();

                var preRet = mustUseParametrizedGet ? MicroEntity<T>.Get<TU>(parametrizedGet, extraParms) : MicroEntity<T>.GetAll<TU>();

                var oPreRet = (object) preRet;

                PostCollectionAction(RequestType.GetAll, AccessType.Read, ref oPreRet, ref tot);
                PostAction(RequestType.GetAll, AccessType.Read);

                sw.Stop();

                if (MicroEntity<T>.TableData.AuditAccess) AuditRequest("ACCESS", typeof(T).FullName + ":ALL");

                Current.Log.Add("GET " + typeof(T).FullName + " OK (" + sw.ElapsedMilliseconds + " ms)");

                ret.Content = preRet.Select(i => (object) i);
                ret.PageSize = parametrizedGet.PageSize;
                ret.TotalCount = tot;

                return ret;
            } catch (Exception e)
            {
                sw.Stop();
                Current.Log.Add("GET " + typeof(T).FullName + " ERR (" + sw.ElapsedMilliseconds + " ms): " + e.Message, e);
                throw e;
            }
        }

        public virtual void PostCollectionAction(RequestType pRequestType, AccessType pAccessType,
                                                 ref object collectionList, ref long totalRecords, string pidentifier = null, T pObject = null,
                                                 string pContext = null) { }

        [Route("summary"), HttpGet]
        public virtual object WebApiGetSummary()
        {
            var summaryType = typeof(T);

            var step = "Initializing";
            try
            {
                if (ClassBehavior != null)
                    if (ClassBehavior.SummaryType != null)
                        summaryType = ClassBehavior.SummaryType;

                var method = GetType().GetMethod("InternalGetAll", BindingFlags.Public | BindingFlags.Instance);
                var genericMethod = method.MakeGenericMethod(summaryType);
                var preRet = (InternalGetAllPayload) genericMethod.Invoke(this, new object[] {null});

                step = "Preparing JSON payload";
                var ret = ControllerHelper.RenderJsonResult(preRet.Content);

                ControllerHelper.ProcessPipelineHeaders<T>(ret.Headers);

                if (!preRet.HasExtendedProperties) return ret;

                step = "Adding extra data";
                ret.Headers.Add("X-Total-Count", preRet.TotalCount.ToString());
                ret.Headers.Add("X-Total-Pages", preRet.TotalPages.ToString());

                return ret;
            } catch (Exception e)
            {
                Current.Log.Add(e, step);

                throw e;
            }
        }

        [Route("new"), HttpGet]
        public virtual HttpResponseMessage WebApiGetNew()
        {
            var sw = new Stopwatch();

            try
            {
                sw.Start();

                EvaluateAuthorization(ClassSecurity, RequestType.New, AccessType.Read, null);

                var preRet = (T) Activator.CreateInstance(typeof(T), new object[] { });
                sw.Stop();
                Current.Log.Add("NEW " + typeof(T).FullName + " OK (" + sw.ElapsedMilliseconds + " ms)");

                PostAction(RequestType.New, AccessType.Read);

                return ControllerHelper.RenderJsonResult(preRet);
            } catch (Exception e)
            {
                sw.Stop();
                Current.Log.Add("NEW " + typeof(T).FullName + " ERR (" + sw.ElapsedMilliseconds + " ms): " + e.Message,
                                e);
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, e);
            }
        }

        [Route("{id}"), HttpGet]
        public virtual HttpResponseMessage WebApiGet(string id)
        {
            var sw = new Stopwatch();

            try
            {
                sw.Start();

                EvaluateAuthorization(ClassSecurity, RequestType.Get, AccessType.Read, id);

                var preRet = InternalGet(id);
                if (preRet == null) return Request.CreateResponse(HttpStatusCode.NotFound);

                if (MicroEntity<T>.TableData.AuditAccess) AuditRequest("ACCESS", typeof(T).FullName + ":" + id);

                Current.Log.Add("GET " + typeof(T).FullName + ":" + id + " OK (" + sw.ElapsedMilliseconds + " ms)");

                PostAction(RequestType.Get, AccessType.Read, id, preRet);

                var postRet = InternalPostGet(preRet);

                return ControllerHelper.RenderJsonResult(postRet);
            } catch (Exception e)
            {
                sw.Stop();
                Current.Log.Add(
                    "GET " + id + " " + typeof(T).FullName + ":" + id + " ERR (" + sw.ElapsedMilliseconds + " ms): " +
                    e.Message, e);

                if (!(e is HttpException)) return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, e);

                var we = (HttpException) e;
                return Request.CreateErrorResponse((HttpStatusCode) we.GetHttpCode(), we.Message);
            }
        }

        public virtual object InternalPostGet(T source) { return source; }

        public virtual T InternalGet(string id) { return MicroEntity<T>.Get(id); }

        [Route("subset"), HttpPost]
        public virtual HttpResponseMessage WebApiGetSetByPost()
        {
            var idset = Request.Content.ReadAsStringAsync().Result;
            return ControllerHelper.RenderJsonResult(InternalGetSet(idset));
        }

        [Route("subset/{idset}"), HttpGet]
        public virtual HttpResponseMessage WebApiGetSetByGet(string idset) { return ControllerHelper.RenderJsonResult(InternalGetSet(idset)); }

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
            } catch (Exception e)
            {
                sw.Stop();
                Current.Log.Add(
                    "InternalGetSet " + typeof(T).FullName + ":" + idset + " ERR (" + sw.ElapsedMilliseconds + " ms): "
                    + e.Message, e);
                ControllerHelper.RenderException(HttpStatusCode.InternalServerError, e.Message, Request);
                return null;
            }
        }

        [Route(""), HttpPost]
        public virtual HttpResponseMessage WebApiPost(T item)
        {
            var originalId = item.GetEntityIdentifier();

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

                if (preRet != null)
                {
                    if (MicroEntity<T>.TableData.AuditChange) AuditRequest("CHANGE", typeof(T).FullName + ":" + item.GetEntityIdentifier(), item.ToJson());

                    Current.Log.Add("UPD " + typeof(T).FullName + ":" + item.GetEntityIdentifier() + " OK ("
                                    + sw.ElapsedMilliseconds + " ms)");

                    PostAction(RequestType.Post, AccessType.Write, preRet.GetEntityIdentifier(), preRet, preRet.GetEntityIdentifier() != originalId ? "CREATE" : "UPDATE");

                    return ControllerHelper.RenderJsonResult(preRet);
                }

                Current.Log.Add("UPD " + typeof(T).FullName + ":" + item.GetEntityIdentifier() + " ACCEPTED (" + sw.ElapsedMilliseconds + " ms)");
                return new HttpResponseMessage(HttpStatusCode.Accepted);
            } catch (Exception e)
            {
                sw.Stop();
                Current.Log.Add(
                    "POST " + typeof(T).FullName + " ERR (" + sw.ElapsedMilliseconds + " ms): " + e.Message, e);
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, e);
            }
        }

        [Route("{id}"), HttpPatch, HttpPut]
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

                return ControllerHelper.RenderJsonResult(preRet);
            } catch (Exception e)
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

        private static bool IsNullableType(Type type) { return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>); }

        private static void AuditRequest(string verb, string target, string content = null)
        {
            //TODO: Create Audit entry here.
        }

        [HttpDelete, Route("{id}")]
        public virtual HttpResponseMessage WebApiDelete(string id)
        {
            var sw = new Stopwatch();

            if (MicroEntity<T>.TableData.IsReadOnly) return Request.CreateErrorResponse(HttpStatusCode.MethodNotAllowed, "This entity is market as read-only.");

            T probe = null;

            try
            {
                sw.Start();

                probe = MicroEntity<T>.Get(id);

                EvaluateAuthorization(ClassSecurity, RequestType.Delete, AccessType.Remove, id, probe);

                sw.Start();
                var ret = MicroEntity<T>.Remove(id);

                if (probe == null)
                {
                    sw.Stop();
                    return Request.CreateErrorResponse(HttpStatusCode.NotFound, "No Entity was found for ID " + id);
                }

                sw.Stop();

                if (ret)
                {
                    Current.Log.Add("DEL " + typeof(T).FullName + ":" + id + " OK (" + sw.ElapsedMilliseconds + " ms)");

                    if (MicroEntity<T>.TableData.AuditChange) AuditRequest("REMOVAL", typeof(T).FullName + ":" + id, probe.ToJson());

                    PostAction(RequestType.Delete, AccessType.Remove, id, probe);

                    return Request.CreateResponse(HttpStatusCode.NoContent, "Entity removed successfully.");
                }

                Current.Log.Add("DEL " + typeof(T).FullName + ":" + id + " ACCEPTED (" + sw.ElapsedMilliseconds + " ms)");
                return new HttpResponseMessage(HttpStatusCode.Accepted);
            } catch (Exception e)
            {
                Current.Log.Add(
                    "DEL " + typeof(T).FullName + ":" + probe.GetEntityIdentifier() + " ERR (" + sw.ElapsedMilliseconds
                    +
                    " ms): " + e.Message, e);
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, e);
            }
        }

        [Route("{id}/reference/{entityReference:alpha}"), HttpGet]
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

                    if (id.Equals("new"))
                    {
                        referenceCollection = new List<object>();
                    }
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
                } catch (Exception ex) { ret = Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex); }

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

        public class InternalGetAllPayload
        {
            private long _pageSize;
            private long _totalCount;
            public bool HasExtendedProperties;
            public IEnumerable<object> Content { get; set; }
            public long TotalCount
            {
                get => _totalCount;
                set
                {
                    _totalCount = value;
                    CalcPages();
                }
            }
            public long TotalPages { get; set; }
            public long PageSize
            {
                get => _pageSize;
                set
                {
                    _pageSize = value;
                    CalcPages();
                }
            }

            private void CalcPages()
            {
                if (TotalCount == 0)
                {
                    TotalPages = 0;
                    return;
                }

                if (PageSize == 0)
                {
                    _pageSize = TotalCount;
                    return;
                }

                TotalPages = Convert.ToInt32(Math.Truncate((double) TotalCount / PageSize) + 1);
            }
        }
    }
}