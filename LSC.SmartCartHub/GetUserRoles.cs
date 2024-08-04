using LSC.SmartCartHub.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSC.SmartCartHub
{
    public partial class UserProfileFunction
    {
        const string source = "GetUserRoles";

        [FunctionName("GetUserRoles")]
        public async Task<IActionResult> GetUserRoles(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "GetUserRoles/{adObjId}")] HttpRequest req,
            string adObjId,
            ILogger log, ExecutionContext context)
        {
            log.LogInformation("C# HTTP trigger function processing GetUserRoles request.");

            var userRoles = new List<string>();

            StringBuilder sb = new();
            try
            {
                string connectionString = Environment.GetEnvironmentVariable("DbContext");

                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

                if (string.IsNullOrEmpty(adObjId))
                {
                    string errormsg = "Please provide AdObjId in the request.";
                    log.LogInformation(errormsg);
                    return new BadRequestObjectResult(errormsg);
                }
                else
                {
                    log.LogInformation($"received object id as {adObjId}");
                }

                var defaultRole = "Guest";

                using (SqlConnection sqlConnection = new(connectionString))
                {
                    sqlConnection.Open();

                    using (SqlCommand comm = new())
                    {
                        comm.Connection = sqlConnection;
                        comm.CommandText = $"select name from roles r , UserRoles ur where r.id=ur.roleid and ur.userid='{adObjId}'";
                        log.LogInformation($"executing sql query {comm.CommandText}");

                        var reader = comm.ExecuteReader();
                        while (reader.Read())
                        {
                            sb.Append(reader.GetString(0) + ",");
                        }
                        reader.Close();
                    }
                    sqlConnection.Close();
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message + ex.InnerException?.Message);
            }
            
            var rolesSeparatedByComma = "";
            if (sb.ToString().EndsWith(","))
                rolesSeparatedByComma = sb.ToString().Substring(0, sb.ToString().Length - 1);

            log.LogInformation(rolesSeparatedByComma);
            return new OkObjectResult(rolesSeparatedByComma);
        }
    }
}
