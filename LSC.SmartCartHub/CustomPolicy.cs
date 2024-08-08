using LSC.SmartCartHub.Entities;
using LSC.SmartCartHub.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace LSC.SmartCartHub
{
    public class CustomPolicy
    {
        public const string source = "CustomPolicy";
        string msg;

        public CustomPolicy(HttpClient httpClient, ILogger<CustomPolicy> logger)
        {
            HttpClient = httpClient;
            Logger = logger;
        }

        public HttpClient HttpClient { get; }
        public ILogger<CustomPolicy> Logger { get; }

        [FunctionName("CustomPolicy")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            
            //Check HTTP basic authorization
            if (!Authorize(req, log))
            {
                log.LogInformation("HTTP basic authentication validation failed.");
                return (ActionResult)new UnauthorizedResult();
            }

            // Get the request body
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            // If input data is null, show block page
            if (data == null)
            {
                log.LogInformation("There was a problem with your request.");
                return (ActionResult)new OkObjectResult(new ResponseContent("ShowBlockPage", "There was a problem with your request."));
            }

            // Print out the request body
            log.LogInformation("Request body: " + requestBody);

            // Get the current user language 
            string language = (data.ui_locales == null || data.ui_locales.ToString() == "") ? "default" : data.ui_locales.ToString();
            log.LogInformation($"Current language: {language}");

            // If displayName claim doesn't exist, or it is too short, show validation error message. So, user can fix the input data.
            if (data.accessCode == null || data.accessCode.ToString()==string.Empty)
            {
                string msg = "Please provide a access code.";
                log.LogInformation(msg);
                return (ActionResult)new BadRequestObjectResult(new ResponseContent("ValidationError", msg));
            }
            if (data.accessCode == "88888")
                return new OkResult();
            else
                return new ConflictResult();

        }

        private bool Authorize(HttpRequest req, ILogger log)
        {
            // Get the environment's credentials 
            string username = System.Environment.GetEnvironmentVariable("BASIC_AUTH_USERNAME", EnvironmentVariableTarget.Process);
            string password = System.Environment.GetEnvironmentVariable("BASIC_AUTH_PASSWORD", EnvironmentVariableTarget.Process);
            log.LogInformation($"{username} - {password}");
            // Returns authorized if the username is empty or not exists.
            if (string.IsNullOrEmpty(username))
            {
                string msg = "HTTP basic authentication is not set.";
                log.LogInformation(msg);
                return true;
            }

            // Check if the HTTP Authorization header exist
            if (!req.Headers.ContainsKey("Authorization"))
            {
                string msg = "Missing HTTP basic authentication header.";
                log.LogInformation(msg);
                return false;
            }

            // Read the authorization header
            var auth = req.Headers["Authorization"].ToString();

            // Ensure the type of the authorization header id `Basic`
            if (!auth.StartsWith("Basic "))
            {
                string msg = "HTTP basic authentication header must start with 'Basic '.";
                log.LogInformation(msg); 
                return false;
            }

            // Get the the HTTP basinc authorization credentials
            var cred = System.Text.UTF8Encoding.UTF8.GetString(Convert.FromBase64String(auth.Substring(6))).Split(':');

            // Evaluate the credentials and return the result
            return (cred[0] == username && cred[1] == password);
        }
        
    }
}

