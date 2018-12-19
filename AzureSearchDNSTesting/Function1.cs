using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AzureSearchDNSTesting
{
    public class DelegatingHandlerImpl : DelegatingHandler
    {
        public DelegatingHandlerImpl(HttpMessageHandler hanlder) : base(hanlder)
        {

        }

        protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response =  await base.SendAsync(request, cancellationToken);
            return response;
        }
    }


    class MyEventListener : EventListener
    {
        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            var memberNameIndex = eventData.PayloadNames.IndexOf("memberName");

            var message = new StringBuilder();
            for (var i = 0; i < eventData.Payload.Count; i++)
            {
                if (i == memberNameIndex) continue;
                if (i > 0)
                {
                    message.Append(", ");
                }

                message.Append(eventData.PayloadNames[i] + "=" + eventData.Payload[i]);
            }

            var last = eventData.Payload.Last().ToString();

            if (string.IsNullOrWhiteSpace(last)) return;
            var content = message.ToString();
            if (content.Contains("devopshackfestfuji.simplearchitect.club"))
                Debug.WriteLine(message);
        }
    }

    public class KeepAliveAvailableHttpClientHandler : HttpClientHandler
    {
        private readonly FieldInfo _fieldInfo =
            typeof(HttpClientHandler).GetField("_socketsHttpHandler", BindingFlags.NonPublic | BindingFlags.Instance);

        public KeepAliveAvailableHttpClientHandler()
        {
            var socketHttpHandler = (SocketsHttpHandler) _fieldInfo.GetValue(this);
            socketHttpHandler.PooledConnectionIdleTimeout = TimeSpan.FromMinutes(3);
            socketHttpHandler.PooledConnectionLifetime = TimeSpan.FromMinutes(1);
        }
    }

    public static class Function1
    {
        private static SearchIndexClient indexClient;
        private static bool IsClearKeepAlive;

        static Function1()
        {

            var netEventSource = EventSource.GetSources().FirstOrDefault(es => es.Name == "Microsoft-System-Net-Http");

            if (netEventSource != null)
            {
                var myEventListener = new MyEventListener();

                myEventListener.EnableEvents(netEventSource, EventLevel.LogAlways);
            }

            string searchServiceName = Environment.GetEnvironmentVariable("SearchServiceName");
            string adminApiKey = Environment.GetEnvironmentVariable("SearchServiceAdminApiKey");
            var handler = new KeepAliveAvailableHttpClientHandler();


            handler.ServerCertificateCustomValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
            {
                return true;
            };
            var delegatingHanlder = new DelegatingHandlerImpl(handler);
            var hanlders = new DelegatingHandler[]
            {
                delegatingHanlder
            };


            indexClient = new SearchIndexClient(searchServiceName, "azuresql-index", new SearchCredentials(adminApiKey), handler , hanlders);
        }

        [FunctionName("Search")]
        public static async Task<IActionResult> Search(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]
            HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            List<string> searchFields = new List<string>();
            searchFields.Add("Name");

            return new OkObjectResult(await SearchAsync("*", searchFields));
         }

        public static async Task<IEnumerable<Product>> SearchAsync(string query, List<string> searchFields)
        {
            var parameters = new SearchParameters();
            parameters.SearchFields = searchFields;
            indexClient.SearchDnsSuffix = "simplearchitect.club/func";
            var results = await indexClient.Documents.SearchAsync<Product>(query, parameters);
            
            return results.Results.Select<SearchResult<Product>, Product>(p => p.Document);
        }

    }
}
