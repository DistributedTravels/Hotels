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
            List<Hotel> searched_hotels;
            if (taskContext.Message.Country.Equals("any"))
            {
                searched_hotels = hotelContext.Hotels.ToList();
            }
            else
            {
                searched_hotels = hotelContext.Hotels
                .Where(b => b.Country.Equals(taskContext.Message.Country))
                .ToList();
            }            
            List<HotelItem> hotel_items = new List<HotelItem>();
            foreach (var hotel in searched_hotels)
            {
                hotel_items.Add(new HotelItem(hotel.Id, hotel.Name, hotel.Country));
            }

            Console.WriteLine("Hotels list:");
            foreach (var hotel in hotel_items)
            {
                Console.WriteLine($"{ hotel.HotelItemId} { hotel.HotelName} { hotel.HotelCountry }");
            }
            await taskContext.RespondAsync<GetHotelsEventReply>(new GetHotelsEventReply(hotel_items, taskContext.Message.CorrelationId) {  HotelItems = hotel_items, CorrelationId = taskContext.Message.CorrelationId});
        }
    }
}
