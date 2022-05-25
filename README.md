# Hotels
## populating database
The ```initDB()``` function (invoked at line 45) populate database with sample records if database is empty.

```initDB()``` function adds 90 hotels, which ```Name``` and ```Country``` properties are taken from file ```Init/hotels.json```. The rest of properties, like prices, facilities and rooms' lists are randomly generated.

Definition of this function starts at line 89.

## sample events
(originally) at lines 47 - 85 there are invoked sample events. Uncomment proper events to see their execution.
