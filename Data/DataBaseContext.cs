using SQLite;
using RegistroPonto.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RegistroPonto.Data
{
    public class DatabaseContext
    {
        private readonly SQLiteAsyncConnection _database;

        public DatabaseContext(string dbPath)
        {
            _database = new SQLiteAsyncConnection(dbPath);
            _database.CreateTableAsync<Models.RegistroPonto>().Wait();
        }

        public Task<List<Models.RegistroPonto>> ObterRegistrosAsync()
        {
            return _database.Table<Models.RegistroPonto>().ToListAsync();
        }

        public Task<int> SalvarRegistroAsync(Models.RegistroPonto registro)
        {
            return _database.InsertAsync(registro);
        }
    }
}