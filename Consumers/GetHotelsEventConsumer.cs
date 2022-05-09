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
                $"Country: {taskContext.Message.Country},\n" +
                $"BeginDate: {taskContext.Message.BeginDate},\n" +
                $"EndDate: {taskContext.Message.EndDate},\n" +
                $"AppartmentsAmount: {taskContext.Message.AppartmentsAmount},\n" +
                $"CasualRoomAmount: {taskContext.Message.CasualRoomAmount},\n" +
                $"Breakfast: {taskContext.Message.Breakfast},\n" +
                $"Internet: {taskContext.Message.Internet}\n\n"
            );

            var searched_hotels = hotelContext.Hotels
                .Where(b => b.Country.Equals(taskContext.Message.Country));
            if (taskContext.Message.Breakfast)
            {
                searched_hotels = searched_hotels.Where(b => b.HasBreakfast == true);
            }
            if (taskContext.Message.Internet)
            {
                searched_hotels = searched_hotels.Where(b => b.HasInternet == true);
            }
            
            List<HotelItem> hotel_items = new List<HotelItem>();
            foreach (var hotel in searched_hotels.ToList())
            {
                Boolean can_be_reserved = AdditionalFunctions.checkIfRoomsAbleToReserve(
                    hotelContext.Rooms.Include(b => b.Reservations).Where(b => b.HotelId == hotel.Id).ToList(),
                    taskContext.Message.AppartmentsAmount,
                    taskContext.Message.CasualRoomAmount,
                    taskContext.Message.BeginDate,
                    taskContext.Message.EndDate);
                if (can_be_reserved)
                {
                    hotel_items.Add(new HotelItem(hotel.Id, hotel.Name));
                }
            }

            Console.WriteLine("Hotels list:");
            foreach (var hotel in hotel_items)
            {
                Console.WriteLine($"{ hotel.HotelItemId} { hotel.HotelName}");
            }
            await taskContext.Publish<GetHotelsEventReply>(new GetHotelsEventReply(hotel_items, taskContext.Message.CorrelationId));
        }
    }
}
