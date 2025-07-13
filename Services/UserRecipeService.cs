using Microsoft.Extensions.Options;
using MongoDB.Driver;
using RecipeApi.Data;
using RecipeApi.Models;

namespace RecipeApi.Services
{
    public class UserRecipeService
    {
        private readonly IMongoCollection<UserRecipe> _userRecipes;

        public UserRecipeService(IOptions<MongoDbSettings> settings, IMongoClient mongoClient)
        {
            var database = mongoClient.GetDatabase(settings.Value.DatabaseName);
            _userRecipes = database.GetCollection<UserRecipe>("userrecipes");
        }

        public async Task<List<UserRecipe>> GetAllForUserAsync(string userId) =>
            await _userRecipes.Find(ur => ur.UserId == userId).ToListAsync();

        public async Task<List<UserRecipe>> GetAllForUserByStatusAsync(string userId, string status) =>
            await _userRecipes.Find(ur => ur.UserId == userId && ur.Status == status).ToListAsync();

        public async Task<UserRecipe?> GetAsync(string userId, string recipeId) =>
            await _userRecipes.Find(ur => ur.UserId == userId && ur.RecipeId == recipeId).FirstOrDefaultAsync();

        public async Task AddOrUpdateAsync(UserRecipe userRecipe)
        {
            var filter = Builders<UserRecipe>.Filter.Where(ur =>
                ur.UserId == userRecipe.UserId && ur.RecipeId == userRecipe.RecipeId
            );
            await _userRecipes.ReplaceOneAsync(filter, userRecipe, new ReplaceOptions { IsUpsert = true });
        }

        public async Task<bool> RemoveAsync(string userId, string recipeId)
        {
            var result = await _userRecipes.DeleteOneAsync(ur => ur.UserId == userId && ur.RecipeId == recipeId);
            return result.DeletedCount > 0;
        }
    }
}
