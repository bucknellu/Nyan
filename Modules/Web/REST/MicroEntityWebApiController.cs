using Nyan.Core.Extensions;
using Nyan.Core.Modules.Data;
using Nyan.Core.Modules.Data.Adapter;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;

namespace Nyan.Modules.Web.REST
{
    [RoutePrefix("api2/entity")]
    public class EntityWebApiController<T> : ApiController where T : MicroEntity<T>
    {
        // ReSharper disable once InconsistentNaming
        public Type RESTHeaderClassType = null;
        public string RESTHeaderQuery = null;

        //Reference resolution cache
        private Dictionary<string, WebApiEntityReferenceAttribute> _entityReferenceAttribute;

        private Dictionary<string, WebApiEntityReferenceAttribute> EntityReferenceAttribute
        {
            get
            {
                if (_entityReferenceAttribute != null)
                    return _entityReferenceAttribute;

                _entityReferenceAttribute = new Dictionary<string, WebApiEntityReferenceAttribute>();

                //var probe = GetType().GetMethods();

                var probe = Attribute.GetCustomAttributes(GetType(), typeof(WebApiEntityReferenceAttribute)).ToList();

                foreach (var att in probe)
                {
                    var attProbe = (WebApiEntityReferenceAttribute)att;
                    _entityReferenceAttribute.Add(attProbe.MetaName, attProbe);
                }

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

        [Route("")]
        [HttpGet]
        public virtual object WebApiGetAll()
        {
            if (ClassSecurity != null &&
                ClassSecurity.ReadPermission != null &&
                !true)//TODO: This is the entry point for Authorization engines.
            {


                Nyan.Core.Settings.Current.Log.Add("ClassSecurity.ReadPermission: " + ClassSecurity.ReadPermission + ". Environment.Current: " +
                        Nyan.Core.Settings.Current.Environment.CurrentCode);
                throw new UnauthorizedAccessException("Insufficient permissions.");
            }

            var sw = new Stopwatch();
            sw.Start();

            try
            {
                object preRet;
                if (RESTHeaderQuery == null)
                {
                    preRet = MicroEntity<T>.GetAll();
                }
                else
                {
                    if (RESTHeaderClassType != null)
                    {
                        var m = typeof(MicroEntity<T>).GetMethodExt("Query", RESTHeaderClassType);
                        var mg = m.MakeGenericMethod(RESTHeaderClassType);
                        preRet = mg.Invoke(null, new object[] { RESTHeaderQuery });
                    }
                    else
                    {
                        preRet = MicroEntity<T>.Query<object>(RESTHeaderQuery);
                    }
                }

                sw.Stop();

                if (MicroEntity<T>.TableData.AuditAccess)
                    AuditRequest("ACCESS", typeof(T).FullName + ":ALL");

                Nyan.Core.Settings.Current.Log.Add("  GET " + typeof(T).FullName + " OK (" + sw.ElapsedMilliseconds + " ms)");

                return RenderJsonResult(preRet);
            }
            catch (Exception e)
            {
                sw.Stop();
                Nyan.Core.Settings.Current.Log.Add("  GET " + typeof(T).FullName + " ERR (" + sw.ElapsedMilliseconds + " ms): " + e.Message, e);
                throw;
            }
        }

        [Route("new")]
        [HttpGet]
        public virtual HttpResponseMessage WebApiGetNew()
        {
            var sw = new Stopwatch();
            sw.Start();

            try
            {
                var preRet = (T)Activator.CreateInstance(typeof(T), new object[] { });
                sw.Stop();
                Nyan.Core.Settings.Current.Log.Add("  NEW " + typeof(T).FullName + " OK (" + sw.ElapsedMilliseconds + " ms)");

                return RenderJsonResult(preRet);
            }
            catch (Exception e)
            {
                sw.Stop();
                Nyan.Core.Settings.Current.Log.Add("  NEW " + typeof(T).FullName + " ERR (" + sw.ElapsedMilliseconds + " ms): " + e.Message,
                    e);
                throw;
            }
        }

        [Route("{id}")]
        [HttpGet]
        public virtual HttpResponseMessage WebApiGet(string id)
        {
            if (ClassSecurity != null &&
                ClassSecurity.ReadPermission != null &&
                !true)//TODO: This is the entry point for Authorization engines.
                throw new UnauthorizedAccessException("Insufficient permissions.");

            var sw = new Stopwatch();
            sw.Start();

            try
            {
                var preRet = MicroEntity<T>.Get(id);
                if (preRet == null) throw new HttpResponseException(HttpStatusCode.NotFound);

                if (MicroEntity<T>.TableData.AuditAccess)
                    AuditRequest("ACCESS", typeof(T).FullName + ":" + id);

                Nyan.Core.Settings.Current.Log.Add("  GET " + typeof(T).FullName + ":" + id + " OK (" + sw.ElapsedMilliseconds + " ms)");

                return RenderJsonResult(preRet);
            }
            catch (Exception e)
            {
                sw.Stop();
                Nyan.Core.Settings.Current.Log.Add(
                    "  GET " + id + " " + typeof(T).FullName + ":" + id + " ERR (" + sw.ElapsedMilliseconds + " ms): " +
                    e.Message, e);
                throw;
            }
        }

        [Route("")]
        [HttpPost]
        public virtual HttpResponseMessage WebApiPost(T item)
        {
            if (ClassSecurity != null &&
                ClassSecurity.WritePermission != null &&
                !true) //TODO: This is the entry point for Authorization engines.
                throw new UnauthorizedAccessException("Insufficient permissions.");

            var sw = new Stopwatch();
            sw.Start();

            TryAgentImprinting(ref item);

            try
            {
                if (MicroEntity<T>.TableData.IsReadOnly)
                    throw new HttpResponseException(HttpStatusCode.MethodNotAllowed);

                var preRet = MicroEntity<T>.Get(item.Save());

                if (MicroEntity<T>.TableData.AuditChange)
                    AuditRequest("CHANGE", typeof(T).FullName + ":" + item.GetEntityIdentifier(), item.ToJson());


                Nyan.Core.Settings.Current.Log.Add("  POST " + typeof(T).FullName + " OK (" + sw.ElapsedMilliseconds + " ms)");

                return RenderJsonResult(preRet);
            }
            catch (Exception e)
            {
                sw.Stop();
                Nyan.Core.Settings.Current.Log.Add("  POST " + typeof(T).FullName + " ERR (" + sw.ElapsedMilliseconds + " ms): " + e.Message, e);
                throw;
            }
        }

        private static void TryAgentImprinting(ref T item)
        {
            //TODO: Capture current user iodentifier and write to storage.

            //var curPId = Environment.Current.PersonId;

            //Nyan.Core.Settings.Current.Log.Add("Imprinting Agent " + curPId);

            //if (item.IsNew())
            //{
            //    var propCreator = item.GetType().GetProperty("CreatorId");
            //    if (propCreator != null) propCreator.SetValue(item, curPId, null);
            //}

            //var propUpdater = item.GetType().GetProperty("LastUpdaterId");
            //if (propUpdater != null) propUpdater.SetValue(item, curPId, null);
        }

        private static void AuditRequest(string verb, string target, string content = null)
        {
            //TODO: Create Audit entry here.
        }

        [Route("")]
        [HttpPut]
        public virtual HttpResponseMessage WebApiPut(string id, T item)
        {
            TryAgentImprinting(ref item);

            if (MicroEntity<T>.TableData.IsReadOnly)
            {
                return Request.CreateErrorResponse(HttpStatusCode.MethodNotAllowed,
                    "This entity is market as read-only.");
            }

            if (ClassSecurity != null &&
                ClassSecurity.WritePermission != null &&
                !true)//TODO: This is the entry point for Authorization engines.
                throw new UnauthorizedAccessException("Insufficient permissions.");


            HttpResponseMessage ret;

            try
            {
                var isNew = (item.GetEntityIdentifier() == null);
                ret = Request.CreateResponse(isNew ? HttpStatusCode.Created : HttpStatusCode.OK);

                if (MicroEntity<T>.TableData.AuditChange)
                    AuditRequest("CHANGE", typeof(T).FullName + ":" + item.GetEntityIdentifier(), item.ToJson());
            }
            catch (Exception ex)
            {
                ret = Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex);
            }

            return ret;
        }

        [HttpDelete]
        [Route("{id}")]
        public virtual HttpResponseMessage WebApiDelete(string id)
        {
            if (MicroEntity<T>.TableData.IsReadOnly)
            {
                return Request.CreateErrorResponse(HttpStatusCode.MethodNotAllowed,
                    "This entity is market as read-only.");
            }

            if (ClassSecurity != null &&
                ClassSecurity.RemovePermission != null &&
                !true) //TODO: This is the entry point for Authorization engines.
                throw new UnauthorizedAccessException("Insufficient permissions.");

            HttpResponseMessage ret;

            var probe = MicroEntity<T>.Get(id);

            if (probe == null)
                ret = Request.CreateErrorResponse(HttpStatusCode.NotFound, "No Entity was found for ID " + id);
            else
            {
                MicroEntity<T>.Remove(id);

                if (MicroEntity<T>.TableData.AuditChange)
                    AuditRequest("REMOVAL", typeof(T).FullName + ":" + id, probe.ToJson());

                ret = Request.CreateResponse(HttpStatusCode.NoContent, "Entity removed successfully.");
            }

            return ret;
        }

        [Route("{id}/{entityReference:alpha}")]
        [HttpGet]
        public virtual HttpResponseMessage GetReference(string id, string entityReference)
        {
            HttpResponseMessage ret;

            Nyan.Core.Settings.Current.Log.Add("Reference request: {0} for {1}".format(entityReference, id));

            if (!EntityReferenceAttribute.ContainsKey(entityReference))
                ret = Request.CreateErrorResponse(HttpStatusCode.NotFound,
                    "Referenced entity not found [" + entityReference + "].");
            else
            {
                try
                {
                    object referenceCollection;

                    if (id.Equals("new"))
                        referenceCollection = new List<object>();
                    else
                    {
                        var probe = EntityReferenceAttribute[entityReference];
                        var targetType = probe.ForeignType.BaseType.GenericTypeArguments[0];

                        var staMode = targetType.GetMethod("ReferenceQueryByField") == null;

                        Nyan.Core.Settings.Current.Log.Add("RefType: " + targetType);

                        if (staMode) //If the method is Static... (e.g. an Entity)
                        {
                            Nyan.Core.Settings.Current.Log.Add("STATIC " + targetType);
                            dynamic refType = new StaticMembersDynamicWrapper(targetType);
                            referenceCollection = refType.ReferenceQueryByField(probe.ForeignProperty, id);
                        }
                        else // we can just instantiate.
                        {
                            Nyan.Core.Settings.Current.Log.Add("DYNAMIC " + targetType);
                            dynamic refType = Activator.CreateInstance(targetType);
                            referenceCollection = refType.ReferenceQueryByField(probe.ForeignProperty, id);
                        }
                    }
                    ret = Request.CreateResponse(HttpStatusCode.OK, referenceCollection);
                }
                catch (Exception ex)
                {
                    ret = Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex);
                }
            }
            return ret;
        }

        public virtual object ReferenceQueryByField(string field, string id)
        {
            var entbag = MicroEntity<T>.GetNewDynamicParameterBag();
            entbag.Add(field, id, DynamicParametersPrimitive.DbGenericType.String, ParameterDirection.Input);

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
            var ret = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(contents.ToJson()) };
            ret.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            return ret;
        }
    }
}