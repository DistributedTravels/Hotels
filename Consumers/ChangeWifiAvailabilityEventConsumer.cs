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
    public class ChangeWifiAvailabilityEventConsumer : IConsumer<ChangeWifiAvailabilityEvent>
    {
        private readonly HotelContext hotelContext;
        public ChangeWifiAvailabilityEventConsumer(HotelContext hotelContext)
        {
            this.hotelContext = hotelContext;
        }

        public async Task Consume(ConsumeContext<ChangeWifiAvailabilityEvent> taskContext)
        {
            var searched_rooms_query = hotelContext.Rooms
                .Include(b => b.Hotel)
                .Include(b => b.Reservations)
                .Where(b => b.Hotel.Id == taskContext.Message.HotelId)
                .Where(b => !b.Hotel.Removed);
            Hotel searched_hotel;
            if (!searched_rooms_query.ToList().Any())
            {
                var searched_hotels = hotelContext.Hotels
                    .Where(b => b.Id == taskContext.Message.HotelId)
                    .Where(b => !b.Removed);
                if (!searched_hotels.Any())
                {
                    Console.WriteLine(
                        $"\n\nnot changed\n" +
                        $"Hotel is already removed\n\n"
                    );
                    return;
                }
                searched_hotel = searched_hotels.First();
            }
            else
            {
                searched_hotel = searched_rooms_query.First().Hotel;
            }
            if (!searched_hotel.HasWifi && !taskContext.Message.Wifi || searched_hotel.HasWifi && taskContext.Message.Wifi)
            {
                Console.WriteLine(
                    $"\n\nwifi availability not changed\n\n"
                );
                return;
            }
            var room_numbers = AdditionalFunctions.calculate_rooms_count(
                searched_rooms_query.ToList(), taskContext.Message.CreationDate,
                taskContext.Message.CreationDate.AddDays(1));
            if (!searched_hotel.HasWifi && taskContext.Message.Wifi)
            {
                searched_hotel.HasWifi = taskContext.Message.Wifi;
                hotelContext.SaveChanges();
                Console.WriteLine(
                    $"\n\nwifi set\n\n"
                );
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
                return;
            }
            searched_hotel.HasWifi = taskContext.Message.Wifi;
            searched_rooms_query = searched_rooms_query.Where(b => !b.Removed);
            var current_date = taskContext.Message.CreationDate;
            HashSet<ResponseListDto> users_set = new HashSet<ResponseListDto>(new ResponseListDtoComparer());
            foreach (var searched_room in searched_rooms_query.ToList())
            {
                foreach (var reservation in searched_room.Reservations)
                {
                    if (DateTime.Compare(reservation.BeginDate, current_date) > 0 && reservation.WifiRequired)
                    {
                        users_set.Add(new ResponseListDto
                        {
                            ReservationNumber = reservation.ReservationNumber,
                            UserId = reservation.UserId,
                            CalculatedCost = reservation.CalculatedCost,
                            AppartmentsAmount = reservation.AppartmentsNumber,
                            CasualRoomsAmount = reservation.CasualRoomsNumber
                        });
                        reservation.WifiRequired = false;
                    }
                }
            }
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
                        ReservationAvailable = true
                    });
            }
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
