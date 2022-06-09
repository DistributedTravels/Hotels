using MassTransit;
using Models.Hotels;
using Hotels.Database;
using Hotels.Database.Tables;
using Models.Offers;

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
            if (taskContext.Message.AppartmentsAmountToAdd == 0 && taskContext.Message.CasualRoomAmountToAdd == 0)
            {
                Console.WriteLine(
                    $"\n\nnot added\n" +
                    $"amount unchanged\n\n"
                );
                return;
            }
            
            var searched_hotels = hotelContext.Hotels
                .Include(b => b.Rooms)
                .Where(b => !b.Removed)
                .Where(b => b.Id == taskContext.Message.HotelId).ToList();
            if (!searched_hotels.Any())
            {
                Console.WriteLine(
                    $"\n\nnot added\n" +
                    $"hotel with this name does not exist\n\n"
                );
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
            var searched_rooms_query = hotelContext.Rooms
                .Include(b => b.Hotel)
                .Include(b => b.Reservations)
                .Where(b => b.Hotel.Id == taskContext.Message.HotelId)
                .Where(b => !b.Hotel.Removed)
                .Where(b => !b.Removed);
            var room_numbers = AdditionalFunctions.calculate_rooms_count(
                searched_rooms_query.ToList(), taskContext.Message.CreationDate);
            await taskContext.RespondAsync<ChangesInOffersEvent>(
                new ChangesInOffersEvent
                {
                    HotelId = searched_hotel.Id,
                    HotelName = searched_hotel.Name,
                    BigRoomsAvailable = room_numbers.apartment_count,
                    SmallRoomsAvaialable = room_numbers.casual_room_count,
                    WifiAvailable = searched_hotel.HasWifi,
                    BreakfastAvailable = (searched_hotel.BreakfastPrice >= 0.0 ? true : false),
                    HotelPricePerPerson = searched_hotel.PriceForNightForPerson,
                    TransportId = -1,
                    TransportPricePerSeat = -1.0,
                    PlaneAvailable = false,
                    BreakfastPrice = searched_hotel.BreakfastPrice
                });
        }
    }
}
