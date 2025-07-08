using Microsoft.Extensions.Options;
using MongoDB.Driver;
using RecipeApi.Data;
using RecipeApi.Models;

namespace RecipeApi.Services
{
    public class RecipeService
    {
        private readonly IMongoCollection<Recipe> _recipes;

        public RecipeService(IOptions<MongoDbSettings> settings, IMongoClient mongoClient)
        {
            var database = mongoClient.GetDatabase(settings.Value.DatabaseName);
            _recipes = database.GetCollection<Recipe>("recipes");
        }

        public async Task<List<Recipe>> GetAllAsync() =>
            await _recipes.Find(_ => true).ToListAsync();

        public async Task<Recipe?> GetByIdAsync(string id) =>
            await _recipes.Find(r => r.Id == id).FirstOrDefaultAsync();

        public async Task<List<Recipe>> SearchAsync(string query) =>
            await _recipes.Find(r =>
                r.Name.ToLower().Contains(query.ToLower()) ||
                r.CuisineType.ToLower().Contains(query.ToLower())
            ).ToListAsync();

        public async Task<Recipe> CreateAsync(Recipe recipe)
        {
            await _recipes.InsertOneAsync(recipe);
            return recipe;
        }

        public async Task<bool> UpdateAsync(string id, Recipe recipeIn)
        {
            var result = await _recipes.ReplaceOneAsync(r => r.Id == id, recipeIn);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> DeleteAsync(string id)
        {
            var result = await _recipes.DeleteOneAsync(r => r.Id == id);
            return result.DeletedCount > 0;
        }

        public async Task<bool> UpdateStatusAsync(string id, string status)
        {
            var update = Builders<Recipe>.Update.Set(r => r.Status, status);
            var result = await _recipes.UpdateOneAsync(r => r.Id == id, update);
            return result.ModifiedCount > 0;
        }
    }
}
