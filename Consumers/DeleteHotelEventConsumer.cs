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
    public class DeleteHotelEventConsumer : IConsumer<DeleteHotelEvent>
    {
        private readonly HotelContext hotelContext;
        public DeleteHotelEventConsumer(HotelContext hotelContext)
        {
            this.hotelContext = hotelContext;
        }

        public async Task Consume(ConsumeContext<DeleteHotelEvent> taskContext)
        {
            var searched_rooms_query = hotelContext.Rooms
                .Include(b => b.Hotel)
                .Include(b => b.Reservations)
                .Where(b => b.Hotel.Id == taskContext.Message.HotelId)
                .Where(b => !b.Hotel.Removed);
            if (!searched_rooms_query.ToList().Any())
            {
                var removed_hotels = hotelContext.Hotels.Include(b => b.Rooms).Where(b => b.Id == taskContext.Message.HotelId);
                if (removed_hotels.Any())
                {
                    var removed_hotel = removed_hotels.First();
                    removed_hotel.Removed = true;
                    hotelContext.SaveChanges();
                    Console.WriteLine(
                        $"\n\nHotel removed\n\n"
                    );
                    await taskContext.RespondAsync<ChangesInOffersEvent>(
                        new ChangesInOffersEvent
                        {
                            HotelId = removed_hotel.Id,
                            HotelName = removed_hotel.Name,
                            BigRoomsAvailable = 0,
                            SmallRoomsAvaialable = 0,
                            WifiAvailable = removed_hotel.HasWifi,
                            BreakfastAvailable = (removed_hotel.BreakfastPrice >= 0.0 ? true : false),
                            HotelPricePerPerson = removed_hotel.PriceForNightForPerson,
                            TransportId = -1,
                            TransportPricePerSeat = -1.0,
                            PlaneAvailable = false
                        });
                    return;
                }
                Console.WriteLine($"\n\nHotel is already removed\n\n");
                return;
            }

            searched_rooms_query.First().Hotel.Removed = true;
            searched_rooms_query = searched_rooms_query.Where(b => !b.Removed);
            var current_date = taskContext.Message.CreationDate;
            HashSet<ResponseListDto> users_set = new HashSet<ResponseListDto>(new ResponseListDtoComparer());
            AdditionalFunctions.check_rooms_as_deleted(searched_rooms_query.ToList(), users_set, current_date);
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
                            HotelAvailable = false,
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
