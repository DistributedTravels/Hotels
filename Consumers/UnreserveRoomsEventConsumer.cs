using MassTransit;
using Models.Hotels;
using Hotels.Database;
using Hotels.Database.Tables;
using Models.Offers;

using Microsoft.EntityFrameworkCore;

namespace Hotels.Consumers
{
    public class UnreserveRoomsEventConsumer : IConsumer<UnreserveRoomsEvent>
    {
        private readonly HotelContext hotelContext;
        public UnreserveRoomsEventConsumer(HotelContext hotelContext)
        {
            this.hotelContext = hotelContext;
        }

        public async Task Consume(ConsumeContext<UnreserveRoomsEvent> taskContext)
        {
            Console.WriteLine(
                $"\n\nReceived message:\n" +
                $"ReservationNumber: {taskContext.Message.ReservationNumber},\n\n"
            );
            var searched_reservations = hotelContext.Reservations
                .Include(b => b.Room)
                .Where(b => b.ReservationNumber.Equals(taskContext.Message.ReservationNumber));
            var searched_rooms = hotelContext.Rooms
                    .Include(b => b.Hotel)
                    .Include(b => b.Reservations)
                    .Where(b => b.Id == searched_reservations.First().Room.Id)
                    .Where(b => !b.Hotel.Removed)
                    .Where(b => !b.Removed).ToList();
            if (!searched_rooms.Any())
            {
                Console.WriteLine(
                    $"\n\nlack of that room\n\n"
                );
                await taskContext.RespondAsync<UnreserveRoomsEventReply>(
                    new UnreserveRoomsEventReply(
                        UnreserveRoomsEventReply.State.RESERVED,
                        taskContext.Message.CorrelationId));
                return;
            }
            var searched_hotel = searched_rooms.First().Hotel;

            foreach (var reservation in searched_reservations)
            {
                if (!reservation.Room.Removed)
                {
                    hotelContext.Reservations.Remove(reservation);
                }
            }
            hotelContext.SaveChanges();
            await taskContext.RespondAsync<UnreserveRoomsEventReply>(
                    new UnreserveRoomsEventReply(
                        UnreserveRoomsEventReply.State.NOT_RESERVED,
                        taskContext.Message.CorrelationId));

            var searched_rooms_query = hotelContext.Rooms
                .Include(b => b.Hotel)
                .Include(b => b.Reservations)
                .Where(b => b.HotelId == searched_hotel.Id)
                .Where(b => !b.Hotel.Removed)
                .Where(b => !b.Removed);
            var room_numbers = AdditionalFunctions.calculate_rooms_count(
                searched_rooms_query.ToList(), taskContext.Message.CreationDate,
                taskContext.Message.CreationDate.AddDays(1));
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
