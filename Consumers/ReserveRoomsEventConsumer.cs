using MassTransit;
using Models.Hotels;
using Hotels.Database;
using Hotels.Database.Tables;

using Microsoft.EntityFrameworkCore;

namespace Hotels.Consumers
{
    public class ReserveRoomsEventConsumer : IConsumer<ReserveRoomsEvent>
    {
        private readonly HotelContext hotelContext;
        public ReserveRoomsEventConsumer(HotelContext hotelContext)
        {
            this.hotelContext = hotelContext;
        }

        public async Task Consume(ConsumeContext<ReserveRoomsEvent> taskContext) 
        {
            Console.WriteLine(
                $"\n\nReceived message:\n" +
                $"HotelName: {taskContext.Message.HotelName},\n" +
                $"BeginDate: {taskContext.Message.BeginDate},\n" +
                $"EndDate: {taskContext.Message.EndDate},\n" +
                $"AppartmentsAmount: {taskContext.Message.AppartmentsAmount},\n" +
                $"CasualRoomAmount: {taskContext.Message.CasualRoomAmount},\n\n"
            );

        }
    }
}
