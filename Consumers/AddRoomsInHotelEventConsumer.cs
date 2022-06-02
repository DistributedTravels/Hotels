using MassTransit;
using Models.Hotels;
using Hotels.Database;
using Hotels.Database.Tables;

using Microsoft.EntityFrameworkCore;

namespace Hotels.Consumers
{
    public class AddRoomsInHotelEventConsumer : IConsumer<AddRoomsInHotelEvent>
    {
        private readonly HotelContext hotelContext;
        public AddRoomsInHotelEventConsumer(HotelContext hotelContext)
        {
            this.hotelContext = hotelContext;
        }

        public async Task Consume(ConsumeContext<AddRoomsInHotelEvent> taskContext)
        {
            if (taskContext.Message.HotelName.Equals("any"))
            {
                Console.WriteLine(
                    $"\n\nnot added\n" +
                    $"can't be \"any\" in hotel name field\n\n"
                );
                await taskContext.RespondAsync<AddRoomsInHotelEventReply>(
                    new AddRoomsInHotelEventReply(AddRoomsInHotelEventReply.State.NOT_ADDED, taskContext.Message.CorrelationId));
                return;
            }
            var searched_hotels = hotelContext.Hotels
                .Include(b => b.Rooms)
                .Where(b => !b.Removed)
                .Where(b => b.Name.Equals(taskContext.Message.HotelName)).ToList();
            if (!searched_hotels.Any())
            {
                Console.WriteLine(
                    $"\n\nnot added\n" +
                    $"hotel with this name does not exist\n\n"
                );
                await taskContext.RespondAsync<AddRoomsInHotelEventReply>(
                    new AddRoomsInHotelEventReply(AddRoomsInHotelEventReply.State.NOT_ADDED, taskContext.Message.CorrelationId));
                return;
            }
            var searched_hotel = searched_hotels[0];
            if (searched_hotel.Rooms == null)
            {
                searched_hotel.Rooms = new List<Room>();
            }
            for (int i = 0; i < taskContext.Message.AppartmentsAmountToAdd; i++)
            {
                searched_hotel.Rooms.Add(new Room
                {
                    Type = "appartment",
                    Reservations = new List<Reservation> { }
                });
            }
            for (int i = 0; i < taskContext.Message.CasualRoomAmountToAdd; i++)
            {
                searched_hotel.Rooms.Add(new Room
                {
                    Type = "2 person",
                    Reservations = new List<Reservation> { }
                });
            }
            hotelContext.SaveChanges();
            Console.WriteLine($"\n\nadded\n\n");
            await taskContext.RespondAsync<AddRoomsInHotelEventReply>(
                    new AddRoomsInHotelEventReply(AddRoomsInHotelEventReply.State.ADDED, taskContext.Message.CorrelationId));
        }
    }
}
