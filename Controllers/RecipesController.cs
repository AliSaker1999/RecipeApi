using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecipeApi.Data;
using RecipeApi.Models;
using RecipeApi.Dtos;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;

namespace RecipeApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RecipesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;

        public RecipesController(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        // GET: api/recipes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Recipe>>> GetAllRecipes()
        {
            return await _context.Recipes.ToListAsync();
        }

        // GET: api/recipes/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Recipe>> GetRecipe(int id)
        {
            var recipe = await _context.Recipes.FindAsync(id);
            if (recipe == null)
                return NotFound();

            return recipe;
        }

        // POST: api/recipes
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

            _context.Recipes.Add(recipe);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetRecipe), new { id = recipe.Id }, recipe);
        }


        // PUT: api/recipes/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRecipe(int id, Recipe recipe)
        {
            if (id != recipe.Id)
                return BadRequest();

            _context.Entry(recipe).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Recipes.Any(e => e.Id == id))
                    return NotFound();
                throw;
            }

            return NoContent();
        }

        // DELETE: api/recipes/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRecipe(int id)
        {
            var recipe = await _context.Recipes.FindAsync(id);
            if (recipe == null)
                return NotFound();

            _context.Recipes.Remove(recipe);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // GET: api/recipes/search?query=pizza
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<Recipe>>> SearchRecipes([FromQuery] string query)
        {
            return await _context.Recipes
                .Where(r => r.Name.Contains(query) || r.CuisineType.Contains(query))
                .ToListAsync();
        }

        // PATCH: api/recipes/{id}/status?status=favorite
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromQuery] string status)
        {
            var recipe = await _context.Recipes.FindAsync(id);
            if (recipe == null)
                return NotFound();

            recipe.Status = status;
            await _context.SaveChangesAsync();

            return Ok(recipe);
        }
        [HttpPost("ask-ai")]
        public async Task<IActionResult> AskAi([FromBody] AskAiDto dto)
        {
            // 1. Get all recipes from the DB
            var recipes = await _context.Recipes.ToListAsync();

            // 2. Prepare prompt for the AI (keep it short if you have lots of recipes)
            var prompt = $"Here are some recipes:\n" +
                string.Join("\n", recipes.Select(r => 
                    $"- {r.Name}: Ingredients: {string.Join(", ", r.Ingredients)}. " +
                    $"Cuisine: {r.CuisineType}. PrepTime: {r.PreparationTime}min. Instructions: {r.Instructions}"
                )) +
                $"\n\nUser question: {dto.Question}\n" +
                "Based only on the recipes above, answer the user's question. Include the best recipe(s) and explain your choice with nutritional info if possible.";

            // 3. Call OpenAI API
            var apiKey = _config["Groq:ApiKey"];; // Or from config
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var requestBody = new
            {
                model = "llama3-8b-8192", // or llama3-70b-8192, etc
                messages = new[]
                {
                    new { role = "user", content = prompt }
                }
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
