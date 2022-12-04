# ValheimSeedFinder
This tool scans hundreds of seeds every second for the perfect spot according to your preferences.

# Download
Here:

# Usage

*Search Resolution*
How many points is analyzed for each seed. The larger this value is, the more precise your search results will be. So if you want to search for a very small area, you'll likely want to increase the resolution. But the higher the resolution is, the slower the it will search.

*CPU Usage*
You can drag this all the way down to 0, to pause scanning.

*Size / Inv zoom*
The size of the area win world units you're searching for. (A world map is 20.000 x 20.000 units)

*Center Biome*
The algorithm works by isolating all contours / blobs of your "center biome", it then analyzes a rectangle around each center at the size you've specified, and calculates the biome weights.

*Weights*
Left toggle:
Toggle on a specific biome you want included, and adjust the weight.

Right toggle:
By enabling the right toggle, you tell the algorithm only to analyze contours / biom blobs if the biom has the specified biom as a direct neighbour.

*Normalize*
If you don't want "unkown" bioms, then idealy all weights have to sum up to one. The normalize button will do that for you.

# Roadmap

* Improve water levels.
* Improve Valheim version compatibility support.
* Maybe one day I'll move this out of Unity, into a normal C# project with SDL + Dear ImGUI or something. I don't want to do UI in Unity.
* Look into filtering by assets that must be present, such as make sure there is a Maypole, or minimum x number of Copper diposits.

