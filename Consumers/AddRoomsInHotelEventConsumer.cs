﻿using MassTransit;
using Models.Hotels;
using Hotels.Database;
using Hotels.Database.Tables;

using Microsoft.EntityFrameworkCore;

namespace Hotels.Consumers
{
    public class AddRoomsInHotelEventConsumer : IConsumer<AddRoomsInHotelEvent>
    {
        private readonly HotelContext hotelContext;
        public AddRoomsInHotelEventConsumer(HotelContext hotelContext)
        {
            this.hotelContext = hotelContext;
        }

        public async Task Consume(ConsumeContext<AddRoomsInHotelEvent> taskContext)
        {
            var searched_hotels = hotelContext.Hotels
                .Include(b => b.Rooms)
                .Where(b => !b.Removed)
                .Where(b => b.Id == taskContext.Message.HotelId).ToList();
            if (!searched_hotels.Any())
            {
                Console.WriteLine(
                    $"\n\nnot added\n" +
                    $"hotel with this name does not exist\n\n"
                );
                return;
            }
            var searched_hotel = searched_hotels[0];
            if (searched_hotel.Rooms == null)
            {
                searched_hotel.Rooms = new List<Room>();
            }
            for (int i = 0; i < taskContext.Message.AppartmentsAmountToAdd; i++)
            {
                searched_hotel.Rooms.Add(new Room
                {
                    Type = "appartment",
                    Reservations = new List<Reservation> { }
                });
            }
            for (int i = 0; i < taskContext.Message.CasualRoomAmountToAdd; i++)
            {
                searched_hotel.Rooms.Add(new Room
                {
                    Type = "2 person",
                    Reservations = new List<Reservation> { }
                });
            }
            await hotelContext.SaveChangesAsync();
            Console.WriteLine($"\n\nadded\n\n");
        }
    }
}
