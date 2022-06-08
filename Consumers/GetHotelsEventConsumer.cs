using MassTransit;
using Models.Hotels;
using Hotels.Database;
using Hotels.Database.Tables;

using Microsoft.EntityFrameworkCore;

namespace Hotels.Consumers
{
    public class GetHotelsEventConsumer : IConsumer<GetHotelsEvent>
    {
        private readonly HotelContext hotelContext;
        public GetHotelsEventConsumer(HotelContext hotelContext)
        {
            this.hotelContext = hotelContext;
        }
        public async Task Consume(ConsumeContext<GetHotelsEvent> taskContext)
        {
            Console.WriteLine(
                $"\n\nReceived message:\n" +
                $"Country: {taskContext.Message.Country}\n\n"
            );
            
            var hotels_raw = hotelContext.Hotels.Include(b => b.Rooms).Where(b => !b.Removed);
            if (!taskContext.Message.Country.Equals("any"))
            {
                hotels_raw = hotels_raw.Where(b => b.Country.Equals(taskContext.Message.Country));
            }
            List<Hotel> searched_hotels = hotels_raw.ToList();
            List<HotelItem> hotel_items = new List<HotelItem>();
            foreach (var hotel in searched_hotels)
            {
                var rooms = hotelContext.Rooms
                .Include(b => b.Hotel)
                .Include(b => b.Reservations)
                .Where(b => b.Hotel.Name.Equals(hotel.Name))
                .Where(b => !b.Hotel.Removed);
                var all_apartments = rooms.Where(b => b.Type.Equals("appartment"));
                var all_casual_rooms = rooms.Where(b => b.Type.Equals("2 person"));
                var apartment_count = all_apartments.Count();
                var casual_room_count = all_casual_rooms.Count();
                foreach (var room in all_apartments)
                {
                    foreach (var reservation in room.Reservations)
                    {
                        if (DateTime.Compare(reservation.EndDate, taskContext.Message.BeginDate) > 0 &&
                            DateTime.Compare(reservation.BeginDate, taskContext.Message.EndDate) < 0)
                        {
                            apartment_count--;
                        }
                    }
                }
                foreach (var room in all_casual_rooms)
                {
                    foreach (var reservation in room.Reservations)
                    {
                        if (DateTime.Compare(reservation.EndDate, taskContext.Message.BeginDate) > 0 &&
                            DateTime.Compare(reservation.BeginDate, taskContext.Message.EndDate) < 0)
                        {
                            casual_room_count--;
                        }
                    }
                }

                hotel_items.Add(new HotelItem(hotel.Id, hotel.Name, hotel.Country, hotel.BreakfastPrice,
                    hotel.HasWifi, hotel.PriceForNightForPerson, apartment_count, casual_room_count));
            }

            Console.WriteLine("Hotels list:");
            foreach (var hotel in hotel_items)
            {
                Console.WriteLine($"{ hotel.HotelItemId} { hotel.HotelName} { hotel.HotelCountry }\n" +
                    $"{hotel.HotelBreakfastPrice} {hotel.HotelHasWifi} {hotel.HotelPriceForNightForPerson}\n" +
                    $"{hotel.ApartmentsAmount} {hotel.CasualRoomsAmount}");
            }
            await taskContext.RespondAsync<GetHotelsEventReply>(
                new GetHotelsEventReply(hotel_items, taskContext.Message.CorrelationId) {
                    HotelItems = hotel_items, CorrelationId = taskContext.Message.CorrelationId});
        }
    }
}
