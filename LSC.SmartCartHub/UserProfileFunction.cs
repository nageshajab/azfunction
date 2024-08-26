using LSC.SmartCartHub.Entities;
using LSC.SmartCartHub.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace LSC.SmartCartHub
{
    public partial class UserProfileFunction
    {
        static string msg;

        [FunctionName("UpdateUserProfile")]
        public static async Task<IActionResult> UpdateUserProfile(
           [HttpTrigger(AuthorizationLevel.Function, "post", Route = "UpdateUserProfile")] HttpRequest req,
           ILogger log, ExecutionContext context)
        {
            msg = "C# HTTP trigger function processed a request.";
            log.LogInformation(msg);

            var userProfileResponse = new Profile();

            try
            {
                // Get the connection string from the configuration

                string connectionString = Environment.GetEnvironmentVariable("DbContext");

                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

                if (string.IsNullOrEmpty(requestBody))
                {
                    msg = "Invalid request body. Please provide a valid Profile. Body cannot be empty";
                    log.LogInformation(msg);
                    return new BadRequestObjectResult(msg);
                }

                Profile? profile = JsonSerializer.Deserialize<Profile>(requestBody);

                if (profile == null)
                {
                    msg = "Invalid request body. Please provide a valid Profile.";
                    log.LogInformation(msg);
                    return new BadRequestObjectResult(msg);
                }

                string adObjId = profile.AdObjId;

                if (string.IsNullOrEmpty(adObjId))
                {
                    msg = "Please provide AdObjId in the request body.";
                    log.LogInformation(msg);
                    return new BadRequestObjectResult(msg);
                }

                var adminUsers = new List<string>() { "learnsmartcoding@gmail.com" };
                var supportUsers = new List<string>() { "karthiktechblog@gmail.com" };


                // Check if UserProfile with given AdObjId exists
                using SqlConnection connection = new SqlConnection(connectionString);
                connection.Open();
                using SqlCommand command = new();
                command.Connection = connection;
                command.CommandText = $"select * from Users where ObjectId='{adObjId}'";
                log.LogInformation(command.CommandText);
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        string displyaname = reader.IsDBNull(1) ? "" : reader.GetString(1);
                        profile.DisplayName = displyaname + "found user";
                    }
                    else
                    {
                        SqlConnection conn2 = new SqlConnection(connectionString) ;
                        conn2.Open();
                        using SqlCommand command1 = new();
                        command1.Connection = conn2;
                        command1.CommandText = $"select count(*) from Users where ObjectId='{profile.AdObjId}' and email='{profile.Email}'";
                        log.LogInformation(command1.CommandText);
                        int rows = int.Parse(command1.ExecuteScalar().ToString());
                        conn2.Close();

                        if (rows == 0)
                        {
                            SqlConnection conn3=new SqlConnection(connectionString) ;
                            conn3.Open();
                            log.LogInformation("user does not exists , creating one");
                            using SqlCommand command2 = new();
                            command2.Connection = conn3;
                            command2.CommandText = $"insert into Users(ObjectId,email,Surname,DisplayName,GivenName) values('{profile.AdObjId}','{profile.Email}','{profile.LastName}','{profile.DisplayName}','{profile.FirstName}')";
                            log.LogInformation(command2.CommandText);
                            command2.ExecuteNonQuery();
                            conn3.Close();

                            SqlConnection conn4 = new SqlConnection(connectionString);
                            conn4.Open();
                            using SqlCommand command3 = new();
                            command3.Connection = conn4;
                            command3.CommandText = $"insert into UserRoles (userid, roleid) values('{profile.AdObjId}',3)";
                            log.LogInformation(command3.CommandText);
                            command3.ExecuteNonQuery();
                            conn4.Close();
                        }
                        else
                        {
                            log.LogInformation("user already exist");
                        }
                    }
                }
                userProfileResponse.AdObjId = adObjId;
                userProfileResponse.Email = profile.Email;
                userProfileResponse.FirstName = profile.FirstName;
                userProfileResponse.LastName = profile.LastName;
                userProfileResponse.DisplayName = profile.DisplayName;

                DeleteDuplicateUsers( profile.Email, profile.AdObjId, log);
            }
            catch (Exception ex)
            {
                log.LogInformation(ex.ToString());
            }

            return new OkObjectResult(userProfileResponse);
        }

        private static void DeleteDuplicateUsers(string email, string objectid, ILogger log)
        {
            string connectionString = Environment.GetEnvironmentVariable("DbContext");
            SqlConnection conn=new SqlConnection(connectionString);
            conn.Open();

            //delete users matching same email id but with different object id, that means user was earlier there but was deleted and recreated again with same email
            using SqlCommand command4 = new();
            command4.Connection = conn;
            command4.CommandText = $"delete users where email='{email}' and objectid !='{objectid}'";
            log.LogInformation(command4.CommandText);
            command4.ExecuteNonQuery();

            conn.Close();
        }
    }
}
