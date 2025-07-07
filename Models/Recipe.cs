namespace RecipeApi.Models
{
    public class Recipe
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public List<string> Ingredients { get; set; } = new();
        public string Instructions { get; set; }
        public string CuisineType { get; set; }
        public int PreparationTime { get; set; }
        public string Status { get; set; } // favorite, to try, made before
    }
}
