using MassTransit;
using Models.Hotels;
using Hotels.Database;
using Hotels.Database.Tables;
using Models.Hotels.Dto;

using Microsoft.EntityFrameworkCore;

namespace Hotels.Consumers
{
    public class Returned_values
    {
        public double price { get; set; }
        public int num_of_nights { get; set; }
    }

    public class Available_room_types
    {
        public int apartment_count { get; set; }
        public int casual_room_count { get; set; }
    }

    public class AdditionalFunctions
    {
        public static readonly int persons_in_appartment = 4;
        public static readonly int persons_in_casual_room = 2;
        
        public static Returned_values checkIfRoomsAbleToReserve(List<Room> searched_rooms,
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
                        persons_counter += persons_in_appartment;
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
                        persons_counter += persons_in_casual_room;
                        roomsToReserve.Add(room);
                    }
                }
                if (appartmentsAmountToFind <= 0 && casualRoomAmountToFind <= 0)
                {
                    return new Returned_values
                    {
                        price = price * persons_counter * numOfNights,
                        num_of_nights = numOfNights
                    };
                }
            }
            return new Returned_values
            {
                price = -1.0,
                num_of_nights = 0
            };
        }

        public static void check_rooms_as_deleted(List<Room> searched_rooms, HashSet<ResponseListDto> users_set, DateTime current_date)
        {
            foreach (var searched_room in searched_rooms)
            {
                searched_room.Removed = true;
                foreach (var reservation in searched_room.Reservations)
                {
                    if (DateTime.Compare(reservation.BeginDate, current_date) > 0)
                    {
                        users_set.Add(new ResponseListDto
                        {
                            ReservationNumber = reservation.ReservationNumber,
                            UserId = reservation.UserId,
                            CalculatedCost = reservation.CalculatedCost,
                            AppartmentsAmount = reservation.AppartmentsNumber,
                            CasualRoomsAmount = reservation.CasualRoomsNumber
                        });
                    }
                }
            }
        }

        public static Available_room_types calculate_rooms_count(List<Room> rooms, DateTime beginDate)
        {
            var all_apartments = rooms.Where(b => b.Type.Equals("appartment"));
            var all_casual_rooms = rooms.Where(b => b.Type.Equals("2 person"));
            var apartment_count = all_apartments.Count();
            var casual_room_count = all_casual_rooms.Count();
            foreach (var room in all_apartments)
            {
                foreach (var reservation in room.Reservations)
                {
                    if (DateTime.Compare(beginDate, reservation.EndDate) < 0 &&
                        DateTime.Compare(beginDate, reservation.BeginDate) >= 0)
                    {
                        apartment_count--;
                    }
                }
            }
            foreach (var room in all_casual_rooms)
            {
                foreach (var reservation in room.Reservations)
                {
                    if (DateTime.Compare(beginDate, reservation.EndDate) < 0 &&
                        DateTime.Compare(beginDate, reservation.BeginDate) >= 0)
                    {
                        casual_room_count--;
                    }
                }
            }
            return new Available_room_types
            {
                apartment_count = apartment_count,
                casual_room_count = casual_room_count
            };
        }

        public static Available_room_types calculate_rooms_count(List<Room> rooms, DateTime beginDate, DateTime endDate)
        {
            var all_apartments = rooms.Where(b => b.Type.Equals("appartment"));
            var all_casual_rooms = rooms.Where(b => b.Type.Equals("2 person"));
            var apartment_count = all_apartments.Count();
            var casual_room_count = all_casual_rooms.Count();
            foreach (var room in all_apartments)
            {
                foreach (var reservation in room.Reservations)
                {
                    if (DateTime.Compare(reservation.EndDate, beginDate) > 0 &&
                            DateTime.Compare(reservation.BeginDate, endDate) < 0)
                    {
                        apartment_count--;
                    }
                }
            }
            foreach (var room in all_casual_rooms)
            {
                foreach (var reservation in room.Reservations)
                {
                    if (DateTime.Compare(reservation.EndDate, beginDate) > 0 &&
                            DateTime.Compare(reservation.BeginDate, endDate) < 0)
                    {
                        casual_room_count--;
                    }
                }
            }
            return new Available_room_types
            {
                apartment_count = apartment_count,
                casual_room_count = casual_room_count
            };
        }
    }
}
