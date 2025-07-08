// using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
// using Microsoft.EntityFrameworkCore;
// using RecipeApi.Models;

// namespace RecipeApi.Data
// {
//     public class AppDbContext : IdentityDbContext<AppUser>
//     {
//         public AppDbContext(DbContextOptions options) : base(options) { }

//         public DbSet<Recipe> Recipes { get; set; }

//         protected override void OnModelCreating(ModelBuilder builder)
//         {
//             base.OnModelCreating(builder);

//             builder.Entity<Recipe>()
//                 .Property(r => r.Ingredients)
//                 .HasConversion(
//                     v => string.Join(",", v),
//                     v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
//                 );
//         }

        
//     }
// }
