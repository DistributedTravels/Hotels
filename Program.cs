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
    cfg.AddConsumer<AddRoomsInHotelEventConsumer>();
    cfg.AddConsumer<DeleteHotelEventConsumer>();
    cfg.AddConsumer<DeleteRoomsInHotelEventConsumer>();
    cfg.AddConsumer<ChangeBasePriceEventConsumer>();
    cfg.AddConsumer<ChangeBreakfastPriceEventConsumer>();
    cfg.AddConsumer<ChangeWifiAvailabilityEventConsumer>();
    cfg.AddConsumer<ChangeNamesEventConsumer>();

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
//    new ReserveRoomsEvent
//    {
//        HotelId = 91,
//        BeginDate = new DateTime(2022, 6, 12).ToUniversalTime(),
//        EndDate = new DateTime(2022, 6, 16).ToUniversalTime(),
//        AppartmentsAmount = 1,
//        CasualRoomAmount = 1,
//        UserId = Guid.Parse("11111111-0000-0000-0001-000000000002"),
//        ReservationNumber = Guid.Parse("00000000-0000-0000-0001-000000000005"),
//        Breakfast = false,
//        Wifi = true
//    });

//await busControl.Publish<UnreserveRoomsEvent>(
//    new UnreserveRoomsEvent(
//        Guid.Parse("00000000-0000-0000-0001-000000000001")));

//await busControl.Publish<AddHotelEvent>(
//    new AddHotelEvent
//    {
//        Name = "abc",
//        Country = "Litwa",
//        BreakfastPrice = 2.0,
//        HasWifi = true,
//        PriceForNightForPerson = 50.0,
//        AppartmentsAmount = 0,
//        CasualRoomAmount = 0
//    });

//await busControl.Publish<DeleteHotelEvent>(
//    new DeleteHotelEvent
//    {
//        Name = "abc"
//    });

//await busControl.Publish<AddRoomsInHotelEvent>(
//    new AddRoomsInHotelEvent
//    {
//        HotelName = "abc",
//        AppartmentsAmountToAdd = 2,
//        CasualRoomAmountToAdd = 1,
//    });

//await busControl.Publish<DeleteRoomsInHotelEvent>(
//    new DeleteRoomsInHotelEvent
//    {
//        HotelName = "abc",
//        AppartmentsAmountToDelete = 1,
//        CasualRoomAmountToDelete = 2,
//    });

//await busControl.Publish<ChangeBasePriceEvent>(
//    new ChangeBasePriceEvent
//    {
//        HotelName = "abc",
//        NewPrice = 48.0
//    });

//await busControl.Publish<ChangeBreakfastPriceEvent>(
//    new ChangeBreakfastPriceEvent
//    {
//        HotelName = "abc",
//        NewPrice = -1.0
//    });

//await busControl.Publish<ChangeWifiAvailabilityEvent>(
//    new ChangeWifiAvailabilityEvent
//    {
//        HotelName = "abc",
//        Wifi = true
//    });

//await busControl.Publish<ChangeNamesEvent>(
//    new ChangeNamesEvent
//    {
//        ChangedParameter = "name",
//        OldName = "abc",
//        NewName = "def",
//        NewCountry = "wololo"
//    });

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