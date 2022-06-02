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
                $"UserId: {taskContext.Message.UserId},\n" +
                $"ReservationNumber: {taskContext.Message.ReservationNumber},\n" +
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
                .Where(b => b.HotelId == taskContext.Message.HotelId)
                .Where(b => !b.Hotel.Removed)
                .Where(b => !b.Removed);
            if (taskContext.Message.Breakfast)
            {
                searched_rooms_query = searched_rooms_query.Where(b => b.Hotel.BreakfastPrice > 0.0);
            }
            if (taskContext.Message.Wifi)
            {
                searched_rooms_query = searched_rooms_query.Where(b => b.Hotel.HasWifi == true);
            }
            var searched_rooms = searched_rooms_query.ToList();

            var roomsToReserve = new List<Room>();
            var results = AdditionalFunctions.checkIfRoomsAbleToReserve(
                    searched_rooms,
                    taskContext.Message.AppartmentsAmount,
                    taskContext.Message.CasualRoomAmount,
                    taskContext.Message.Breakfast,
                    taskContext.Message.BeginDate,
                    taskContext.Message.EndDate,
                    roomsToReserve);
            if (results.price <= 0.0)
            {
                Console.WriteLine(
                    $"\n\nCan not reserve with these parameters\n" +
                    $"UserId: {taskContext.Message.UserId},\n" +
                    $"ReservationNumber: {taskContext.Message.ReservationNumber},\n" +
                    $"HotelId: {taskContext.Message.HotelId},\n" +
                    $"BeginDate: {taskContext.Message.BeginDate},\n" +
                    $"EndDate: {taskContext.Message.EndDate},\n" +
                    $"AppartmentsAmount: {taskContext.Message.AppartmentsAmount},\n" +
                    $"CasualRoomAmount: {taskContext.Message.CasualRoomAmount},\n" +
                    $"Breakfast: {taskContext.Message.Breakfast},\n" +
                    $"Internet: {taskContext.Message.Wifi}\n\n"
                );
                await taskContext.RespondAsync<ReserveRoomsEventReply>(
                    new ReserveRoomsEventReply(ReserveRoomsEventReply.State.NOT_RESERVED, 0.0, taskContext.Message.CorrelationId));
            }
            else
            {
                foreach (var room in roomsToReserve)
                {
                    var added_reservation = new Reservation
                    {
                        UserId = taskContext.Message.UserId,
                        ReservationNumber = taskContext.Message.ReservationNumber,
                        BeginDate = taskContext.Message.BeginDate.ToUniversalTime(),
                        EndDate = taskContext.Message.EndDate.ToUniversalTime(),
                        BreakfastRequired = taskContext.Message.Breakfast,
                        WifiRequired = taskContext.Message.Wifi,
                        CalculatedCost = results.price,
                        PersonsNumber = results.num_of_persons,
                        NightsNumber = results.num_of_nights
                    };
                    room.Reservations.Add(added_reservation);
                }
                hotelContext.SaveChanges();
                Console.WriteLine(
                    $"\n\nReserved with these parameters\n" +
                    $"UserId: {taskContext.Message.UserId},\n" +
                    $"ReservationNumber: {taskContext.Message.ReservationNumber},\n" +
                    $"HotelId: {taskContext.Message.HotelId},\n" +
                    $"BeginDate: {taskContext.Message.BeginDate},\n" +
                    $"EndDate: {taskContext.Message.EndDate},\n" +
                    $"AppartmentsAmount: {taskContext.Message.AppartmentsAmount},\n" +
                    $"CasualRoomAmount: {taskContext.Message.CasualRoomAmount},\n" +
                    $"price: {results.price},\n" +
                    $"Breakfast: {taskContext.Message.Breakfast},\n" +
                    $"Internet: {taskContext.Message.Wifi}\n\n"
                );
                await taskContext.RespondAsync<ReserveRoomsEventReply>(
                    new ReserveRoomsEventReply(
                        ReserveRoomsEventReply.State.RESERVED,
                        results.price,
                        taskContext.Message.CorrelationId));
            }
        }
    }
}
