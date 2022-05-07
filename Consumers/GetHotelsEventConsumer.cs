using MassTransit;
using Models.Hotels;

namespace Hotels.Consumers
{
    public class GetHotelsEventConsumer : IConsumer<GetHotelsEvent>
    {
        public async Task Consume(ConsumeContext<GetHotelsEvent> context)
        {
            Console.WriteLine($"Received message with this Country: {context.Message.Country} and this Attraction: {context.Message.Attraction}");
        }
    }
}
