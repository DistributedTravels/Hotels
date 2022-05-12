using MassTransit;
using Models.Hotels;
using Hotels.Database;
using Hotels.Database.Tables;

using Microsoft.EntityFrameworkCore;

namespace Hotels.Consumers
{
    public class GetInfoFromHotelEventConsumer : IConsumer<GetInfoFromHotelEvent>
    {
        private readonly HotelContext hotelContext;
        public GetInfoFromHotelEventConsumer(HotelContext hotelContext)
        {
            this.hotelContext = hotelContext;
        }

        public async Task Consume(ConsumeContext<GetInfoFromHotelEvent> taskContext)
        {
            Console.WriteLine(
                $"\n\nReceived message:\n" +
                $"HotelId: {taskContext.Message.HotelId},\n" +
                $"BeginDate: {taskContext.Message.BeginDate},\n" +
                $"EndDate: {taskContext.Message.EndDate},\n" +
                $"AppartmentsAmount: {taskContext.Message.AppartmentsAmount},\n" +
                $"CasualRoomAmount: {taskContext.Message.CasualRoomAmount},\n" +
                $"Breakfast: {taskContext.Message.Breakfast},\n" +
                $"Internet: {taskContext.Message.Wifi}\n\n"
            );

            var searched_rooms_query = hotelContext.Rooms
                .Include(b => b.Hotel)
                .Include(b => b.Reservations)
                .Where(b => b.HotelId == taskContext.Message.HotelId);
            if (taskContext.Message.Breakfast)
            {
                searched_rooms_query = searched_rooms_query.Where(b => b.Hotel.HasBreakfast == true);
            }
            if (taskContext.Message.Wifi)
            {
                searched_rooms_query = searched_rooms_query.Where(b => b.Hotel.HasWifi == true);
            }
            var searched_rooms = searched_rooms_query.ToList();

            var roomsToReserve = new List<Room>();
            double price = AdditionalFunctions.checkIfRoomsAbleToReserve(
                    searched_rooms,
                    taskContext.Message.AppartmentsAmount,
                    taskContext.Message.CasualRoomAmount,
                    taskContext.Message.BeginDate,
                    taskContext.Message.EndDate,
                    roomsToReserve);
            if (price <= 0.0)
            {
                Console.WriteLine(
                    $"\n\nCan not reserve with these parameters\n" +
                    $"HotelId: {taskContext.Message.HotelId},\n" +
                    $"BeginDate: {taskContext.Message.BeginDate},\n" +
                    $"EndDate: {taskContext.Message.EndDate},\n" +
                    $"AppartmentsAmount: {taskContext.Message.AppartmentsAmount},\n" +
                    $"CasualRoomAmount: {taskContext.Message.CasualRoomAmount},\n" +
                    $"Breakfast: {taskContext.Message.Breakfast},\n" +
                    $"Internet: {taskContext.Message.Wifi}\n\n"
                );
                await taskContext.Publish<GetInfoFromHotelEventReply>(
                    new GetInfoFromHotelEventReply(
                        GetInfoFromHotelEventReply.State.CAN_NOT_BE_RESERVED,
                        0.0,
                        taskContext.Message.CorrelationId));
            }
            else
            {
                Console.WriteLine(
                    $"\n\nCan be reserved with these parameters\n" +
                    $"HotelId: {taskContext.Message.HotelId},\n" +
                    $"BeginDate: {taskContext.Message.BeginDate},\n" +
                    $"EndDate: {taskContext.Message.EndDate},\n" +
                    $"AppartmentsAmount: {taskContext.Message.AppartmentsAmount},\n" +
                    $"CasualRoomAmount: {taskContext.Message.CasualRoomAmount},\n" +
                    $"price: {price},\n" +
                    $"Breakfast: {taskContext.Message.Breakfast},\n" +
                    $"Internet: {taskContext.Message.Wifi}\n\n"
                );
                await taskContext.Publish<GetInfoFromHotelEventReply>(
                    new GetInfoFromHotelEventReply(
                        GetInfoFromHotelEventReply.State.CAN_BE_RESERVED,
                        price,
                        taskContext.Message.CorrelationId));
            }
        }
    }
}
