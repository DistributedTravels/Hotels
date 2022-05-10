# Hotels
## populating database
uncomment (originally) line 43:
```
//initDB();
```
This function populate database with sample records (a few hotels with a few rooms, in the first hotel there two reservations on the first room).

Definition of this function starts at line 79.

You can add room to the Hotel, by copying any ```new Room``` element, setting your values, and pasting in rooms' list of selected hotel (```Rooms = new List<Room>```)
You can add more hotels by copying all ```hotel = new Hotel {<parameters>}``` section, setting your values and pasting before ```context.SaveChanges();``` (originally at line 232). Also remember to add ```context.Hotels.Add(hotel);```.

## sample events
(originally) at lines 45 -75 there are invoked sample events. Uncomment proper events to see their execution.
