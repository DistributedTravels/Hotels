using MassTransit;
using Models.Hotels;
using Hotels.Database;
using Hotels.Database.Tables;
using Models.Hotels.Dto;
using Models.Reservations;

using Microsoft.EntityFrameworkCore;

namespace Hotels.Consumers
{
    public class DeleteRoomsInHotelEventConsumer : IConsumer<DeleteRoomsInHotelEvent>
    {
        private readonly HotelContext hotelContext;
        public DeleteRoomsInHotelEventConsumer(HotelContext hotelContext)
        {
            this.hotelContext = hotelContext;
        }

        public async Task Consume(ConsumeContext<DeleteRoomsInHotelEvent> taskContext)
        {
            if (taskContext.Message.HotelName.Equals("any"))
            {
                Console.WriteLine(
                    $"\n\nnot deleted\n" +
                    $"can't be \"any\" in hotel name field\n\n"
                );
                return;
            }
            var searched_rooms_query = hotelContext.Rooms
                .Include(b => b.Hotel)
                .Include(b => b.Reservations)
                .Where(b => b.Hotel.Name.Equals(taskContext.Message.HotelName))
                .Where(b => !b.Hotel.Removed);
            if (!searched_rooms_query.ToList().Any())
            {
                Console.WriteLine(
                    $"\n\nHotel is already removed\n\n"
                );
                return;
            }
            var searched_rooms = searched_rooms_query.Where(b => !b.Removed);
            var searched_appartments = searched_rooms.Where(b => b.Type.Equals("appartment")).OrderBy(o => o.Id).Reverse().Take(taskContext.Message.AppartmentsAmountToDelete).ToList();
            var searched_casual_rooms = searched_rooms.Where(b => b.Type.Equals("2 person")).OrderBy(o => o.Id).Reverse().Take(taskContext.Message.CasualRoomAmountToDelete).ToList();
            if(searched_appartments.Count<taskContext.Message.AppartmentsAmountToDelete)
            {
                Console.WriteLine(
                    $"\n\nnot deleted\n" +
                    $"too many appartments to remove\n\n"
                );
                return;
            }
            if (searched_casual_rooms.Count < taskContext.Message.CasualRoomAmountToDelete)
            {
                Console.WriteLine(
                    $"\n\nnot deleted\n" +
                    $"too many casual rooms to remove\n\n"
                );
                return;
            }
            var current_date = DateTime.Now.ToUniversalTime();
            HashSet<ResponseListDto> users_set = new HashSet<ResponseListDto>(new ResponseListDtoComparer());
            AdditionalFunctions.check_rooms_as_deleted(searched_appartments, users_set, current_date);
            AdditionalFunctions.check_rooms_as_deleted(searched_casual_rooms, users_set, current_date);
            hotelContext.SaveChanges();
            Console.WriteLine("Users list:");
            foreach (var user in users_set.ToList())
            {
                Console.WriteLine($"{user.ReservationNumber} {user.UserId} {user.CalculatedCost}");
                await taskContext.RespondAsync<ChangesInReservationsEvent>(
                    new ChangesInReservationsEvent
                    {
                        ReservationId = user.ReservationNumber,
                        ChangesInHotel = new HotelChange
                        {
                            HotelId = searched_rooms_query.First().Hotel.Id,
                            HotelName = searched_rooms_query.First().Hotel.Name,
                            ChangeInHotelPrice = user.CalculatedCost,
                            WifiAvailable = searched_rooms_query.First().Hotel.HasWifi,
                            BreakfastAvailable = (searched_rooms_query.First().Hotel.BreakfastPrice >= 0.0 ? true : false),
                            HotelAvailable = true,
                            BigRoomNumberChange = user.AppartmentsAmount,
                            SmallRoomNumberChange = user.CasualRoomsAmount
                        },
                        ChangesInTransport = new TransportChange { TransportId = -1 },
                        ReservationAvailable = false
                    });
            }
        }
    }
}
