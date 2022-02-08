# TOK ELF Tool
A tool that allows modding Paper Mario The Origami King easily by editing .elf files and compressing them with ZSTD.

![Screenshot of modified NPC in Paper Mario TOK](https://i.imgur.com/AsZlzvh.png)\
This is a modified room in Paper Mario TOK entirely done using this tool.

# Download
~~https://github.com/Darxoon/TOKElfTool/releases/~~

~~You can download the latest release from there. I always make sure that it's up to date. Note that it's still 
in development. Features are missing, stuff can break and field names are incomplete.~~

### The Releases page has long been outdated, instead, download it from below:

Development builds:  
https://ci.appveyor.com/project/Darxoon/tokelftool/build/artifacts

Here, you can download the lastest development build. Note that it might be really unstable and some things
might not work at all.

## Requirements
This program is Windows-only and requires .NET Framework 4.7.2. If there's enough demand, I can make it cross-platform. 
However, you should be able to run it with Wine with extra steps.

## Building on Windows
To build it on Windows, you need Visual Studio (I use VS 2019, older versions may not work). Clone the
repository using git and open the solution. There, you can select Release as your target and compile it.

![Screenshot of how to compile](https://i.imgur.com/LL3ZmAQ.png)

## Building on Unix-based Systems
Building on Unix-based Systems probably doesn't work. I couldn't find any information about it 
but I don't think you can build .NET Framework Apps on systems other than Windows.

Still, if you want to try it, you need MSBuild and NuGet (you can download them 
from https://dotnet.microsoft.com/download). However, they will probably not be able
to compile a .NET Framework Application.

## Features
This program can:
 * Load and save .elf files from Paper Mario TOK
 * Compress and decompress ZSTD files
 * Change placements of NPC's, blocks, objects and more in Paper Mario TOK
 * Add new content to Paper Mario TOK

Features that will be coming:
 * Battle Editing
 * More supported file types
 * Collision editing at some point?

## Contact
To report bugs or give suggestions, you can open an Issue here on GitHub. You can also contact me privately on Discord (my tag is Darxoon#2884).

You can also visit the Paper Mario Modding discord (https://discord.gg/Pj4u7wB) and talk about it there (in the #tok-modding channel). 
