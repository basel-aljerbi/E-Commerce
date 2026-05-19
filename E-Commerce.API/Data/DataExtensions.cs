using E_Commerce.API.Models;
using Microsoft.EntityFrameworkCore;

namespace E_Commerce.API.Data;

public static class DataExtensions
{
    public static async Task MigrateDbAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ECommerceContext>();
        if (dbContext.Database.IsInMemory())
        {
            await dbContext.Database.EnsureCreatedAsync();
            return;
        }
        await dbContext.Database.MigrateAsync();
    }

    public static void AddECommerceDb(this WebApplicationBuilder builder)
    {
        // Test environment: InMemory database (data is disposable)
        if (builder.Environment.IsEnvironment("Test"))
        {
            var dbName = builder.Configuration["TestDbName"] ?? "TestDb";
            builder.Services.AddDbContext<ECommerceContext>(options =>
                options.UseInMemoryDatabase(dbName)
                    .ConfigureWarnings(w => w.Ignore(
                        Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning)));
            return;
        }

        // SQL Server (connection string from configuration)
        var connString = builder.Configuration.GetConnectionString("ECommerce");
        builder.Services.AddDbContext<ECommerceContext>(options =>
        {
            options.UseSqlServer(connString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(3);
                sqlOptions.CommandTimeout(60);
            });
            options.ConfigureWarnings(w =>
                w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        });
    }

    public static async Task SeedDataAsync(this ECommerceContext context)
    {
        var adminEmail = "admin@admin.com";
        var existingAdmin = await context.Users.FirstOrDefaultAsync(u => u.Email == adminEmail);
        if (existingAdmin is not null)
        {
            existingAdmin.Role = "Admin";
            existingAdmin.IsEmailVerified = true;
        }
        else if (!await context.Users.AnyAsync(u => u.Role == "Admin"))
        {
            context.Users.Add(new User
            {
                Email = adminEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123", workFactor: 12),
                FullName = "Admin",
                Role = "Admin",
                IsEmailVerified = true,
                CreatedAt = DateTime.UtcNow
            });
        }
        await context.SaveChangesAsync();

        if (!await context.Categories.AnyAsync())
        {
            var categories = new List<Category>
            {
                new() { Name = "Electronics" },
                new() { Name = "Clothing" },
                new() { Name = "Books" },
                new() { Name = "Sports" }
            };
            context.Categories.AddRange(categories);
            await context.SaveChangesAsync();
        }

        if (!await context.Products.AnyAsync())
        {
            var electronics = await context.Categories.FirstAsync(c => c.Name == "Electronics");
            var clothing = await context.Categories.FirstAsync(c => c.Name == "Clothing");

            context.Products.AddRange(
                new Product
                {
                    Name = "Wireless Headphones",
                    Description = "Noise cancelling, 30h battery",
                    Price = 99.99m,
                    StockQuantity = 50,
                    CategoryId = electronics.Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Product
                {
                    Name = "Cotton T-Shirt",
                    Description = "100% organic cotton",
                    Price = 29.99m,
                    StockQuantity = 100,
                    CategoryId = clothing.Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            );
            await context.SaveChangesAsync();
        }
    }
}
