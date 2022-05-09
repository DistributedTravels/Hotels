using MassTransit;
using Models.Hotels;
using Hotels.Database;
using Hotels.Database.Tables;

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
                .Where(b => b.ReservationNumber == taskContext.Message.ReservationNumber);
            foreach (var reservation in searched_reservations)
            {
                hotelContext.Reservations.Remove(reservation);
            }
            hotelContext.SaveChanges();
            await taskContext.Publish<UnreserveRoomsEventReply>(
                    new UnreserveRoomsEventReply(
                        UnreserveRoomsEventReply.State.NOT_RESERVED, 
                        taskContext.Message.CorrelationId));
        }
    }
}
