using Hotels.Database;
using Hotels.Database.Tables;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();
var connString = builder.Configuration.GetConnectionString("PsqlConnection");
//var manager = new EventManager(connString);
initDB();
//manager.ListenForEvents();

app.Run();

void initDB()
{
    var options = new DbContextOptionsBuilder<HotelContext>()
        .UseNpgsql(connString)
        .LogTo(Console.WriteLine, LogLevel.Information)
        .Options;

    using (var context = new HotelContext(options))
    {
        context.Database.EnsureCreated();
        
        var country = new Country { Name = "Grecja", Hotels = new List<Hotel> { } };
        context.Countries.Add(country); // add new item
        country = new Country { Name = "Hiszpania", Hotels = new List<Hotel> { } };
        context.Countries.Add(country);
        context.SaveChanges();

        var searched_country = context.Countries.Include(b => b.Hotels).Single(b => b.Name.Equals("Grecja"));
        var hotel = new Hotel { Name = "Hotel Acharavi Mare" };
        searched_country.Hotels.Add(hotel);

        hotel = new Hotel { Name = "Hotel Aldemar Royal Olympian" };
        searched_country.Hotels.Add(hotel);

        searched_country = context.Countries.Include(b => b.Hotels).Single(b => b.Name.Equals("Hiszpania"));
        hotel = new Hotel { Name = "Hotel Bg Pamplona" };
        searched_country.Hotels.Add(hotel);

        context.SaveChanges(); // save to DB
        Console.WriteLine("Done inserting test data");
        // manager.Publish(new ReserveTransportEvent(1));
    }
}
