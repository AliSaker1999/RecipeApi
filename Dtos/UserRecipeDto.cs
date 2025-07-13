public class UserRecipeDto
{
    public string UserId { get; set; } = null!;
    public string RecipeId { get; set; } = null!;
    public string Status { get; set; } = null!; // "favorite", "to try", "made before"
}
