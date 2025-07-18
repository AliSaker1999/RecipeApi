using Microsoft.AspNetCore.Authentication.JwtBearer;
// using Microsoft.EntityFrameworkCore; // Not needed unless you use Identity with EF for users
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RecipeApi.Models;
using RecipeApi.Services;
using RecipeApi.Interfaces;
using System.Text;

// MongoDB
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using RecipeApi.Data; // Make sure your namespace matches

var builder = WebApplication.CreateBuilder(args);

// ----------------- MONGODB CONFIGURATION -----------------
builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDbSettings"));

builder.Services.AddSingleton<IMongoClient>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;
    return new MongoClient(settings.ConnectionString);
});

// Register your RecipeService (and any other Mongo-backed services)
builder.Services.AddSingleton<RecipeService>();

builder.Services.AddSingleton<UserService>();

builder.Services.AddSingleton<UserRecipeService>();


builder.Services.AddScoped<ITokenService, TokenService>(); // (If you keep your token service)

// ----------------- JWT CONFIG -----------------
var jwtKey = builder.Configuration["Jwt:Key"] ?? "super_secret_key_12345";
var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = key
    };
});

builder.Services.AddAuthorization();

// ----------------- CORS -----------------
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

// ----------------- SWAGGER -----------------
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "RecipeApi", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and then your token.\n\nExample: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6...",
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();



using (var scope = app.Services.CreateScope())
{
    var userService = scope.ServiceProvider.GetRequiredService<UserService>();
    var admin = await userService.GetByUsernameAsync("admin");
    if (admin == null)
    {
        var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<AppUser>();
        var newAdmin = new AppUser
        {
            UserName = "admin",
            Role = "Admin"
        };
        newAdmin.PasswordHash = hasher.HashPassword(newAdmin, "Admin123!");
        await userService.CreateAsync(newAdmin);
        Console.WriteLine("Seeded admin user (username: admin, password: Admin123!)");
    }
}




// ----------------- MIDDLEWARE -----------------
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();

app.Run();
