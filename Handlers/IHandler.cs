using Models;
using Microsoft.EntityFrameworkCore;
using Hotels.Database;

namespace Hotels.Handlers
{
    /**
     * <summary>
     * Abstract class to create further handlers for each event type.
     * </summary>
     */
    public abstract class IHandler
    {

        protected Action<EventModel> publish; // to allow for publishing within handler
        protected Action<EventModel, string, string> reply;
        protected readonly string connString; // required for database calls

        public IHandler(Action<EventModel> publish, Action<EventModel, string, string> reply, string connString)
        {
            this.publish = publish;
            this.reply = reply;
            this.connString = connString;
        }

        public virtual DbContextOptions<HotelContext> GetDbOptions()
        {
            return new DbContextOptionsBuilder<HotelContext>()
                .UseNpgsql(connString)
                .LogTo(Console.WriteLine, LogLevel.Information)
                .Options;
        }

        public virtual HotelContext GetDbContext()
        {
            var options = GetDbOptions();
            return new HotelContext(options);
        }

        /**
         * <summary>
         * Method HandleEvent processes data transferred within event message and acts upon it.
         * </summary>
         */
        public abstract Task HandleEvent(string content, string replyTo, string cID);
    }
}
