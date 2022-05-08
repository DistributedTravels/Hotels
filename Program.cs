using MassTransit;
using Microsoft.EntityFrameworkCore;

using Hotels.Database;
using Hotels.Database.Tables;
using Hotels.Consumers;
using Models.Hotels;

var builder = WebApplication.CreateBuilder(args);
var connString = builder.Configuration.GetConnectionString("PsqlConnection");
//initDB();

builder.Services.AddDbContext<HotelContext>(
    DbContextOptions => DbContextOptions
        .UseNpgsql(connString)
        .LogTo(Console.WriteLine, LogLevel.Information)
        .EnableSensitiveDataLogging()
        .EnableDetailedErrors()
);

// configuration for mass transit
builder.Services.AddMassTransit(cfg =>
{
    // adding consumers
    cfg.AddConsumer<GetHotelsEventConsumer>();

    // telling masstransit to use rabbitmq
    cfg.UsingRabbitMq((context, rabbitCfg) =>
    {
        // rabbitmq config
        rabbitCfg.Host("rabbitmq", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });
        // automatic endpoint configuration (and I think the reason why naming convention is important
        rabbitCfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();


// bus for publishing a message, to check if everything works
// THIS SHOULD NOT EXIST IN FINAL PROJECT
var busControl = Bus.Factory.CreateUsingRabbitMq(cfg =>
{
    cfg.Host("rabbitmq", "/", h =>
    {
        h.Username("guest");
        h.Password("guest");
    });
});
busControl.Start();
await busControl.Publish<GetHotelsEvent>(new GetHotelsEvent("Grecja", "pla풹"));
busControl.Stop();

app.Run();

void initDB()
{
    using (var context = new HotelContext())
    {
        //context.Database.EnsureDeleted();
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
        context.SaveChanges();

        var attraction = new Attraction { Name = "basen", HotelsWithAttraction = new List<AttractionInHotel> { } };
        context.Attractions.Add(attraction);
        attraction = new Attraction { Name = "spa", HotelsWithAttraction = new List<AttractionInHotel> { } };
        context.Attractions.Add(attraction);
        attraction = new Attraction { Name = "pla풹", HotelsWithAttraction = new List<AttractionInHotel> { } };
        context.Attractions.Add(attraction);
        context.SaveChanges();

        var searched_hotel = context.Hotels.Include(b => b.AttractionsInHotel).Single(b => b.Name.Equals("Hotel Acharavi Mare"));
        var searched_attraction = context.Attractions.Include(b => b.HotelsWithAttraction).Single(b => b.Name.Equals("basen"));
        var attraction_in_hotel = new AttractionInHotel { };
        searched_hotel.AttractionsInHotel.Add(attraction_in_hotel);
        searched_attraction.HotelsWithAttraction.Add(attraction_in_hotel);
        searched_hotel = context.Hotels.Include(b => b.AttractionsInHotel).Single(b => b.Name.Equals("Hotel Acharavi Mare"));
        searched_attraction = context.Attractions.Include(b => b.HotelsWithAttraction).Single(b => b.Name.Equals("pla풹"));
        attraction_in_hotel = new AttractionInHotel { };
        searched_hotel.AttractionsInHotel.Add(attraction_in_hotel);
        searched_attraction.HotelsWithAttraction.Add(attraction_in_hotel);
        searched_hotel = context.Hotels.Include(b => b.AttractionsInHotel).Single(b => b.Name.Equals("Hotel Aldemar Royal Olympian"));
        searched_attraction = context.Attractions.Include(b => b.HotelsWithAttraction).Single(b => b.Name.Equals("pla풹"));
        attraction_in_hotel = new AttractionInHotel { };
        searched_hotel.AttractionsInHotel.Add(attraction_in_hotel);
        searched_attraction.HotelsWithAttraction.Add(attraction_in_hotel);
        searched_hotel = context.Hotels.Include(b => b.AttractionsInHotel).Single(b => b.Name.Equals("Hotel Bg Pamplona"));
        searched_attraction = context.Attractions.Include(b => b.HotelsWithAttraction).Single(b => b.Name.Equals("basen"));
        attraction_in_hotel = new AttractionInHotel { };
        searched_hotel.AttractionsInHotel.Add(attraction_in_hotel);
        searched_attraction.HotelsWithAttraction.Add(attraction_in_hotel);
        searched_hotel = context.Hotels.Include(b => b.AttractionsInHotel).Single(b => b.Name.Equals("Hotel Bg Pamplona"));
        searched_attraction = context.Attractions.Include(b => b.HotelsWithAttraction).Single(b => b.Name.Equals("spa"));
        attraction_in_hotel = new AttractionInHotel { };
        searched_hotel.AttractionsInHotel.Add(attraction_in_hotel);
        searched_attraction.HotelsWithAttraction.Add(attraction_in_hotel);
        context.SaveChanges(); // save to DB

        Console.WriteLine("Done inserting test data");
    }
}
