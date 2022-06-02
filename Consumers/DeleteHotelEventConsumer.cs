using MassTransit;
using Models.Hotels;
using Hotels.Database;
using Hotels.Database.Tables;
using Models.Hotels.Dto;

using Microsoft.EntityFrameworkCore;

namespace Hotels.Consumers
{
    public class DeleteHotelEventConsumer : IConsumer<DeleteHotelEvent>
    {
        private readonly HotelContext hotelContext;
        public DeleteHotelEventConsumer(HotelContext hotelContext)
        {
            this.hotelContext = hotelContext;
        }

        public async Task Consume(ConsumeContext<DeleteHotelEvent> taskContext)
        {
            if (taskContext.Message.Name.Equals("any"))
            {
                Console.WriteLine(
                    $"\n\nnot deleted\n" +
                    $"can't be \"any\" in name field\n\n"
                );
                await taskContext.RespondAsync<DeleteHotelEventReply>(
                    new DeleteHotelEventReply(DeleteHotelEventReply.State.NOT_DELETED, 
                    new List<ResponseListDto>(), taskContext.Message.CorrelationId));
                return;
            }
            
            var searched_rooms_query = hotelContext.Rooms
                .Include(b => b.Hotel)
                .Include(b => b.Reservations)
                .Where(b => b.Hotel.Name == taskContext.Message.Name)
                .Where(b => !b.Hotel.Removed);
            if (!searched_rooms_query.ToList().Any())
            {
                Console.WriteLine(
                    $"\n\nHotel is already removed\n\n"
                );
                await taskContext.RespondAsync<DeleteHotelEventReply>(
                    new DeleteHotelEventReply(DeleteHotelEventReply.State.NOT_DELETED,
                    new List<ResponseListDto>(), taskContext.Message.CorrelationId));
                return;
            }

            searched_rooms_query.First().Hotel.Removed = true;
            searched_rooms_query = searched_rooms_query.Where(b => !b.Removed);
            var current_date = DateTime.Now.ToUniversalTime();
            HashSet<ResponseListDto> users_set = new HashSet<ResponseListDto>(new ResponseListDtoComparer());
            AdditionalFunctions.check_rooms_as_deleted(searched_rooms_query.ToList(), users_set, current_date);
            hotelContext.SaveChanges();
            Console.WriteLine("Users list:");
            foreach (var user in users_set.ToList())
            {
                Console.WriteLine($"{user.ReservationNumber} {user.UserId} {user.CalculatedCost}");
            }
            await taskContext.RespondAsync<DeleteHotelEventReply>(
                    new DeleteHotelEventReply(DeleteHotelEventReply.State.DELETED,
                    users_set.ToList(), taskContext.Message.CorrelationId));
        }
    }
}
