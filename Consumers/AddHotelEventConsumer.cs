using MassTransit;
using Models.Hotels;
using Hotels.Database;
using Hotels.Database.Tables;
using Models.Offers;

using Microsoft.EntityFrameworkCore;

namespace Hotels.Consumers
{
    public class AddHotelEventConsumer : IConsumer<AddHotelEvent>
    {
        private readonly HotelContext hotelContext;
        public AddHotelEventConsumer(HotelContext hotelContext)
        {
            this.hotelContext = hotelContext;
        }

        public async Task Consume(ConsumeContext<AddHotelEvent> taskContext)
        {
            if (taskContext.Message.Country.Equals("any") || taskContext.Message.Name.Equals("any"))
            {
                Console.WriteLine(
                    $"\n\nnot added\n" +
                    $"can't be \"any\" in name or country field\n\n"
                );
            }
            else if (hotelContext.Hotels.Where(b => !b.Removed)
                .Where(b => b.Name.Equals(taskContext.Message.Name))
                .ToList().Any())
            {
                Console.WriteLine(
                    $"\n\nnot added\n" +
                    $"hotel with given name already exists\n\n"
                );
            }
            else
            {
                var rooms = new List<Room>();
                for (int i = 0; i < taskContext.Message.AppartmentsAmount; i++)
                {
                    rooms.Add(new Room
                    {
                        Type = "appartment",
                        Reservations = new List<Reservation> { }
                    });
                }
                for (int i = 0; i < taskContext.Message.CasualRoomAmount; i++)
                {
                    rooms.Add(new Room
                    {
                        Type = "2 person",
                        Reservations = new List<Reservation> { }
                    });
                }

                var hotel = new Hotel
                {
                    Name = taskContext.Message.Name,
                    Country = taskContext.Message.Country,
                    BreakfastPrice = taskContext.Message.BreakfastPrice,
                    HasWifi = taskContext.Message.HasWifi,
                    PriceForNightForPerson = taskContext.Message.PriceForNightForPerson,
                    Rooms = rooms,
                };
                hotelContext.Hotels.Add(hotel);
                await hotelContext.SaveChangesAsync();
                Console.WriteLine($"\n\nadded\n\n");
            }
        }
    }
}
