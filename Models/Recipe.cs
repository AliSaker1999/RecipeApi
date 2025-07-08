using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RecipeApi.Models
{
    public class Recipe
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = null!;

        public string Name { get; set; } = null!;
        public List<string> Ingredients { get; set; } = new();
        public string Instructions { get; set; } = null!;
        public string CuisineType { get; set; } = null!;
        public int PreparationTime { get; set; }
        public string Status { get; set; } = null!; // favorite, to try, made before
    }
}
