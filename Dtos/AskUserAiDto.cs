using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RecipeApi.Dtos
{
    public class AskUserAiDto
    {
        public string Question { get; set; } = null!;
    public string? Status { get; set; } // Optional: favorite, to try, made before
    }
}