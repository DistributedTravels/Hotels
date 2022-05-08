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
await busControl.Publish<GetHotelsEvent>(
    new GetHotelsEvent(
        "Grecja",
        new DateTime(2022, 4, 1),
        new DateTime(2022, 4, 7),
        4, 4, true, true));
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
            HasInternet = true,
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
            HasInternet = false,
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
            Country = "Grecja",
            HasBreakfast = false,
            HasInternet = true,
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
        context.SaveChanges();

        Console.WriteLine("Done inserting test data");
    }
}
