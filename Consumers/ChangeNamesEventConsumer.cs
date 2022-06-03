using MassTransit;
using Models.Hotels;
using Hotels.Database;
using Hotels.Database.Tables;

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
            if (taskContext.Message.NewCountry.Equals("any") || taskContext.Message.NewName.Equals("any") || taskContext.Message.OldName.Equals("any"))
            {
                Console.WriteLine(
                    $"\n\nnot changed\n" +
                    $"can't be \"any\" in name or country fields\n\n"
                );
                await taskContext.RespondAsync<ChangeNamesEventReply>(
                new ChangeNamesEventReply(
                    ChangeNamesEventReply.State.NOT_CHANGED, taskContext.Message.CorrelationId));
                return;
            }
            var searched_hotels = hotelContext.Hotels.Where(b => b.Name.Equals(taskContext.Message.OldName)).Where(b => !b.Removed);
            if (!searched_hotels.Any())
            {
                Console.WriteLine(
                    $"\n\nnot changed\n" +
                    $"can't find hotel\n\n"
                );
                await taskContext.RespondAsync<ChangeNamesEventReply>(
                new ChangeNamesEventReply(
                    ChangeNamesEventReply.State.NOT_CHANGED, taskContext.Message.CorrelationId));
                return;
            }
            var searched_hotel = searched_hotels.First();
            var old_name = searched_hotel.Name;
            var old_country = searched_hotel.Country;
            if (taskContext.Message.ChangedParameter.Equals("name"))
            {
                searched_hotel.Name = taskContext.Message.NewName;
            }
            else if (taskContext.Message.ChangedParameter.Equals("country"))
            {
                searched_hotel.Country = taskContext.Message.NewCountry;
            }
            else if (taskContext.Message.ChangedParameter.Equals("both"))
            {
                searched_hotel.Name = taskContext.Message.NewName;
                searched_hotel.Country = taskContext.Message.NewCountry;
            }
            else
            {
                Console.WriteLine(
                    $"\n\nnot changed\n" +
                    $"wrong ChangedParameter value\n\n"
                );
                await taskContext.RespondAsync<ChangeNamesEventReply>(
                new ChangeNamesEventReply(
                    ChangeNamesEventReply.State.NOT_CHANGED, taskContext.Message.CorrelationId));
                return;
            }
            hotelContext.SaveChanges();
            Console.WriteLine($"\n\nchanged\n\n");
            await taskContext.RespondAsync<ChangeNamesEventReply>(
                new ChangeNamesEventReply(
                    ChangeNamesEventReply.State.CHANGED, searched_hotel.Id, old_name, old_country, searched_hotel.Name,
                    searched_hotel.Country, searched_hotel.BreakfastPrice, searched_hotel.HasWifi,
                    searched_hotel.PriceForNightForPerson, taskContext.Message.CorrelationId));
        }
    }
}
