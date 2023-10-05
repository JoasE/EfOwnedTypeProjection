using Microsoft.EntityFrameworkCore;

using var context = new TestDbContext();
await context.Database.EnsureCreatedAsync();
var newPerson = new Person("name");
context.Add(newPerson);
await context.SaveChangesAsync();
context.Entry(newPerson).State = EntityState.Detached;

var persons = await context.Persons.AsNoTracking().Select(x => new
{
    x.Id,
    x.Name,
    Addresses = x.Homes.Select(x => x.Address.Value).ToArray(),
}).ToArrayAsync();

Console.ReadLine();

public class Person
{
    private readonly List<Home> _homes = new();

    public Person(string name)
    {
        Name = name;    
    }

    private Person() { }

    public Guid Id { get; } = Guid.NewGuid();

    public string Name { get; }

    public IEnumerable<Home> Homes => _homes;

    public void AddHome(Home home)
    {
        _homes.Add(home);
    }
}

public class Home
{
    public Home(Address address)
    {
        Address = address;
    }

    private Home()
    {
    }

    public Guid Id { get; } = Guid.NewGuid();

    public Address Address { get; }
}

public class Address
{
    public Address(string value)
    {
        Value = value;
    }

    private Address() { }

    public string Value { get; }
}

public class TestDbContext : DbContext
{
    public TestDbContext() : base(new DbContextOptionsBuilder<TestDbContext>().UseCosmos("AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==", "Test").Options)
    {
    }

    public DbSet<Person> Persons { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UsePropertyAccessMode(PropertyAccessMode.PreferFieldDuringConstruction);

        modelBuilder.Entity<Person>(p =>
        {
            p.Property(x => x.Id).ValueGeneratedNever().ToJsonProperty("id");
            p.HasKey(x => x.Id);
            p.Property(x => x.Name);
            p.OwnsMany(x => x.Homes, h =>
            {
                h.Property(x => x.Id).ValueGeneratedNever();
                h.OwnsOne(x => x.Address, a =>
                {
                    a.Property(x => x.Value);
                });
            });
            p.HasPartitionKey(x => x.Id);
        });
    }
}