using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Nyan.Core.Extensions;
using Nyan.Core.Modules.Log;
using Nyan.Core.Settings;

namespace Nyan.Core.Modules.Diagnostics
{
    public static class Factory
    {
        public static HttpResponseMessage AsResponse(this List<KeyValuePair<string, DiagnosticsEvaluation>> source)
        {
            var response = new HttpResponseMessage {StatusCode = HttpStatusCode.OK};


            //Let's transform the source a bit.

            var contents = source.Aggregate(new Dictionary<string, List<DiagnosticsEvaluation>>(),
                             (result, item) =>
                             {
                                 if (!result.ContainsKey(item.Key)) result.Add(item.Key, new List<DiagnosticsEvaluation>());
                                 result[item.Key].Add(item.Value);
                                 return result;
                             });

            var sourceJson = contents.ToJson();

            if (!source.Any()) response.StatusCode = HttpStatusCode.NotFound;

            if (source.Any(i => i.Value.State == DiagnosticsEvaluation.EState.Unknown)) response.StatusCode = HttpStatusCode.ServiceUnavailable;
            if (source.Any(i => i.Value.State == DiagnosticsEvaluation.EState.Critical)) response.StatusCode = HttpStatusCode.InternalServerError;

            response.Content = new StringContent(sourceJson, Encoding.UTF8, "application/json");

            return response;
        }

        public static List<KeyValuePair<string, DiagnosticsEvaluation>> RunDiagnostics(string category = null)
        {
            var msw = new Stopwatch();

            Dictionary<DiagnosticsEvaluationSetupAttribute, IDiagnosticsEvaluation> evaluators = null;

            var results = new List<KeyValuePair<string, DiagnosticsEvaluation>>();

            if (category == null)
            {
                evaluators = Instances.Evaluators;
                Current.Log.Add($"Starting Diagnostics: {Instances.Evaluators.Count} registered evaluators", Message.EContentType.Maintenance);
            }
            else
            {
                evaluators = Instances.Evaluators.Where(i => i.Key.Category == category).Select(i => i).ToDictionary(j => j.Key, j => j.Value);
                Current.Log.Add($"Starting Diagnostics: {Instances.Evaluators.Count} registered [{category}] evaluators", Message.EContentType.Maintenance);
            }

            try
            {
                Parallel.ForEach(evaluators, evaluator =>
                {
                    try
                    {
                        Current.Log.Add($"Diagnostics: [{evaluator.Key.Category}] {evaluator.Key.Name}", Message.EContentType.Maintenance);

                        results.Add(new KeyValuePair<string, DiagnosticsEvaluation>(evaluator.Key.Name, evaluator.Value.RunDiagnostics()));
                    } catch (Exception e)
                    {
                        Current.Log.Add($"    EVALUATOR [{evaluators.GetType().Name}] FAILURE: {e.Message} @ {e.FancyString()}", Message.EContentType.Maintenance);
                        Current.Log.Add(e);
                    }
                });
            } catch (Exception e)
            {
                Current.Log.Add($"    FAILURE: {e.Message} @ {e.FancyString()}", Message.EContentType.Maintenance);
                Current.Log.Add(e);
            }

            msw.Stop();

            return results;
        }
    }
}