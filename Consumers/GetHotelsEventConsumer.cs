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
            Console.WriteLine($"Received message with this Country:" +
                $" {taskContext.Message.Country} and this Attraction:" +
                $" {taskContext.Message.Attraction}");
            var searched_hotels = hotelContext.AttractionInHotel.Include(b => b.Hotel)
                    .Include(b => b.Hotel.Country).Include(b => b.Attraction)
                    .Where(b => b.Attraction.Name.Equals(taskContext.Message.Attraction))
                    .Where(b => b.Hotel.Country.Name.Equals(taskContext.Message.Country))
                    .Select(b => new HotelItem(b.Hotel.Id, b.Hotel.Name))
                    .ToList();

            Console.WriteLine("Hotels list:");
            foreach (var hotel in searched_hotels)
            {
                Console.WriteLine($"{ hotel.HotelItemId} { hotel.HotelName}");
            }
            await taskContext.Publish<GetHotelsEventReply>(new GetHotelsEventReply(searched_hotels));
        }
    }
}
