UberASM Tool
============

"UberASM Tool" is a tool that allows to insert common codes without relying to
patches. The main objective is reducing the number of patches and at the same
time allow for inserting ASM file easier and develop new codes quicker compared
to other approaches.

## Building
How to make the ZIP file for uploading a release:

1. Compile the C# source code using Visual Studio Community.
2. Copy UberASMTool.exe and UberASMTool.pdb from UberASMTool/bin/Release to the
assets folder.
3. Copy asar.dll from the UberASMTool folder to the assets folder.
4. Create a ZIP file from all files inside assets.

## Examples

At [examples] there is some ASM files that can be used for testing and/or for
figuring out some specific UberASM Tool features.