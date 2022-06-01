using MassTransit;
using Microsoft.EntityFrameworkCore;

using Hotels.Database;
using Hotels.Database.Tables;
using Hotels.Consumers;
using Models.Hotels;
using Newtonsoft.Json;

var builder = WebApplication.CreateBuilder(args);
var connString = builder.Configuration.GetConnectionString("PsqlConnection");

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
    cfg.AddConsumer<GetInfoFromHotelEventConsumer>();
    cfg.AddConsumer<ReserveRoomsEventConsumer>();
    cfg.AddConsumer<UnreserveRoomsEventConsumer>();
    cfg.AddConsumer<AddHotelEventConsumer>();

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
initDB();

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

//await busControl.Publish<GetHotelsEvent>(
//    new GetHotelsEvent("Grecja"));

//await busControl.Publish<GetInfoFromHotelEvent>(
//    new GetInfoFromHotelEvent(
//        3,
//        new DateTime(2022, 6, 1).ToUniversalTime(),
//        new DateTime(2022, 6, 6).ToUniversalTime(),
//        2, 2,
//        false, true));

//await busControl.Publish<ReserveRoomsEvent>(
//    new ReserveRoomsEvent(
//        1,
//        new DateTime(2022, 6, 1).ToUniversalTime(),
//        new DateTime(2022, 6, 7).ToUniversalTime(),
//        4, 4,
//        new Guid(1, 2, 3, new byte[] { 0, 1, 2, 3, 4, 5, 6, 7 }),
//        new Guid(3, 2, 1, new byte[] { 0, 1, 2, 3, 4, 5, 6, 7 }),
//        false, true));

//await busControl.Publish<UnreserveRoomsEvent>(
//    new UnreserveRoomsEvent(
//        new Guid(3, 2, 1, new byte[] { 0, 1, 2, 3, 4, 5, 6, 7 })));

busControl.Stop();

app.Run();

void initDB()
{
    using (var contScope = app.Services.CreateScope())
    using (var context = contScope.ServiceProvider.GetRequiredService<HotelContext>())
    {
        //context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
        if (!context.Hotels.Any())
        {
            using (var r = new StreamReader(@"Init/hotels.json"))
            {
                string json = r.ReadToEnd();
                List<HotelFromJson> hotelsFromJson = JsonConvert.DeserializeObject<List<HotelFromJson>>(json);
                var random = new Random();
                foreach (var hotelFromJson in hotelsFromJson)
                {
                    var priceForNightForPerson = Math.Round(random.NextDouble() * (100.0 - 35.0) + 35.0, 2);
                    double breakfastPrice;
                    if (random.Next(3) == 0)
                    {
                        breakfastPrice = -1.0;
                    }
                    else
                    {
                        breakfastPrice = Math.Round(random.NextDouble() * (7.0 - 1.0) + 1.0, 2);
                    }
                    bool hasWifi;
                    if (random.Next(3) == 0)
                    {
                        hasWifi = false;
                    }
                    else
                    {
                        hasWifi = true;
                    }
                    int appartments_number = random.Next(7);
                    int casual_rooms_number = random.Next(6) + 1;
                    var rooms = new List<Room>();
                    for(int i=0; i < appartments_number; i++)
                    {
                        rooms.Add(new Room
                        {
                            Type = "appartment",
                            Reservations = new List<Reservation> { }
                        });
                    }
                    for (int i = 0; i < casual_rooms_number; i++)
                    {
                        rooms.Add(new Room
                        {
                            Type = "2 person",
                            Reservations = new List<Reservation> { }
                        });
                    }

                    var hotel = new Hotel
                    {
                        Name = hotelFromJson.Name,
                        Country = hotelFromJson.Country,
                        BreakfastPrice = breakfastPrice,
                        HasWifi = hasWifi,
                        PriceForNightForPerson = priceForNightForPerson,
                        Rooms = rooms,
                    };
                    context.Hotels.Add(hotel);
                }
            }
            context.SaveChanges();
        }
    }
}

class HotelFromJson
{
    public string Name { get; set; }
    public string Country { get; set; }
}