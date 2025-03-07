using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using CerbiClientLogging.Interfaces;

namespace CerbiClientLogging.Classes.Databases
{
    public class AzureSqlDatabase : IDatabase
    {
        private readonly string _connectionString;

        public AzureSqlDatabase(string connectionString)
        {
            _connectionString = connectionString;
        }


        public async Task<bool> SendMessageAsync(string payload, Guid messageId)
        {
            try
            {
                await using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    await using (var command = new SqlCommand(payload, connection))
                    {
                        await command.ExecuteNonQueryAsync();
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($@"Error executing MessageId:{messageId} query: {ex.Message}");
                return false;
            }
        }
    }
}