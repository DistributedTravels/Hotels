using MassTransit;
using Models.Hotels;
using Hotels.Database;
using Hotels.Database.Tables;
using Models.Hotels.Dto;
using Models.Reservations;
using Models.Offers;

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
            var searched_rooms_query = hotelContext.Rooms
                .Include(b => b.Hotel)
                .Include(b => b.Reservations)
                .Where(b => b.Hotel.Id == taskContext.Message.HotelId)
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
            if (taskContext.Message.AppartmentsAmountToDelete == 0 && taskContext.Message.CasualRoomAmountToDelete == 0)
            {
                Console.WriteLine(
                    $"\n\nnot deleted\n" +
                    $"amount unchanged\n\n"
                );
                return;
            }
            var current_date = taskContext.Message.CreationDate;
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
    }
}
