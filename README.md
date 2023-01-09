# TOK ELF Tool
A tool that allows modding Paper Mario The Origami King easily by editing .elf files and compressing them with ZSTD.

![Screenshot of modified NPC in Paper Mario TOK](https://i.imgur.com/AsZlzvh.png)\
This is a modified room in Paper Mario TOK entirely done using this tool.

# NOTE
TOKElfTool is pretty obsolete by now. It does not know many file types and does not have a good user experience. **I highly suggest using [Origami Wand](https://darxoon.neocities.org/OrigamiWand) instead.**

If you STILL want to use TOKElfTool:

## Download
https://github.com/Darxoon/TOKElfTool/releases/

You can download the latest release from there. As TOKElfTool is discontinued, this will be the final release I put up.

### Development builds:
https://ci.appveyor.com/project/Darxoon/tokelftool/build/artifacts

Here, you can download the lastest development build. Note that because Appveyor deletes any artifacts after a month, there usually is nothing to download there and besides, you should stick with the latest Release from the Releases page anyway.

## Requirements
This program is Windows-only and requires .NET Framework 4.7.2.

## Building on Windows
To build it on Windows, you need Visual Studio (I use VS 2019, older versions may not work). Clone the
repository using git and open the solution. There, you can select Release as your target and compile it.

![Screenshot of how to compile](https://i.imgur.com/LL3ZmAQ.png)

Because TOKElfTool is Windows-only, you are not able to build or use it on UNIX-based systems, only Windows.

## Features
This program can:
 * Load and save .elf files from Paper Mario TOK
 * Compress and decompress ZSTD files
 * Change placements of NPC's, blocks, objects and more in Paper Mario TOK
 * Add new content to Paper Mario TOK

Features that would have been coming:
 * Battle Editing
 * More supported file types  
 => OrigamiWand supports both of these
 * Collision editing at some point?  
 => OrigamiWand will support this in the future

## Contact
To report bugs or give suggestions, you can open an Issue here on GitHub. You can also contact me privately on Discord (my tag is Darxoon#2884).

You can also visit the Paper Mario Modding discord (https://discord.gg/Pj4u7wB) or the Paper Mario TOK Refolded discord (https://discord.gg/y7qfTKyhZy) and talk about it there. 
