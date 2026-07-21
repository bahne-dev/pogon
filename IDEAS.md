- allow for "greasing" a chiseled block to prevent it from becoming part
of the assembly? (so you can have pistons in cylinders within 1x1x1?)
- map an selection somewhere and do a voxel level simple bfs on that? kinda
like virtual memory?
- what if u can directly move the player cuboid in the physics realm instead
of intercepting movement ticks and then syncing

- you project the player into the physics realm and then take the allowed 
movement and stuff from there to update the player based on physics shit

- maybe have Apparatus and Mechanism as separate things, so apparatus
would be like a whole thing with a bounding box and environment interaction,
while a mechanism doesnt have a boudning box and is just shit like conrods
and pistons and so on

- TODO: figure out whether sending render data to every client every tick is cheaper
than having every client hold a non ticking physics sim that is updated by
the server using diffs
