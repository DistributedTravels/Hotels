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
                $"HotelId: {taskContext.Message.HotelId},\n" +
                $"BeginDate: {taskContext.Message.BeginDate},\n" +
                $"EndDate: {taskContext.Message.EndDate},\n" +
                $"AppartmentsAmount: {taskContext.Message.AppartmentsAmount},\n" +
                $"CasualRoomAmount: {taskContext.Message.CasualRoomAmount},\n\n"
            );

            var searched_rooms = hotelContext.Rooms
                .Include(b => b.Reservations)
                .Where(b => b.HotelId == taskContext.Message.HotelId)
                .ToList();
            Boolean can_be_reserved = AdditionalFunctions.checkIfRoomsAbleToReserve(
                    searched_rooms,
                    taskContext.Message.AppartmentsAmount,
                    taskContext.Message.CasualRoomAmount,
                    taskContext.Message.BeginDate,
                    taskContext.Message.EndDate);
            if (!can_be_reserved)
            {
                Console.WriteLine(
                    $"\n\nCan not reserve with these parameters\n" +
                    $"UserId: {taskContext.Message.UserId},\n" +
                    $"HotelId: {taskContext.Message.HotelId},\n" +
                    $"BeginDate: {taskContext.Message.BeginDate},\n" +
                    $"EndDate: {taskContext.Message.EndDate},\n" +
                    $"AppartmentsAmount: {taskContext.Message.AppartmentsAmount},\n" +
                    $"CasualRoomAmount: {taskContext.Message.CasualRoomAmount},\n\n"
                );
                await taskContext.Publish<ReserveRoomsEventReply>(
                    new ReserveRoomsEventReply(ReserveRoomsEventReply.State.NOT_RESERVED, taskContext.Message.CorrelationId));
            }
            else
            {
                int appartmentsAmountToFind = taskContext.Message.AppartmentsAmount;
                int casualRoomAmountToFind = taskContext.Message.CasualRoomAmount;
                foreach (var room in searched_rooms)
                {
                    Boolean able_to_reserve = false;
                    if (room.Type.Equals("appartment") && appartmentsAmountToFind > 0)
                    {
                        able_to_reserve = true;
                        foreach (var reservation in room.Reservations)
                        {
                            if (DateTime.Compare(reservation.EndDate, taskContext.Message.BeginDate) > 0 &&
                                DateTime.Compare(reservation.BeginDate, taskContext.Message.EndDate) < 0)
                            {
                                able_to_reserve = false;
                                break;
                            }
                        }
                        if (able_to_reserve)
                        {
                            appartmentsAmountToFind--;
                        }
                    }
                    if (room.Type.Equals("2 person") && casualRoomAmountToFind > 0)
                    {
                        able_to_reserve = true;
                        foreach (var reservation in room.Reservations)
                        {
                            if (DateTime.Compare(reservation.EndDate, taskContext.Message.BeginDate) > 0 &&
                                DateTime.Compare(reservation.BeginDate, taskContext.Message.EndDate) < 0)
                            {
                                able_to_reserve = false;
                                break;
                            }
                        }
                        if (able_to_reserve)
                        {
                            casualRoomAmountToFind--;
                        }
                    }
                    if (able_to_reserve)
                    {
                        var searched_room = hotelContext.Rooms
                            .Include(b => b.Reservations)
                            .Single(b => b.Id == room.Id);
                        var added_reservation = new Reservation
                        {
                            UserId = taskContext.Message.UserId,
                            BeginDate = taskContext.Message.BeginDate,
                            EndDate = taskContext.Message.EndDate
                        };
                        searched_room.Reservations.Add(added_reservation);
                        hotelContext.SaveChanges();
                    }
                    if (appartmentsAmountToFind <= 0 && casualRoomAmountToFind <= 0)
                    {
                        break;
                    }
                }
                Console.WriteLine(
                    $"\n\nReserved with these parameters\n" +
                    $"UserId: {taskContext.Message.UserId},\n" +
                    $"HotelId: {taskContext.Message.HotelId},\n" +
                    $"BeginDate: {taskContext.Message.BeginDate},\n" +
                    $"EndDate: {taskContext.Message.EndDate},\n" +
                    $"AppartmentsAmount: {taskContext.Message.AppartmentsAmount},\n" +
                    $"CasualRoomAmount: {taskContext.Message.CasualRoomAmount},\n\n"
                );
                await taskContext.Publish<ReserveRoomsEventReply>(
                    new ReserveRoomsEventReply(ReserveRoomsEventReply.State.RESERVED, taskContext.Message.CorrelationId));
            }
        }
    }
}
