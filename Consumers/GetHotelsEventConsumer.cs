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
            if (DateTime.Compare(taskContext.Message.BeginDate, taskContext.Message.EndDate) >= 0)
            {
                Console.WriteLine(
                    $"\n\nEnd date must be greater than begin date\n\n"
                );
                return;
            }

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
                var room_numbers = AdditionalFunctions.calculate_rooms_count(rooms.ToList(), taskContext.Message.BeginDate, taskContext.Message.EndDate);

                hotel_items.Add(new HotelItem(hotel.Id, hotel.Name, hotel.Country, hotel.BreakfastPrice,
                    hotel.HasWifi, hotel.PriceForNightForPerson, room_numbers.apartment_count, room_numbers.casual_room_count));
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
