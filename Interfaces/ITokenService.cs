using RecipeApi.Models;

namespace RecipeApi.Interfaces
{
    public interface ITokenService
    {
        string CreateToken(AppUser user, string role);
    }
}
