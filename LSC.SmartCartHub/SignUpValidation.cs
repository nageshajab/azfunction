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
    public class SignUpValidation
    {
        public const string source = "SignUpValidation";
        string msg;

        public SignUpValidation(HttpClient httpClient, ILogger<SignUpValidation> logger)
        {
            HttpClient = httpClient;
            Logger = logger;
        }

        public HttpClient HttpClient { get; }
        public ILogger<SignUpValidation> Logger { get; }

        [FunctionName("SignUpValidation")]
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
                return new OkObjectResult(new ResponseContent("ShowBlockPage", "There was a problem with your request."));
            }

            // Print out the request body
            log.LogInformation("Request body: " + requestBody);

            // Get the current user language 
            string language = (data.ui_locales == null || data.ui_locales.ToString() == "") ? "default" : data.ui_locales.ToString();
            log.LogInformation($"Current language: {language}");
                        
            var profile = new Profile()
            {
                AdObjId = data.objectId,
                DisplayName = data.displayName.ToString(),
                Email = data.email.ToString(),
                FirstName = data.givenName,
                LastName = data.surname
                //FirstName = string.IsNullOrEmpty(data.firstName) ? (data.givenName ?? "") : data.firstName,
                //LastName = string.IsNullOrEmpty(data.lastName) ? (data.surname ?? "") : data.lastName
            };

            log.LogInformation(JsonConvert.SerializeObject(profile));

            // Call the UpdateUserProfile function
            var userProfileResponse = await CallUpdateUserProfileFunction(profile,log);
            log.LogDebug($"user object id returned from function updateUserProfile is {userProfileResponse.AdObjId}");

            var userRoles = await GetUserRolesFunction(userProfileResponse.AdObjId,log);
            var roleArray= userRoles.Split(new string[] { "roles=", "clientcode=", "TaxId=" }, StringSplitOptions.None);

            var responseToReturn = new ResponseContent()
            {
                //jobTitle = "This value return by the API Connector",// this jobTitle is in built attribute, you ca change that value as well
                // You can also return custom claims using extension properties.
                extension_userRoles = roleArray[1].Substring(0, roleArray[1].Length - 1),
                extension_ClientCode = roleArray[2].Substring(0, roleArray[2].Length - 1),
                extension_TaxId =roleArray[3],
                extension_userId = userProfileResponse.UserId.ToString(),
            };

            log.LogInformation(JsonConvert.SerializeObject(responseToReturn));

            // Input validation passed successfully, return `Allow` response.
            // TO DO: Configure the claims you want to return
            return (ActionResult)new OkObjectResult(responseToReturn);
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

        private async Task<Profile> CallUpdateUserProfileFunction(Profile profile,ILogger log)
        {
            try
            {
                // Adjust the URL based on your Azure Functions app and function name
                var updateProfileURL = Environment.GetEnvironmentVariable("UpdateProfileURL");
                log.LogInformation("update profile url " + updateProfileURL);

                // Serialize the profile to JSON
                var jsonProfile = JsonConvert.SerializeObject(profile);
                log.LogInformation("json profile "+jsonProfile);

                // Create the HTTP content
                var content = new StringContent(jsonProfile, Encoding.UTF8, "application/json");

                // Call the UpdateUserProfile function
                var response = await HttpClient.PostAsync(updateProfileURL, content);

                if (response.IsSuccessStatusCode)
                {
                    // Parse and return the response
                    var responseBody = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<Profile>(responseBody);
                }

                // Handle error cases
                var msg = $"UpdateUserProfile function failed with status code {response.StatusCode}";
                log.LogInformation(msg );
                return new Profile();


            }
            catch (Exception ex)
            {
                var msg = $"UpdateUserProfile function failed with exception {ex.ToString()}";
                log.LogInformation(msg );
                return new Profile();
            }
        }

        private async Task<string> GetUserRolesFunction(string adObjId, ILogger log)
        {
            string responseBody = "";
            try
            {
              
                // Adjust the URL based on your Azure Functions app and function name
                var getUserRolesURL = $"{Environment.GetEnvironmentVariable("GetUserRolesURL")}".Replace("{adObjId}",adObjId);
                log.LogInformation("getUserRolesURL: " + getUserRolesURL);

                    // Call the UpdateUserProfile function
                var response = await HttpClient.GetAsync(getUserRolesURL);

                if (response.IsSuccessStatusCode)
                {   
                    // Parse and return the response
                    responseBody = await response.Content.ReadAsStringAsync();
                }
                else
                { // Handle error cases
                     msg = $"GetUserRoles function failed with status code {response.StatusCode}";
                    log.LogInformation(msg);
                }
           
            }
            catch (Exception ex)
            {
                 msg = "Exception occured in GetUserRolesFunction: " + ex.ToString();
                log.LogInformation(msg);
            };
            msg = "Processed userRoles: " + responseBody;
            log.LogInformation(msg);
            return responseBody;
        }

    }
}


/*
 Request body: {"step":"PreTokenIssuance","client_id":"302bc019-f861-4ba4-971d-bf1b2c2b2cae",
"ui_locales":"en-US","email":"kameshdotnet411@gmail.com","objectId":"85e4c333-b448-422c-a7b8-dbe95af27b23",
"surname":"dotnet","displayName":"kamesh dotnet","givenName":"kamesh","identities":[{"signInType":"federated",
"issuer":"google.com","issuerAssignedId":"110957066204838485355"}]}
 */