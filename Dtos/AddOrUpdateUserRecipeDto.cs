using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RecipeApi.Dtos
{
    public class AddOrUpdateUserRecipeDto

    {
    
    public string? RecipeName { get; set; }
    public string Status { get; set; } = null!;

    }

}