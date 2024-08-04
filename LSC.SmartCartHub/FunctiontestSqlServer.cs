using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Data.SqlClient;
using System.Text;

namespace LSC.SmartCartHub
{
    public static class FunctiontestSqlServer
    {
        [FunctionName("FunctiontestSqlServer")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("inside FunctiontestSqlServer");
         
            //log.LogInformation("C# HTTP trigger function processed a request.");

            string name = req.Query["name"];

            //string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            //dynamic data = JsonConvert.DeserializeObject(requestBody);
            //name = name ?? data?.name;
            try
            {
                using (SqlConnection conn = new SqlConnection())
                {
                    log.LogInformation("reading env variable DbContext");
                    conn.ConnectionString = Environment.GetEnvironmentVariable("DbContext");
                    log.LogInformation(conn.ConnectionString);
                    conn.Open();
                    StringBuilder sb = new StringBuilder();
                    using (SqlCommand comm = new SqlCommand())
                    {
                        comm.Connection = conn;
                        comm.CommandText = "select * from users";
                        var reader = comm.ExecuteReader();
                        while (reader.Read())
                        {
                            sb.AppendLine(reader.GetInt32(0) + "," + reader.GetString(1));
                        }
                        reader.Close();
                    }

                    string responseMessage = string.IsNullOrEmpty(name)
                        ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                        : $"Hello, {name}. This HTTP triggered function executed successfully.";

                    responseMessage = sb.ToString();
                    return new OkObjectResult(responseMessage);
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message + ex.InnerException?.Message);
                throw;
            }
         
        }
    }
}