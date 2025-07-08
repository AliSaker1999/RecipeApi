using Microsoft.Extensions.Options;
using MongoDB.Driver;
using RecipeApi.Data;
using RecipeApi.Models;

namespace RecipeApi.Services
{
    public class UserService
    {
        private readonly IMongoCollection<AppUser> _users;

        public UserService(IOptions<MongoDbSettings> settings, IMongoClient mongoClient)
        {
            var db = mongoClient.GetDatabase(settings.Value.DatabaseName);
            _users = db.GetCollection<AppUser>("users");
        }

        public async Task<AppUser?> GetByUsernameAsync(string username) =>
            await _users.Find(u => u.UserName == username).FirstOrDefaultAsync();

        public async Task CreateAsync(AppUser user) =>
            await _users.InsertOneAsync(user);

        public async Task<bool> AnyUsersAsync() =>
            await _users.Find(_ => true).AnyAsync();
        public async Task DeleteAsync(string id) =>
            await _users.DeleteOneAsync(u => u.Id == id);

        public async Task<List<string>> GetAllUsernamesAsync() =>
            await _users.Find(_ => true).Project(u => u.UserName).ToListAsync();

    }
}
