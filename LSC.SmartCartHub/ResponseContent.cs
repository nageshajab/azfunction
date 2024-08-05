﻿using Newtonsoft.Json;

namespace LSC.SmartCartHub
{
    public class ResponseContent
    {
        public const string ApiVersion = "1.0.0";

        public ResponseContent()
        {
            this.version = ResponseContent.ApiVersion;
            this.action = "Continue";
        }

        public ResponseContent(string action, string userMessage)
        {
            this.version = ResponseContent.ApiVersion;
            this.action = action;
            this.userMessage = userMessage;
            if (action == "ValidationError")
            {
                this.status = "400";
            }
        }

        public string version { get; }
        public string action { get; set; }


        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string userMessage { get; set; }


        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string status { get; set; }


        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string jobTitle { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string extension_userRoles { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string extension_userId { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string extension_ClientCode { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string extension_TaxId { get; set; }
    }
}
