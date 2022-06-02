using MassTransit;
using Models.Hotels;
using Hotels.Database;
using Hotels.Database.Tables;
using Models.Hotels.Dto;

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
            if (taskContext.Message.HotelName.Equals("any"))
            {
                Console.WriteLine(
                    $"\n\nnot changed\n" +
                    $"can't be \"any\" in hotel name field\n\n"
                );
                await taskContext.RespondAsync<ChangeBasePriceEventReply>(
                    new ChangeBasePriceEventReply(ChangeBasePriceEventReply.State.NOT_CHANGED,
                    new List<ResponseListDto>(), taskContext.Message.CorrelationId));
                return;
            }
            if (taskContext.Message.NewPrice <= 0.0)
            {
                Console.WriteLine(
                    $"\n\nnot changed\n" +
                    $"can't be value below 0 as new price\n\n"
                );
                await taskContext.RespondAsync<ChangeBasePriceEventReply>(
                    new ChangeBasePriceEventReply(ChangeBasePriceEventReply.State.NOT_CHANGED,
                    new List<ResponseListDto>(), taskContext.Message.CorrelationId));
                return;
            }
            var searched_rooms_query = hotelContext.Rooms
                .Include(b => b.Hotel)
                .Include(b => b.Reservations)
                .Where(b => b.Hotel.Name == taskContext.Message.HotelName)
                .Where(b => !b.Hotel.Removed)
                .Where(b => !b.Removed);
            if (!searched_rooms_query.ToList().Any())
            {
                Console.WriteLine(
                    $"\n\nHotel is already removed or rooms uncorrect\n\n"
                );
                await taskContext.RespondAsync<ChangeBasePriceEventReply>(
                    new ChangeBasePriceEventReply(ChangeBasePriceEventReply.State.NOT_CHANGED,
                    new List<ResponseListDto>(), taskContext.Message.CorrelationId));
                return;
            }
            var price_difference = taskContext.Message.NewPrice - searched_rooms_query.First().Hotel.PriceForNightForPerson;
            var current_date = DateTime.Now.ToUniversalTime();
            HashSet<ResponseListDto> users_set = new HashSet<ResponseListDto>(new ResponseListDtoComparer());
            foreach (var searched_room in searched_rooms_query.ToList())
            {
                foreach (var reservation in searched_room.Reservations)
                {
                    if (DateTime.Compare(reservation.BeginDate, current_date) > 0)
                    {
                        var total_price_difference = price_difference * reservation.NightsNumber * reservation.PersonsNumber;
                        users_set.Add(new ResponseListDto
                        {
                            ReservationNumber = reservation.ReservationNumber,
                            UserId = reservation.UserId,
                            CalculatedCost = total_price_difference
                        });
                        reservation.CalculatedCost += total_price_difference;
                    }
                }
            }
            searched_rooms_query.First().Hotel.PriceForNightForPerson = taskContext.Message.NewPrice;
            hotelContext.SaveChanges();
            Console.WriteLine("Users list:");
            foreach (var user in users_set.ToList())
            {
                Console.WriteLine($"{user.ReservationNumber} {user.UserId} {user.CalculatedCost}");
            }
            await taskContext.RespondAsync<ChangeBasePriceEventReply>(
                new ChangeBasePriceEventReply(ChangeBasePriceEventReply.State.CHANGED,
                users_set.ToList(), taskContext.Message.CorrelationId));
        }
    }
}
