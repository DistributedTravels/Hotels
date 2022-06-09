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
            if (taskContext.Message.AppartmentsAmountToDelete == 0 && taskContext.Message.CasualRoomAmountToDelete == 0)
            {
                Console.WriteLine(
                    $"\n\nnot deleted\n" +
                    $"amount unchanged\n\n"
                );
                return;
            }
            var searched_rooms = hotelContext.Rooms
                .Include(b => b.Hotel)
                .Include(b => b.Reservations)
                .Where(b => b.Hotel.Id == taskContext.Message.HotelId)
                .Where(b => !b.Hotel.Removed)
                .Where(b => !b.Removed).ToList();
            if (!searched_rooms.Any())
            {
                Console.WriteLine(
                    $"\n\nHotel or rooms already removed\n\n"
                );
                return;
            }
            var searched_hotel = searched_rooms.First().Hotel;
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
                            HotelId = searched_hotel.Id,
                            HotelName = searched_hotel.Name,
                            ChangeInHotelPrice = user.CalculatedCost,
                            WifiAvailable = searched_hotel.HasWifi,
                            BreakfastAvailable = (searched_hotel.BreakfastPrice >= 0.0 ? true : false),
                            HotelAvailable = true,
                            BigRoomNumberChange = user.AppartmentsAmount,
                            SmallRoomNumberChange = user.CasualRoomsAmount
                        },
                        ChangesInTransport = new TransportChange { TransportId = -1 },
                        ReservationAvailable = false
                    });
            }
            var room_numbers = AdditionalFunctions.calculate_rooms_count(
                searched_rooms, taskContext.Message.CreationDate);
            await taskContext.RespondAsync<ChangesInOffersEvent>(
                new ChangesInOffersEvent
                {
                    HotelId = searched_hotel.Id,
                    HotelName = searched_hotel.Name,
                    BigRoomsAvailable = room_numbers.apartment_count,
                    SmallRoomsAvaialable = room_numbers.casual_room_count,
                    WifiAvailable = searched_hotel.HasWifi,
                    BreakfastAvailable = (searched_hotel.BreakfastPrice >= 0.0 ? true : false),
                    HotelPricePerPerson = searched_hotel.PriceForNightForPerson,
                    TransportId = -1,
                    TransportPricePerSeat = -1.0,
                    PlaneAvailable = false,
                    BreakfastPrice = searched_hotel.BreakfastPrice
                });
        }
    }
}
