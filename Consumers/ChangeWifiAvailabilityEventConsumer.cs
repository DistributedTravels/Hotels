using MassTransit;
using Models.Hotels;
using Hotels.Database;
using Hotels.Database.Tables;
using Models.Hotels.Dto;
using Models.Reservations;

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
            if (taskContext.Message.HotelName.Equals("any"))
            {
                Console.WriteLine(
                    $"\n\nnot changed\n" +
                    $"can't be \"any\" in hotel name field\n\n"
                );
                return;
            }
            var searched_rooms_query = hotelContext.Rooms
                .Include(b => b.Hotel)
                .Include(b => b.Reservations)
                .Where(b => b.Hotel.Name.Equals(taskContext.Message.HotelName))
                .Where(b => !b.Hotel.Removed);
            Hotel searched_hotel;
            if (!searched_rooms_query.ToList().Any())
            {
                var searched_hotels = hotelContext.Hotels
                    .Where(b => b.Name.Equals(taskContext.Message.HotelName))
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
            if (!searched_hotel.HasWifi && taskContext.Message.Wifi)
            {
                searched_hotel.HasWifi = taskContext.Message.Wifi;
                hotelContext.SaveChanges();
                Console.WriteLine(
                    $"\n\nwifi set\n\n"
                );
                return;
            }
            searched_hotel.HasWifi = taskContext.Message.Wifi;
            searched_rooms_query = searched_rooms_query.Where(b => !b.Removed);
            var current_date = DateTime.Now.ToUniversalTime();
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
        }
    }
}
