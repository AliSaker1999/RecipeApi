using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RecipeApi.Models;
using RecipeApi.Services;
using System.Security.Claims;
using RecipeApi.Dtos;
using System.Text;
using Newtonsoft.Json;
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserRecipeController : ControllerBase
{
    private readonly UserRecipeService _userRecipeService;
    private readonly RecipeService _recipeService;
    private readonly IConfiguration _config;

    public UserRecipeController(UserRecipeService userRecipeService, RecipeService recipeService, IConfiguration config)
    {
        _userRecipeService = userRecipeService;
        _recipeService = recipeService;
        _config = config;
    }

    private string GetUserIdFromToken()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value
            ?? throw new Exception("User ID not found in token");
    }

    private bool IsValidStatus(string status)
    {
        var valid = new[] { "favorite", "to try", "made before" };
        return valid.Any(v => v.Equals(status, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<string?> ResolveRecipeIdAsync( string recipeName)
    {
        

        if (!string.IsNullOrWhiteSpace(recipeName))
        {
            // Use case-insensitive, exact match
            var matches = await _recipeService.SearchAsync(recipeName);
            var recipe = matches.FirstOrDefault(r =>
                r.Name.Equals(recipeName, StringComparison.OrdinalIgnoreCase));
            return recipe?.Id;
        }

        return null;
    }

    // 1. Add
    [HttpPost]
    public async Task<IActionResult> AddUserRecipe([FromBody] AddOrUpdateUserRecipeDto dto)
    {
        var userId = GetUserIdFromToken();

        if (!IsValidStatus(dto.Status))
            return BadRequest(new { message = "Invalid status." });

        var recipeId = await ResolveRecipeIdAsync( dto.RecipeName);
        if (string.IsNullOrEmpty(recipeId))
            return NotFound(new { message = "Recipe not found." });

        var userRecipe = new UserRecipe
        {
            UserId = userId,
            RecipeId = recipeId,
            Status = dto.Status
        };
        await _userRecipeService.AddOrUpdateAsync(userRecipe);
        return Ok(new { message = "Added!" });
    }

    // 2. Update status
    [HttpPut]
    public async Task<IActionResult> UpdateUserRecipeStatus([FromBody] AddOrUpdateUserRecipeDto dto)
    {
        var userId = GetUserIdFromToken();

        if (!IsValidStatus(dto.Status))
            return BadRequest(new { message = "Invalid status." });

        var recipeId = await ResolveRecipeIdAsync(dto.RecipeName);
        if (string.IsNullOrEmpty(recipeId))
            return NotFound(new { message = "Recipe not found." });

        var userRecipe = await _userRecipeService.GetAsync(userId, recipeId);
        if (userRecipe == null)
            return NotFound(new { message = "UserRecipe not found." });

        userRecipe.Status = dto.Status;
        await _userRecipeService.AddOrUpdateAsync(userRecipe);
        return Ok(new { message = "Updated!" });
    }

    // 3. Remove
    [HttpDelete]
    public async Task<IActionResult> RemoveUserRecipe([FromBody] RemoveUserRecipeDto dto)
    {
        var userId = GetUserIdFromToken();

        var recipeId = await ResolveRecipeIdAsync( dto.RecipeName);
        if (string.IsNullOrEmpty(recipeId))
            return NotFound(new { message = "Recipe not found." });

        var removed = await _userRecipeService.RemoveAsync(userId, recipeId);
        if (!removed)
            return NotFound(new { message = "UserRecipe not found." });

        return Ok(new { message = "Removed!" });
    }

    // 4. Get all for user, with optional status filter
    [HttpGet("my")]
    public async Task<IActionResult> GetUserRecipes([FromQuery] string? status = null)
    {
        var userId = GetUserIdFromToken();

        List<UserRecipe> userRecipes;
        if (string.IsNullOrEmpty(status))
        {
            userRecipes = await _userRecipeService.GetAllForUserAsync(userId);
        }
        else
        {
            if (!IsValidStatus(status))
                return BadRequest(new { message = "Invalid status." });

            userRecipes = await _userRecipeService.GetAllForUserByStatusAsync(userId, status);
        }

        // 1. Get list of recipe IDs for this user
        var recipeIds = userRecipes.Select(ur => ur.RecipeId).ToList();

        // 2. Get full recipe info from DB
        var recipes = await _recipeService.GetByIdsAsync(recipeIds);

        // 3. Project to your DTO
        var result = recipes.Select(r => new CreateRecipeDto
        {
            Name = r.Name,
            Ingredients = r.Ingredients,
            Instructions = r.Instructions,
            CuisineType = r.CuisineType,
            PreparationTime = r.PreparationTime,
            Status = userRecipes.First(ur => ur.RecipeId == r.Id).Status // Take the user's status for this recipe
        }).ToList();

        return Ok(result);
    }

    [HttpPost("ask-user-ai")]
public async Task<IActionResult> AskUserAi([FromBody] AskUserAiDto dto)
{
    var userId = GetUserIdFromToken();

    // Get user-recipes (filtered if status provided)
    List<UserRecipe> userRecipes;
    if (string.IsNullOrEmpty(dto.Status))
        userRecipes = await _userRecipeService.GetAllForUserAsync(userId);
    else
        userRecipes = await _userRecipeService.GetAllForUserByStatusAsync(userId, dto.Status);

    // Get full Recipe details for these
    var allRecipeIds = userRecipes.Select(ur => ur.RecipeId).ToList();
    var recipes = (await _recipeService.GetAllAsync()).Where(r => allRecipeIds.Contains(r.Id)).ToList();

    if (!recipes.Any())
        return Ok(new { answer = "No recipes found for your request." });

    // Prompt as before
    var prompt = $"Here are your recipes:\n" +
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
