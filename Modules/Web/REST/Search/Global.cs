using Nyan.Core.Assembly;
using Nyan.Core.Extensions;
using Nyan.Core.Modules.Data.Contracts;
using Nyan.Core.Settings;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nyan.Modules.Web.REST.Search
{
    public static class Global
    {
        private static List<Type> _searchableTypes = Management.GetClassesByInterface<ISearch>();


        public static Dictionary<string, List<SearchResult>> Run(string term)
        {
            var step = "Initializing";
            try
            {
                var tasks = new List<Task<KeyValuePair<string, List<SearchResult>>>>();
                var timeout = TimeSpan.FromSeconds(10);

                step = "Iterating through ISearch types";
                //Prepare a task for each search-able type
                foreach (var searchableType in _searchableTypes)
                {
                    step = "Creating instance of " + searchableType.FullName;
                    var instance = (ISearch)Activator.CreateInstance(searchableType);

                    step = "Adding task for " + searchableType.FullName;

                    var preTask =
                        new Task<KeyValuePair<string, List<SearchResult>>>(() => ProcessSearchableType(instance, term),
                            TaskCreationOptions.LongRunning);// Required to allow for timeout
                    tasks.Add(preTask);
                }

                step = "Starting {0} task(s)".format(tasks.Count);
                foreach (var task in tasks)
                {
                    task.Start();
                }

                //Wait for all queries to finish - up to the timeout value.
                step = "Waiting for tasks to end";

                Task.WaitAll(tasks.ToArray(), timeout);

                step = "Preparing return content";
                var retcon = new Dictionary<string, List<SearchResult>>();

                foreach (var task in tasks)
                {

                    if (!task.IsCompleted)
                    {
                        var cts = new CancellationTokenSource();
                        var cancellableTask = task.ContinueWith(ignored => { }, cts.Token);
                        cts.Cancel();
                        Task.WaitAny(new[] { cancellableTask }, TimeSpan.FromSeconds(0.1));
                        Current.Log.Add("Task Canceled.");
                    }
                    else
                    {
                        Current.Log.Add("Adding " + task.Result.Key + " Task result.");
                        retcon.Add(task.Result.Key, task.Result.Value);
                    }
                }

                step = "Returning content";
                return retcon;

            }
            catch (Exception e)
            {
                Current.Log.Add("Error while " + step + ":", Core.Modules.Log.Message.EContentType.Warning);
                Current.Log.Add(e);
                return new Dictionary<string, List<SearchResult>>();
            }
        }


        private static KeyValuePair<string, List<SearchResult>> ProcessSearchableType(ISearch instance, string term)
        {
            try
            {
                return new KeyValuePair<string, List<SearchResult>>(instance.SearchResultMoniker, instance.SimpleQuery(term));
            }
            catch (Exception e)
            {
                Current.Log.Add(e);
                return new KeyValuePair<string, List<SearchResult>>(instance.SearchResultMoniker, new List<SearchResult>());
            }
        }

    }
}
