namespace RecipeApi.Dtos
{
    public class CreateRecipeDto
    {
        public string Name { get; set; }
        public List<string> Ingredients { get; set; } = new();
        public string Instructions { get; set; }
        public string CuisineType { get; set; }
        public int PreparationTime { get; set; }
        public string Status { get; set; } // favorite, to try, made before
    }
}
