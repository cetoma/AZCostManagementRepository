using AZCostManagementMC.Models;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using static Dapper.SqlMapper;

namespace AZCostManagementMC.Helpers
{
    public class DBHelper
    {
        public static async Task<int> InsertLog(string connectionString, InsertParameters parameters)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string USProcedure = "USP_InsertCostManagementLogMC";
                int identity = await connection.ExecuteScalarAsync<int>(USProcedure, parameters, commandType: CommandType.StoredProcedure);
                return identity;
            }
        }

        public static async Task UpdateLog(string connectionString, UpdateParameters parameters)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string USProcedure = "USP_UpdateCostManagementLogMC";
                await connection.ExecuteAsync(USProcedure, parameters, commandType: CommandType.StoredProcedure);
            }
        }

        public static async Task<CostManagementLog> GetLog(string connectionString, string Customer)
        {
            using (SqlConnection connection = new(connectionString))
            {
                var parameters = new { Customer };
                var results = await connection.QueryMultipleAsync("USP_GetCostManagementLogMC", parameters, commandType: CommandType.StoredProcedure);
                var costManagementLog = results.Read<CostManagementLog>().ToList().FirstOrDefault();
                return costManagementLog;
            }
        }

        public static async Task CompleteLog(string connectionString, int Identity)
        {
            using (SqlConnection connection = new(connectionString))
            {
                string USProcedure = "USP_FinishCostManagementLogMC";
                var parameters = new { ID = Identity };
                await connection.ExecuteAsync(USProcedure, parameters, commandType: CommandType.StoredProcedure);
            }
        }
    }
}
