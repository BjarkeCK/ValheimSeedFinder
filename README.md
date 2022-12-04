# Valheim Seed Finder
A tool that scans hundreds of seeds every second for the perfect spot according to your preferences.

![ValheimSeedFinder](https://github.com/BjarkeCK/ValheimSeedFinder/blob/main/Src/Images/ValheimSeedFinder.gif?raw=true)

# Download
For Windows: [https://github.com/BjarkeCK/ValheimSeedFinder/releases](https://github.com/BjarkeCK/ValheimSeedFinder/releases)

# Usage

### Search Resolution
How many points is analyzed for each seed. The larger this value is, the more precise your search results will be. So if you want to search for a very small area, you'll likely want to increase the resolution. But the higher the resolution is, the slower it will be.

### CPU Usage
You can drag this all the way down to 0, to pause scanning.

### Size / Inv zoom
The size of the area in (world units) you're searching for. The valheim world map is 20.000 x 20.000 units

### Center Biome
The algorithm works by isolating all contours / blobs of your "center biome", it then analyzes a rectangle around each center at the size you've specified, and calculates the biome weights.

### Weights
**Left toggle:** Toggle on a specific biome you want included, and adjust the weight.

**Right toggle:** By enabling the right toggle, you tell the algorithm only to analyze contours / biome blobs if the biome has the specified biome as a direct neighbour.

### Normalize
If you don't want "unkown" bioms, then idealy all weights have to sum up to one. The normalize button will do that for you.

### The Results
The colors below each results represents the weights of each biome in that area. You can click on each results to open up the area in valheim-map.world

# Roadmap
* Improve water levels.
* Improve Valheim version compatibility support.
* Maybe one day I'll move this out of Unity, into a normal C# project with SDL + Dear ImGUI or something. I don't want to do UI in Unity.
* Look into filtering by assets that must be present, such as make sure there is a Maypole, or minimum x number of Copper nearby.

# Contribute
* Yes please! I've used Unity version 2021, but it should be compatible with pretty much any version of Unity out there. It's a single script file, you can also  just grab it and drag it into your own project and attach the ValheimSeedFinder component to a gameobject and hit play.
