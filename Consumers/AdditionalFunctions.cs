using MassTransit;
using Models.Hotels;
using Hotels.Database;
using Hotels.Database.Tables;

using Microsoft.EntityFrameworkCore;

namespace Hotels.Consumers
{
    public class AdditionalFunctions
    {
        public static double checkIfRoomsAbleToReserve(List<Room> searched_rooms,
            int appartmentsAmountToFind, int casualRoomAmountToFind,
            bool breakfastChecked,
            DateTime beginDate, DateTime endDate,
            List<Room> roomsToReserve)
        {
            int persons_counter = 0;
            int numOfNights = (endDate - beginDate).Days;
            double price;
            if (searched_rooms.Count > 0)
            {
                price = searched_rooms[0].Hotel.PriceForNightForPerson;
                if (breakfastChecked) price += searched_rooms[0].Hotel.BreakfastPrice;
            }
            else
            {
                price = 0.0;
            }
            foreach (var room in searched_rooms)
            {
                Console.WriteLine(
                    $"\n\nappartmentsAmountToFind: {appartmentsAmountToFind},\n" +
                    $"casualRoomAmountToFind: {casualRoomAmountToFind},\n" +
                    $"room.Id: {room.Id},\n\n"
                );
                Boolean able_to_reserve = false;
                int persons_to_add = 0;
                if (room.Type.Equals("appartment") && appartmentsAmountToFind > 0)
                {
                    able_to_reserve = true;
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
                        persons_counter += 4;
                        roomsToReserve.Add(room);
                    }
                }
                if (room.Type.Equals("2 person") && casualRoomAmountToFind > 0)
                {
                    able_to_reserve = true;
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
                        persons_counter += 4;
                        roomsToReserve.Add(room);
                    }
                }
                if (appartmentsAmountToFind <= 0 && casualRoomAmountToFind <= 0)
                {
                    return price * persons_counter * numOfNights;
                }
            }
            return -1.0;
        }
    }
}
