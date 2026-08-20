using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace dynatracefunc
{

    public class DynatraceResponse
    {
        public string TimeGeneratedLocal { get; set; }
        public string TimeGeneratedUTC { get; set; }
        public string appVersion_s { get; set; }
        public string applicationType_s { get; set; }
        public string bounce_b { get; set; }
        public string browserFamily_s { get; set; }
        public string browserMajorVersion_s { get; set; }
        public string browserMonitorId_s { get; set; }
        public string browserMonitorName_s { get; set; }
        public string browserType_s { get; set; }
        public string city_s { get; set; }
        public string connectionType_s { get; set; }
        public string continent_s { get; set; }
        public string country_s { get; set; }
        public string dataProperties_s { get; set; }
        public string displayResolution_s { get; set; }
        public string doubleProperties_s { get; set; }
        public string TenantId { get; set; }
        public string SourceSystem { get; set; }

    }
    public static class Function1
    {
        // Reuse a single instance of HttpClient for performance.
        private static readonly HttpClient httpClient = new HttpClient();

        // TelemetryClient instance initialized with APPLICATIONINSIGHTS_CONNECTION_STRING.
        private static readonly TelemetryClient telemetryClient;

        // Static constructor to initialize the TelemetryClient with the connection string.
        static Function1()
        {
            TelemetryConfiguration config = TelemetryConfiguration.CreateDefault();
            string connectionString = Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING");

            if (!string.IsNullOrEmpty(connectionString))
            {
                config.ConnectionString = connectionString;
            }
            else
            {
                // Optionally, handle the case when the connection string is not set.
                throw new InvalidOperationException("APPLICATIONINSIGHTS_CONNECTION_STRING environment variable is not set.");
            }

            telemetryClient = new TelemetryClient(config);
        }

        [FunctionName("Function1")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            // Retrieve the "name" parameter if provided
            //string name = req.Query["name"];
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            //name = name ?? data?.name;

            // --- Dynatrace Data Retrieval Section ---

            // Replace with your actual Dynatrace API endpoint.
            string dynatraceApiUrl = Environment.GetEnvironmentVariable("DYNATRACE_API_URL");
            string dynatraceApiToken = Environment.GetEnvironmentVariable("DYNATRACE_API_TOKEN");
            HttpResponseMessage response;

            try
            {

                httpClient.DefaultRequestHeaders.Clear();

                httpClient.DefaultRequestHeaders.Add("Authorization", $"Api-Token {dynatraceApiToken}");

                response = await httpClient.GetAsync(dynatraceApiUrl);

            }
            catch (Exception ex)
            {
                log.LogError($"Error calling Dynatrace API: {ex.Message}");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }

            if (!response.IsSuccessStatusCode)
            {
                log.LogError($"Dynatrace API returned error: {response.StatusCode}");
                return new StatusCodeResult((int)response.StatusCode);
            }


            // Read the response from Dynatrace
            string dynatraceData = await response.Content.ReadAsStringAsync();
            log.LogInformation("Data retrieved from Dynatrace successfully.");

            DynatraceResponse dynatraceResponse = null;
            try
            {
                dynatraceResponse = JsonConvert.DeserializeObject<DynatraceResponse>(dynatraceData);
            }
            catch (Exception ex)
            {
                log.LogError($"Error deserializing Dynatrace response: {ex.Message}");
            }
            // --- Telemetry Logging Section ---
            // Create a dictionary with the custom telemetry properties.
            var properties = new Dictionary<string, string>
            {
                { "TimeGeneratedLocal", dynatraceResponse?.TimeGeneratedLocal },
                { "TimeGeneratedUTC", dynatraceResponse?.TimeGeneratedUTC  },
                { "appVersion_s", dynatraceResponse?.appVersion_s },
                { "applicationType_s", dynatraceResponse?.applicationType_s },
                { "bounce_b", dynatraceResponse?.bounce_b},
                { "browserFamily_s", dynatraceResponse?.browserFamily_s },
                { "browserMajorVersion_s", dynatraceResponse?.browserMajorVersion_s},
                { "browserMonitorId_s", dynatraceResponse?.browserMonitorId_s },
                { "browserMonitorName_s", dynatraceResponse?.browserMonitorName_s },
                { "browserType_s", dynatraceResponse?.browserType_s },
                { "city_s", dynatraceResponse?.city_s },
                { "connectionType_s", dynatraceResponse?.connectionType_s },
                { "continent_s", dynatraceResponse?.continent_s },
                { "country_s", dynatraceResponse?.country_s },
                { "dataProperties_s", dynatraceResponse?.dataProperties_s },
                { "displayResolution_s", dynatraceResponse?.displayResolution_s },
                { "doubleProperties_s", dynatraceResponse?.doubleProperties_s },
                { "TenantId", dynatraceResponse?.TenantId },
                { "SourceSystem", dynatraceResponse?.SourceSystem },

            };

            // Log a custom event to Application Insights.
            telemetryClient.TrackEvent("DynatraceDataReceived", properties);
            telemetryClient.Flush();

            // --- Costruzione della Risposta HTTP ---
            string responseMessage = "This HTTP triggered function executed successfully. Data retrieved from Dynatrace and telemetry sent to Application Insights.";
            return new OkObjectResult(responseMessage);
        }
    }
}
