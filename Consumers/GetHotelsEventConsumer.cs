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
                var searched_rooms = hotelContext.Rooms.Include(b => b.Reservations).Where(b => b.HotelId == hotel.Id).ToList();
                var appartmentsAmountToFind = taskContext.Message.AppartmentsAmount;
                var casualRoomAmountToFind = taskContext.Message.CasualRoomAmount;
                foreach (var room in searched_rooms)
                {
                    Console.WriteLine(
                        $"\n\nappartmentsAmountToFind: {appartmentsAmountToFind},\n" +
                        $"casualRoomAmountToFind: {casualRoomAmountToFind},\n" +
                        $"room.Id: {room.Id},\n\n"
                    );
                    if (room.Type.Equals("appartment") && appartmentsAmountToFind > 0)
                    {
                        Boolean able_to_reserve = true;
                        foreach (var reservation in room.Reservations)
                        {
                            if (DateTime.Compare(reservation.EndDate, taskContext.Message.BeginDate) > 0 && 
                                DateTime.Compare(reservation.BeginDate, taskContext.Message.EndDate) < 0)
                            {
                                able_to_reserve = false;
                                break;
                            }
                        }
                        if (able_to_reserve)
                        {
                            appartmentsAmountToFind--;
                        }
                    }
                    if (room.Type.Equals("2 person") && casualRoomAmountToFind > 0)
                    {
                        Boolean able_to_reserve = true;
                        foreach (var reservation in room.Reservations)
                        {
                            if (DateTime.Compare(reservation.EndDate, taskContext.Message.BeginDate) > 0 &&
                                DateTime.Compare(reservation.BeginDate, taskContext.Message.EndDate) < 0)
                            {
                                able_to_reserve = false;
                                break;
                            }
                        }
                        if (able_to_reserve)
                        {
                            casualRoomAmountToFind--;
                        }
                    }
                    if (appartmentsAmountToFind <= 0 && casualRoomAmountToFind <= 0)
                    {
                        hotel_items.Add(new HotelItem(hotel.Id, hotel.Name));
                        break;
                    }
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
