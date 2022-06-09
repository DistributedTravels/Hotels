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
    public class ChangeBasePriceEventConsumer : IConsumer<ChangeBasePriceEvent>
    {
        private readonly HotelContext hotelContext;
        public ChangeBasePriceEventConsumer(HotelContext hotelContext)
        {
            this.hotelContext = hotelContext;
        }

        public async Task Consume(ConsumeContext<ChangeBasePriceEvent> taskContext)
        {
            if (taskContext.Message.NewPrice <= 0.0)
            {
                Console.WriteLine(
                    $"\n\nnot changed\n" +
                    $"can't be value below 0 as new price\n\n"
                );
                return;
            }
            var searched_rooms_query = hotelContext.Rooms
                .Include(b => b.Hotel)
                .Include(b => b.Reservations)
                .Where(b => b.Hotel.Id == taskContext.Message.HotelId)
                .Where(b => !b.Hotel.Removed)
                .Where(b => !b.Removed);
            Hotel searched_hotel;
            if (!searched_rooms_query.ToList().Any())
            {
                var searched_hotels = hotelContext.Hotels
                    .Where(b => b.Id == taskContext.Message.HotelId)
                    .Where(b => !b.Removed);
                if (!searched_hotels.Any())
                {
                    Console.WriteLine(
                        $"\n\nHotel is already removed or rooms uncorrect\n\n"
                    );
                    return;
                }
                searched_hotel = searched_hotels.First();
            }
            else
            {
                searched_hotel = searched_rooms_query.First().Hotel;
            }

            if (searched_hotel.PriceForNightForPerson == taskContext.Message.NewPrice)
            {
                Console.WriteLine(
                        $"\n\nPrice is not changed\n\n"
                    );
                return;
            }

            var price_difference = taskContext.Message.NewPrice - searched_hotel.PriceForNightForPerson;
            var current_date = taskContext.Message.CreationDate;
            HashSet<ResponseListDto> users_set = new HashSet<ResponseListDto>(new ResponseListDtoComparer());
            foreach (var searched_room in searched_rooms_query.ToList())
            {
                foreach (var reservation in searched_room.Reservations)
                {
                    if (DateTime.Compare(reservation.BeginDate, current_date) > 0)
                    {
                        var total_price_difference = price_difference * reservation.NightsNumber * 
                            (reservation.AppartmentsNumber * AdditionalFunctions.persons_in_appartment + 
                            reservation.CasualRoomsNumber * AdditionalFunctions.persons_in_casual_room);
                        reservation.CalculatedCost += total_price_difference;
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
            searched_hotel.PriceForNightForPerson = taskContext.Message.NewPrice;
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
            var room_numbers = AdditionalFunctions.calculate_rooms_count(
                searched_rooms_query.ToList(), taskContext.Message.CreationDate);
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
