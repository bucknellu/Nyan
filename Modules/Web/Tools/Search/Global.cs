using System;
using System.Collections.Generic;
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
        private static readonly List<Type> _searchableTypes = Management.GetClassesByInterface<ISearch>();

        public static Dictionary<string, List<SearchResult>> Run(string term, string categories)
        {
            var step = "Initializing";
            try
            {
                if (categories == null) categories = "";

                var cats = categories
                    .ToLower()
                    .Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
                    .ToList();

                var tasks = new List<Task<KeyValuePair<string, List<SearchResult>>>>();
                var timeout = TimeSpan.FromSeconds(10);

                step = "Iterating through ISearch types";
                //Prepare a task for each search-able type
                foreach (var searchableType in _searchableTypes)
                {
                    step = "Creating instance of " + searchableType.FullName;
                    var instance = (ISearch) Activator.CreateInstance(searchableType);

                    step = "Adding task for " + searchableType.FullName;

                    var preTask =
                        new Task<KeyValuePair<string, List<SearchResult>>>(() => ProcessSearchableType(instance, term),
                            TaskCreationOptions.LongRunning); // Required to allow for timeout
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
                        Task.WaitAny(new[] {cancellableTask}, TimeSpan.FromSeconds(0.1));
                        Current.Log.Add("Task Canceled.");
                    }
                    else
                    {
                        Current.Log.Add("Adding " + task.Result.Key + " Task result.");

                        if (retcon.ContainsKey(task.Result.Key)) // Concatenate
                        {
                            Current.Log.Add("Concatenating " + task.Result.Key);
                            retcon[task.Result.Key] = task.Result.Value.Concat(retcon[task.Result.Key]).ToList();
                        }
                        else // Just add.
                        {
                            Current.Log.Add("Adding " + task.Result.Key);
                            retcon.Add(task.Result.Key, task.Result.Value);
                        }
                    }
                }

                Current.Log.Add("cats.Count " + cats.Count);

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
                return retcon;
            } catch (Exception e)
            {
                Current.Log.Add("Error while " + step + ":", Message.EContentType.Warning);
                Current.Log.Add(e);
                return new Dictionary<string, List<SearchResult>>();
            }
        }

        private static KeyValuePair<string, List<SearchResult>> ProcessSearchableType(ISearch instance, string term)
        {
            try
            {
                return new KeyValuePair<string, List<SearchResult>>(instance.SearchResultMoniker, instance.SimpleQuery(term));
            } catch (Exception e)
            {
                Current.Log.Add(e);
                return new KeyValuePair<string, List<SearchResult>>(instance.SearchResultMoniker, new List<SearchResult>());
            }
        }
    }
}