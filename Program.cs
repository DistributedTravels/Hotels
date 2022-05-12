using MassTransit;
using Microsoft.EntityFrameworkCore;

using Hotels.Database;
using Hotels.Database.Tables;
using Hotels.Consumers;
using Models.Hotels;

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
//initDB();

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

await busControl.Publish<GetInfoFromHotelEvent>(
    new GetInfoFromHotelEvent(
        3,
        new DateTime(2022, 6, 1).ToUniversalTime(),
        new DateTime(2022, 6, 6).ToUniversalTime(),
        2, 2,
        false, true));

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

        var hotel = new Hotel
        {
            Name = "Hotel Acharavi Mare",
            Country = "Grecja",
            HasBreakfast = true,
            HasWifi = true,
            PriceForNightForPerson = 50.0,
            Rooms = new List<Room>
            {
                new Room
                {
                    Type = "appartment",
                    Reservations = new List<Reservation>
                    {
                        new Reservation
                        {
                            BeginDate = new DateTime(2022, 5, 1).ToUniversalTime(),
                            EndDate = new DateTime(2022, 6, 1).ToUniversalTime(),
                        },
                        new Reservation
                        {
                            BeginDate = new DateTime(2022, 6, 6).ToUniversalTime(),
                            EndDate = new DateTime(2022, 6, 9).ToUniversalTime(),
                        }
                    }
                },
                new Room
                {
                    Type = "appartment",
                    Reservations = new List<Reservation>{}
                },
                new Room
                {
                    Type = "appartment",
                    Reservations = new List<Reservation>{}
                },
                new Room
                {
                    Type = "appartment",
                    Reservations = new List<Reservation>{}
                },
                new Room
                {
                    Type = "2 person",
                    Reservations = new List<Reservation>{}
                },
                new Room
                {
                    Type = "2 person",
                    Reservations = new List<Reservation>{}
                },
                new Room
                {
                    Type = "2 person",
                    Reservations = new List<Reservation>{}
                },
                new Room
                {
                    Type = "2 person",
                    Reservations = new List<Reservation>{}
                }
            }
        };
        context.Hotels.Add(hotel);
        hotel = new Hotel
        {
            Name = "Hotel Aldemar Royal Olympian",
            Country = "Grecja",
            HasBreakfast = true,
            HasWifi = false,
            PriceForNightForPerson = 60.0,
            Rooms = new List<Room>
            {
                new Room
                {
                    Type = "appartment",
                    Reservations = new List<Reservation>{}
                },
                new Room
                {
                    Type = "appartment",
                    Reservations = new List<Reservation>{}
                },
                new Room
                {
                    Type = "2 person",
                    Reservations = new List<Reservation>{}
                },
                new Room
                {
                    Type = "2 person",
                    Reservations = new List<Reservation>{}
                }
            }
        };
        context.Hotels.Add(hotel);
        hotel = new Hotel
        {
            Name = "Hotel Bg Pamplona",
            Country = "Hiszpania",
            HasBreakfast = false,
            HasWifi = true,
            PriceForNightForPerson = 70.0,
            Rooms = new List<Room>
            {
                new Room
                {
                    Type = "appartment",
                    Reservations = new List<Reservation>{}
                },
                new Room
                {
                    Type = "appartment",
                    Reservations = new List<Reservation>{}
                },
                new Room
                {
                    Type = "2 person",
                    Reservations = new List<Reservation>{}
                },
                new Room
                {
                    Type = "2 person",
                    Reservations = new List<Reservation>{}
                }
            }
        };
        context.Hotels.Add(hotel);
        hotel = new Hotel
        {
            Name = "Small hotel",
            Country = "Hiszpania",
            HasBreakfast = false,
            HasWifi = true,
            PriceForNightForPerson = 80.0,
            Rooms = new List<Room>
            {
                new Room
                {
                    Type = "2 person",
                    Reservations = new List<Reservation>{}
                }
            }
        };
        context.Hotels.Add(hotel);
        context.SaveChanges();

        Console.WriteLine("Done inserting test data");
    }
}
