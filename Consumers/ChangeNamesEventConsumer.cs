using MassTransit;
using Models.Hotels;
using Hotels.Database;
using Hotels.Database.Tables;
using Models.Reservations;
using Models.Hotels.Dto;
using Models.Offers;

using Microsoft.EntityFrameworkCore;

namespace Hotels.Consumers
{
    public class ChangeNamesEventConsumer : IConsumer<ChangeNamesEvent>
    {
        private readonly HotelContext hotelContext;
        public ChangeNamesEventConsumer(HotelContext hotelContext)
        {
            this.hotelContext = hotelContext;
        }

        public async Task Consume(ConsumeContext<ChangeNamesEvent> taskContext)
        {
            if (taskContext.Message.NewName.Equals("any"))
            {
                Console.WriteLine(
                    $"\n\nnot changed\n" +
                    $"can't be \"any\" in name or country fields\n\n"
                );
                return;
            }
            var searched_hotels = hotelContext.Hotels.Where(b => b.Id == taskContext.Message.HotelId).Where(b => !b.Removed);
            if (!searched_hotels.Any())
            {
                Console.WriteLine(
                    $"\n\nnot changed\n" +
                    $"can't find hotel\n\n"
                );
                return;
            }
            var searched_hotel = searched_hotels.First();
            if (searched_hotel.Name.Equals(taskContext.Message.NewName)) 
            {
                Console.WriteLine(
                    $"\n\nnot changed\n" +
                    $"same name\n\n"
                );
                return;
            }
            searched_hotel.Name = taskContext.Message.NewName;
            hotelContext.SaveChanges();
            Console.WriteLine($"\n\nchanged\n\n");

            var searched_rooms_query = hotelContext.Rooms
                .Include(b => b.Hotel)
                .Include(b => b.Reservations)
                .Where(b => b.Hotel.Name.Equals(searched_hotel.Name))
                .Where(b => !b.Hotel.Removed)
                .Where(b => !b.Removed);
            HashSet<ResponseListDto> users_set = new HashSet<ResponseListDto>(new ResponseListDtoComparer());
            foreach (var searched_room in searched_rooms_query.ToList())
            {
                foreach (var reservation in searched_room.Reservations)
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
                searched_rooms_query.ToList(), taskContext.Message.CreationDate,
                taskContext.Message.CreationDate.AddDays(1));
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
                    PlaneAvailable = false
                });
        }
    }
}
