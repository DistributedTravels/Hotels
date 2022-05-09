using MassTransit;
using Models.Hotels;
using Hotels.Database;
using Hotels.Database.Tables;

using Microsoft.EntityFrameworkCore;

namespace Hotels.Consumers
{
    public class AdditionalFunctions
    {
        public static Boolean checkIfRoomsAbleToReserve(List<Room> searched_rooms,
            int appartmentsAmountToFind, int casualRoomAmountToFind,
            DateTime beginDate, DateTime endDate)
        {
            foreach (var room in searched_rooms)
            {
                Console.WriteLine(
                    $"\n\nappartmentsAmountToFind: {appartmentsAmountToFind},\n" +
                    $"casualRoomAmountToFind: {casualRoomAmountToFind},\n" +
                    $"room.Id: {room.Id},\n\n"
                );
                if (room.Type.Equals("appartment") && appartmentsAmountToFind > 0)
                {
                    Boolean able_to_reserve = true;
                    foreach (var reservation in room.Reservations)
                    {
                        if (DateTime.Compare(reservation.EndDate, beginDate) > 0 &&
                            DateTime.Compare(reservation.BeginDate, endDate) < 0)
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
                    Boolean able_to_reserve = true;
                    foreach (var reservation in room.Reservations)
                    {
                        if (DateTime.Compare(reservation.EndDate, beginDate) > 0 &&
                            DateTime.Compare(reservation.BeginDate, endDate) < 0)
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
                if (appartmentsAmountToFind <= 0 && casualRoomAmountToFind <= 0)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
