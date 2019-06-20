using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;
using System.Threading.Tasks;

namespace GWManagementFunctions
{
    public class DatabaseOperation
    {
        /// <summary>
        /// Invoke database update command
        /// </summary>
        /// <param name="message"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        [FunctionName("InvokeDatabaseOperation")]
        public static async Task<bool> InvokeDatabaseOperation([ActivityTrigger] string query, ILogger log)
        {
            // Get the connection string from app settings and use it to create a connection.
            var str = Environment.GetEnvironmentVariable("sqldb_connection");
            try
            {
                using (SqlConnection conn = new SqlConnection(str))
                {
                    conn.Open();

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        // Execute the command and log the # rows affected.
                        var rows = await cmd.ExecuteNonQueryAsync();
                        log.LogInformation($"{rows} rows were updated");
                    }
                }
            }
            catch (Exception e)
            {
                log.LogInformation(e.Message);
            }

            return true;
        }
    }
}
