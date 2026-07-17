using Bogus;
using Catalog.Domain.Entities;

namespace Catalog.Infrastructure.Persistence;

public class DataSeeder
{
    private readonly AppDbContext _context;

    public DataSeeder(AppDbContext context)
    {
        _context = context;
    }

    public void Seed(int total = 20_000)
    {
        const int batchSize = 10_000;

        var faker = new Faker<Product>()
            .RuleFor(p => p.Name, f => f.Commerce.ProductName())
            .RuleFor(p => p.Category, f => f.Commerce.Categories(1)[0])
            .RuleFor(p => p.Price, f => Math.Round(f.Random.Decimal(0.99m, 9_999.99m), 2))
            .RuleFor(p => p.Stock, f => f.Random.Int(0, 500))
            .RuleFor(p => p.CreatedAt, f => f.Date.Past(2));

        for (int i = 0; i < total; i += batchSize)
        {
            int count = Math.Min(batchSize, total - i);
            _context.Products.AddRange(faker.Generate(count));
            _context.SaveChanges();
            _context.ChangeTracker.Clear();
        }
    }
}
