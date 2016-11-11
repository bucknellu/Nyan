using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nyan.Core.Assembly;
using Nyan.Core.Extensions;
using Nyan.Core.Modules.Data.Contracts;
using Nyan.Core.Modules.Log;
using Nyan.Core.Settings;

namespace Nyan.Modules.Web.Tools.Search
{
    public static class Global
    {
        private static readonly List<Type> SearchableTypes = Management.GetClassesByInterface<ISearch>();

        public static Dictionary<string, object> Run(string term, string categories)
        {
            var step = "Initializing";
            try
            {
                if (categories == null) categories = "";

                var cats = categories
                    .ToLower()
                    .Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
                    .ToList();

                var tasks = new List<Task<KeyValuePair<string, SearchResultBlock>>>();
                var timeout = TimeSpan.FromSeconds(10);

                step = "Iterating through ISearch types";
                //Prepare a task for each search-able type
                foreach (var searchableType in SearchableTypes)
                {
                    step = "Creating instance of " + searchableType.FullName;
                    var instance = (ISearch)Activator.CreateInstance(searchableType);

                    step = "Adding task for " + searchableType.FullName;

                    var preTask =
                        new Task<KeyValuePair<string, SearchResultBlock>>(() => ProcessSearchableType(instance, term), TaskCreationOptions.LongRunning); // Required to allow for timeout
                    tasks.Add(preTask);
                }

                step = "Starting {0} task(s)".format(tasks.Count);
                foreach (var task in tasks)
                    task.Start();

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
                        if (retcon.ContainsKey(task.Result.Key)) // Concatenate
                        {
                            var col1 = task.Result.Value.Results;
                            var col2 = retcon[task.Result.Key];
                            retcon[task.Result.Key] = col1.Concat(col2).ToList();


                            Current.Log.Add("CONCAT [{0}]: {1} items ({2} ms)".format(task.Result.Key, retcon[task.Result.Key].Count, task.Result.Value.ProcessingTime));
                        }
                        else // Just add.
                        {
                            retcon.Add(task.Result.Key, task.Result.Value.Results);
                            Current.Log.Add("ADD    [{0}]: {1} items ({2} ms)".format(task.Result.Key, task.Result.Value.Results.Count, task.Result.Value.ProcessingTime));
                        }
                    }
                }

                if (cats.Count > 0)
                {
                    var retcon2 = new Dictionary<string, List<SearchResult>>();

                    foreach (var i in retcon.Where(i => cats.Contains(i.Key.ToLower())))
                    {
                        Current.Log.Add("Selecting " + i.Key);
                        retcon2.Add(i.Key, i.Value);
                    }

                    retcon = retcon2;
                }

                step = "Returning content";


                var finalCon = new Dictionary<string, object>();
                var regCount = 0;

                foreach (var i in retcon)
                {
                    regCount += i.Value.Count;
                    finalCon.Add(i.Key, i.Value);
                }

                finalCon.Add("_total", regCount);

                return finalCon;

            }
            catch (Exception e)
            {
                Current.Log.Add("Error while " + step + ":", Message.EContentType.Warning);
                Current.Log.Add(e);
                return new Dictionary<string, object> { { "_total", 0 } };
            }
        }

        private static KeyValuePair<string, SearchResultBlock> ProcessSearchableType(ISearch instance, string term)
        {
            var ret = new SearchResultBlock { Results = new List<SearchResult>() };

            var sw = new Stopwatch();

            sw.Start();


            try { ret.Results = instance.SimpleQuery(term); }
            catch (Exception e) { Current.Log.Add(e); }

            sw.Stop();
            ret.ProcessingTime = sw.ElapsedMilliseconds;

            return new KeyValuePair<string, SearchResultBlock>(instance.SearchResultMoniker, ret);

        }

        internal class SearchResultBlock
        {
            internal List<SearchResult> Results = new List<SearchResult>();
            internal long ProcessingTime;

        }
    }
}