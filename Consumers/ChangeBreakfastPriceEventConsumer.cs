using MassTransit;
using Models.Hotels;
using Hotels.Database;
using Hotels.Database.Tables;
using Models.Hotels.Dto;

using Microsoft.EntityFrameworkCore;

namespace Hotels.Consumers
{
    public class ChangeBreakfastPriceEventConsumer : IConsumer<ChangeBreakfastPriceEvent>
    {
        private readonly HotelContext hotelContext;
        public ChangeBreakfastPriceEventConsumer(HotelContext hotelContext)
        {
            this.hotelContext = hotelContext;
        }

        public async Task Consume(ConsumeContext<ChangeBreakfastPriceEvent> taskContext)
        {
            if (taskContext.Message.HotelName.Equals("any"))
            {
                Console.WriteLine(
                    $"\n\nnot changed\n" +
                    $"can't be \"any\" in hotel name field\n\n"
                );
                await taskContext.RespondAsync<ChangeBreakfastPriceEventReply>(
                    new ChangeBreakfastPriceEventReply(ChangeBreakfastPriceEventReply.State.PRICE_NOT_CHANGED,
                    new List<ResponseListDto>(), taskContext.Message.CorrelationId));
                return;
            }
            var searched_rooms_query = hotelContext.Rooms
                .Include(b => b.Hotel)
                .Include(b => b.Reservations)
                .Where(b => b.Hotel.Name.Equals(taskContext.Message.HotelName))
                .Where(b => !b.Hotel.Removed)
                .Where(b => !b.Removed);
            Hotel searched_hotel;
            if (!searched_rooms_query.ToList().Any())
            {
                var searched_hotels = hotelContext.Hotels
                    .Where(b => b.Name.Equals(taskContext.Message.HotelName))
                    .Where(b => !b.Removed);
                if (!searched_hotels.Any())
                {
                    Console.WriteLine(
                        $"\n\nHotel is already removed or rooms uncorrect\n\n"
                    );
                    await taskContext.RespondAsync<ChangeBreakfastPriceEventReply>(
                        new ChangeBreakfastPriceEventReply(ChangeBreakfastPriceEventReply.State.PRICE_NOT_CHANGED,
                        new List<ResponseListDto>(), taskContext.Message.CorrelationId));
                    return;
                }
                searched_hotel = searched_hotels.First();
            }
            else
            {
                searched_hotel = searched_rooms_query.First().Hotel;
            }
            if (searched_hotel.BreakfastPrice < 0.0 && taskContext.Message.NewPrice < 0.0)
            {
                Console.WriteLine(
                    $"\n\nnot changed\n" +
                    $"breakfast is still unavailable\n\n"
                );
                await taskContext.RespondAsync<ChangeBreakfastPriceEventReply>(
                    new ChangeBreakfastPriceEventReply(ChangeBreakfastPriceEventReply.State.PRICE_NOT_CHANGED,
                    new List<ResponseListDto>(), taskContext.Message.CorrelationId));
                return;
            }
            if (searched_hotel.BreakfastPrice < 0.0 && taskContext.Message.NewPrice >= 0.0)
            {
                searched_hotel.BreakfastPrice = taskContext.Message.NewPrice;
                hotelContext.SaveChanges();
                Console.WriteLine(
                    $"\n\nPrice of breakfast set\n\n"
                );
                await taskContext.RespondAsync<ChangeBreakfastPriceEventReply>(
                    new ChangeBreakfastPriceEventReply(ChangeBreakfastPriceEventReply.State.BREAKFAST_BECOME_AVAILABLE,
                    new List<ResponseListDto>(), taskContext.Message.CorrelationId));
                return;
            }
            ChangeBreakfastPriceEventReply.State answer;
            if (taskContext.Message.NewPrice >= 0.0)
            {
                answer = ChangeBreakfastPriceEventReply.State.PRICE_CHANGED;
            }
            else
            {
                answer = ChangeBreakfastPriceEventReply.State.BREAKFAST_NO_MORE_AVAILABLE;
            }
            var current_date = DateTime.Now.ToUniversalTime();
            HashSet<ResponseListDto> users_set = new HashSet<ResponseListDto>(new ResponseListDtoComparer());
            foreach (var searched_room in searched_rooms_query.ToList())
            {
                foreach (var reservation in searched_room.Reservations)
                {
                    if (DateTime.Compare(reservation.BeginDate, current_date) > 0 && reservation.BreakfastRequired)
                    {
                        double total_price_difference;
                        if (taskContext.Message.NewPrice>=0.0)
                        {
                            total_price_difference = (taskContext.Message.NewPrice - searched_room.Hotel.BreakfastPrice) * reservation.NightsNumber * reservation.PersonsNumber;
                        }
                        else
                        {
                            total_price_difference = ( - searched_room.Hotel.BreakfastPrice) * reservation.NightsNumber * reservation.PersonsNumber;
                            reservation.BreakfastRequired = false;
                        }
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
            if (taskContext.Message.NewPrice >= 0) { searched_hotel.BreakfastPrice = taskContext.Message.NewPrice; }
            else { searched_hotel.BreakfastPrice = -1.0; }
            hotelContext.SaveChanges();
            Console.WriteLine("Users list:");
            foreach (var user in users_set.ToList())
            {
                Console.WriteLine($"{user.ReservationNumber} {user.UserId} {user.CalculatedCost}");
            }
            await taskContext.RespondAsync<ChangeBreakfastPriceEventReply>(
                new ChangeBreakfastPriceEventReply(answer,
                users_set.ToList(), taskContext.Message.CorrelationId));
        }
    }
}
