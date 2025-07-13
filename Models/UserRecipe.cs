using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;


namespace RecipeApi.Models
{
    public class UserRecipe
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("userId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; }

    [BsonElement("recipeId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string RecipeId { get; set; }

    [BsonElement("status")]
    public string Status { get; set; } // "favorite", "to try", "tried before"
}

}