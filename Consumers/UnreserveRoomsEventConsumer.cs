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
            var proper_unreservation = false;
            foreach (var reservation in searched_reservations)
            {
                if (!reservation.Room.Removed)
                {
                    hotelContext.Reservations.Remove(reservation);
                    proper_unreservation = true;
                }
            }
            if (proper_unreservation)
            {
                hotelContext.SaveChanges();
                await taskContext.RespondAsync<UnreserveRoomsEventReply>(
                        new UnreserveRoomsEventReply(
                            UnreserveRoomsEventReply.State.NOT_RESERVED,
                            taskContext.Message.CorrelationId));
                var searched_rooms_query = hotelContext.Rooms
                    .Include(b => b.Hotel)
                    .Include(b => b.Reservations)
                    .Where(b => b.Id == searched_reservations.First().Room.Id)
                    .Where(b => !b.Hotel.Removed)
                    .Where(b => !b.Removed);
                var room_numbers = AdditionalFunctions.calculate_rooms_count(
                    searched_rooms_query.ToList(), taskContext.Message.CreationDate,
                    taskContext.Message.CreationDate.AddDays(1));
                await taskContext.RespondAsync<ChangesInOffersEvent>(
                    new ChangesInOffersEvent
                    {
                        HotelId = searched_rooms_query.First().Hotel.Id,
                        HotelName = searched_rooms_query.First().Hotel.Name,
                        BigRoomsAvailable = room_numbers.apartment_count,
                        SmallRoomsAvaialable = room_numbers.casual_room_count,
                        WifiAvailable = searched_rooms_query.First().Hotel.HasWifi,
                        BreakfastAvailable = (searched_rooms_query.First().Hotel.BreakfastPrice >= 0.0 ? true : false),
                        HotelPricePerPerson = searched_rooms_query.First().Hotel.PriceForNightForPerson,
                        TransportId = -1,
                        TransportPricePerSeat = -1.0,
                        PlaneAvailable = false
                    });
            }
            else
            {
                Console.WriteLine($"\n\nunreservation in removed room, unchanged\n\n");
                await taskContext.RespondAsync<UnreserveRoomsEventReply>(
                        new UnreserveRoomsEventReply(
                            UnreserveRoomsEventReply.State.RESERVED,
                            taskContext.Message.CorrelationId));
            }
        }
    }
}
