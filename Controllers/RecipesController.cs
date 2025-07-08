using Microsoft.AspNetCore.Mvc;
using RecipeApi.Models;
using RecipeApi.Dtos;
using RecipeApi.Services;
using Newtonsoft.Json;
using System.Text;

namespace RecipeApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RecipesController : ControllerBase
    {
        private readonly RecipeService _recipeService;
        private readonly IConfiguration _config;

        public RecipesController(RecipeService recipeService, IConfiguration config)
        {
            _recipeService = recipeService;
            _config = config;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Recipe>>> GetAllRecipes()
        {
            var recipes = await _recipeService.GetAllAsync();
            return Ok(recipes);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Recipe>> GetRecipe(string id)
        {
            var recipe = await _recipeService.GetByIdAsync(id);
            if (recipe == null)
                return NotFound();
            return recipe;
        }

        [HttpPost]
        public async Task<ActionResult<Recipe>> CreateRecipe(CreateRecipeDto dto)
        {
            var recipe = new Recipe
            {
                Name = dto.Name,
                Ingredients = dto.Ingredients,
                Instructions = dto.Instructions,
                CuisineType = dto.CuisineType,
                PreparationTime = dto.PreparationTime,
                Status = dto.Status
            };
            await _recipeService.CreateAsync(recipe);

            return CreatedAtAction(nameof(GetRecipe), new { id = recipe.Id }, recipe);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRecipe(string id, Recipe recipe)
        {
            if (id != recipe.Id)
                return BadRequest();

            var updated = await _recipeService.UpdateAsync(id, recipe);
            if (!updated)
                return NotFound();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRecipe(string id)
        {
            var deleted = await _recipeService.DeleteAsync(id);
            if (!deleted)
                return NotFound();
            return NoContent();
        }

        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<Recipe>>> SearchRecipes([FromQuery] string query)
        {
            var recipes = await _recipeService.SearchAsync(query);
            return Ok(recipes);
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(string id, [FromQuery] string status)
        {
            var updated = await _recipeService.UpdateStatusAsync(id, status);
            if (!updated)
                return NotFound();

            var recipe = await _recipeService.GetByIdAsync(id);
            return Ok(recipe);
        }

        [HttpPost("ask-ai")]
        public async Task<IActionResult> AskAi([FromBody] AskAiDto dto)
        {
            var recipes = await _recipeService.GetAllAsync();

            var prompt = $"Here are some recipes:\n" +
                string.Join("\n", recipes.Select(r =>
                    $"- {r.Name}: Ingredients: {string.Join(", ", r.Ingredients)}. " +
                    $"Cuisine: {r.CuisineType}. PrepTime: {r.PreparationTime}min. Instructions: {r.Instructions}"
                )) +
                $"\n\nUser question: {dto.Question}\n" +
                "Based only on the recipes above, answer the user's question. Include the best recipe(s) and explain your choice with nutritional info if possible.";

            var apiKey = _config["Groq:ApiKey"];
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

            var requestBody = new
            {
                model = "llama3-8b-8192",
                messages = new[] { new { role = "user", content = prompt } }
            };

            var response = await client.PostAsync(
                "https://api.groq.com/openai/v1/chat/completions",
                new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json")
            );
            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode, responseString);

            dynamic result = JsonConvert.DeserializeObject(responseString);
            string aiResponse = result.choices[0].message.content;

            return Ok(new { answer = aiResponse });
        }
    }
}
