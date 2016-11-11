using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Nyan.Core.Modules.Data;
using Nyan.Core.Settings;

namespace Nyan.Modules.Web.REST
{
    [RoutePrefix("api2/entity")]
    public class ComplexEntityWebApiController<T, TU> : ApiController
        where T : ComplexMicroEntity<T, TU>, new()
        where TU : MicroEntity<TU>
    {
        // ReSharper disable once InconsistentNaming

        [Route("")]
        [HttpGet]
        public virtual object WebApiGetAll()
        {
            var sw = new Stopwatch();
            sw.Start();

            try
            {
                var ret = ComplexMicroEntity<T, TU>.Get().ToList();
                sw.Stop();
                Current.Log.Add("  GET " + typeof(T).FullName + " OK (" + sw.ElapsedMilliseconds + " ms)");
                return ret;
            }
            catch (Exception e)
            {
                sw.Stop();
                Current.Log.Add("  GET " + typeof(T).FullName + " ERR (" + sw.ElapsedMilliseconds + " ms): " + e.Message, e);
                throw;
            }
        }

        [Route("new")]
        [HttpGet]
        public virtual T WebApiGetNew()
        {
            var sw = new Stopwatch();
            sw.Start();

            try
            {
                var ret = (T)Activator.CreateInstance(typeof(T), new object[] { });
                sw.Stop();
                Current.Log.Add("  NEW " + typeof(T).FullName + " OK (" + sw.ElapsedMilliseconds + " ms)");
                return ret;
            }
            catch (Exception e)
            {
                sw.Stop();
                Current.Log.Add("  NEW " + typeof(T).FullName + " ERR (" + sw.ElapsedMilliseconds + " ms): " + e.Message, e);
                throw;
            }
        }

        [Route("{id}")]
        [HttpGet]
        public virtual object WebApiGet(string id)
        {
            var sw = new Stopwatch();
            sw.Start();

            try
            {
                var ret = ComplexMicroEntity<T, TU>.Get(MicroEntity<TU>.Get(id));
                if (ret == null) throw new HttpResponseException(HttpStatusCode.NotFound);

                Current.Log.Add("  GET " + typeof(T).FullName + ":" + id + " OK (" + sw.ElapsedMilliseconds + " ms)");
                return ret;
            }
            catch (Exception e)
            {
                sw.Stop();
                Current.Log.Add("  GET:" + id + " " + typeof(T).FullName + ":" + id + " ERR (" + sw.ElapsedMilliseconds + " ms): " + e.Message, e);
                throw;
            }
        }

        [Route("")]
        [HttpPost]
        public virtual HttpResponseMessage WebApiPost(T item)
        {
            var ret = new SimpleHttpResponseMessage(HttpStatusCode.NoContent, "Method not implemented");
            return ret;
        }
    }
}