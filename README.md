# Map-script
A script that can draw a new ground/path tile in a tower defense game, that I made for a school project.
What follows here is a short description of the entire script.

Each block consists of 5x5 tiles, they're drawn on a tilemap.
Im using a ton of SetTile lines for this. 
All the blocks have conditions on their borders, which means they will look at the surronding blocks, and find the right tile that matches the layout.

Because the blocks consists of 5x5 tiles, we need to be able to find the center of these. 
Empty GameObjects is placed in the middle of the blocks, and these are the ones I call chunks. 
A list is generated with all chunks on it, which we can loop through, and calculate the distance from mouse to nearest chunk. 

Everytime the player exits the "build mode", the new map will be scanned with A*, to be sure there is a path to the finish line.
Whenever the player places a block while building, the previous block will be saved in a list. 
If the A* scan is not able to find a path, a foreach loop will go through the list, and revert all the blocks back to the previous layout.

When build mode is entered, and a block is selected, it's possible to rotate through the blocks with a keystroke, or just pressing another block.

The script can be tested in the game: https://mild-breeze.itch.io/dreams-and-darkness
An easy way to do this, is press the build "Build Path" button next to the minimap, and placing a ground block on the path, click the button again to exit build mode.
The game is really laggy in the browser, works more smoothly if the game is downloaded.
