 _    _ _                       _____ __  __   _______          _ 
| |  | | |               /\    / ____|  \/  | |__   __|        | |
| |  | | |__   ___ _ __ /  \  | (___ | \  / |    | | ___   ___ | |
| |  | | '_ \ / _ \ '__/ /\ \  \___ \| |\/| |    | |/ _ \ / _ \| |
| |__| | |_) |  __/ | / ____ \ ____) | |  | |    | | (_) | (_) | |
 \____/|_.__/ \___|_|/_/    \_\_____/|_|  |_|    |_|\___/ \___/|_|
         VERSION 1.4                BY VITOR VILELA

Thank you for downloading my tool. I hope it helps you though your
SMW hacking journey.

UberASM Tool allows you to easily insert level ASM, overworld ASM,
game mode ASM and much more. It was inspired from GPS, Sprite Tool,
edit1754's levelASM tool, levelASM+NMI version and
33953YoShI's japanese levelASM tool.

At same time UberASM Tool allows easy insertion and distribution of
code, it has a very robust support for complex ASM projects, with
a shared library where you can put your .asm and .bin files without
worrying about freespace and bank overflows.

Features:
 - Level ASM (INIT/MAIN/NMI*/LOAD*)
 - Overworld ASM (INIT/MAIN/NMI*/LOAD*)
 - Game mode ASM (INIT/MAIN/NMI*)
 - Global code ASM (INIT*/MAIN/NMI)
 - Status bar code (MAIN)
 - Shared library with binary support
 - Macro and defines library
 - Automatic multiple banks support
 - Automatic patching and cleaning
 - Native SA-1 support
 - Friendly list with various settings
 - LM Restore System signature
 - Easily editable source code.

* Specific features not present in original uberASM patch.

---------------------------------------------------------------------
-                         Version History                           -
---------------------------------------------------------------------

Version 1.4:
- Updated Asar to 1.71.
- Fixed newer Asar printing weird path-related warnings.
- Fixed a crash when stdin is either redirected or set to empty.
- Fixed a crash when an .asm file prints either an empty or white-space
only string.
- Moved working .asm files to the dedicated ./asm/work instead of 
./asm/.

Version 1.3:
- (randomdude999) Now uses Asar 1.61.

Version 1.2:
- Added sprite defines with SA-1 support.
- Fixed a crash when using both Level NMI and Overworld NMI.
- Fixed some unintended behavior when enabling only Overworld NMI.
- (RPG Hacker) Now uses Asar 1.50.

Version 1.1:
 - Added global_load support.
 - Added more error checks for avoiding free space leaking when user
wrongly uses pullpc/pushpc.
 - Fixed program crashing when running on a Dropbox folder.
 - Fixed minor display/print changes when running program.
 - Fixed minor grammar errors.

Version 1.0:
 - First Public Release

---------------------------------------------------------------------
-                         Getting Started                           -
---------------------------------------------------------------------

Since UberASM Tool relies on Asar and pretty much uses same hijacks,
you can safely apply it on your ROM even if you used uberASM or
levelASM patch previously. Be sure to make a backup of your ROM
before just to you be sure.

Just like a Block or Sprite tool, each level/OW/etc. now have their
own .asm file, making easier to manage each level ASM code. Together
with that, each .asm file (except global and status code) are now
separated from the other files, so things like labels are not shared
anymore with them. Plus, each one now have a separated RATS, so you
don't have to worry at all with bank limitations, since each .asm
file can have their own ROM bank. Due of that, of course, each .asm
file now must end with RTL, instead of RTS.

IMPORTANT: in case of crash while porting, double check your .asm
file in case you missed any RTS, since the code must end with RTL
now.

The INIT and MAIN labels changed too. Since it is a tool, now you
must simply point them as "init:" and "main:". For example, in
your level_init_code.asm, there is this:

levelinit105:
	LDA #$01
	STA $19
	RTS

And in your level_code.asm, there is this:

level105:
	INC $19
	RTS

Merging both codes, changing label and changing RTS to RTL, it will
look like this:

init:
	LDA #$01
	STA $19
	RTL

main:
	INC $19
	RTL

Done! Simply save this file, place on level folder and reference it
on your list file, for example:

level:
105		your file.asm

IMPORTANT: when referencing your file in your list.txt, make sure to
put your file below "level:" line for levels, below "overworld:"
line for OW code and etc.

In case you don't have either init code or main code, you can simply
delete the unused label. For example:

init:
	LDA #$01
	STA $19
	RTL

or

main:
	INC $19
	RTL

Of course at least one label (init, main or nmi) is required to
work.
---------------------------------------------------------------------
-                       Running UberASM Tool                        -
---------------------------------------------------------------------

To run UberASM Tool, change the file name located after "rom:" label,
which is near end of the list. For example:

rom:		Your SMW hack.smc		; ROM file to use.

And after that double click UberASMTool program. The program will
automatically look for "list.txt" and will apply everything you told
to the ROM file you specified inside "list.txt".

Alternatively, you can run UberASM Tool from command line with the
following syntax:

UberASMTool [list file]
or
UberASMTool [list file] [ROM file]


---------------------------------------------------------------------
-                           The List File                           -
---------------------------------------------------------------------

UberASM Tool's list file is pretty different from the other tools you
usually use. In that list you can set your level code, global code,
overworld code, game mode code, free RAM addresses and even your ROM
file!

In a list file you can have the following commands:
 - verbose: <on/off>
   The verbose command basically tells how the program will output
the information on the console. If "on" is used, the program will
put much details like insert size, pointers, progress, statistics,
etc. If "off" is used, the program will only report anything special
if an error occured. Default is off.

 - global: <asm path>
   This defines the path for the global code .asm file location.
Normally you don't have to change this since it's already defined in
the base list file.

 - statusbar: <asm path>
   This defines the path for the status bar .asm file location.
Pretty much same as global code.

 - macrolib: <asm path>
   This defines the path for the macro/define library .asm file
location.

 - sprite: <RAM address>
   This defines what free RAM address will be used to hold the
sprites early execution pointers and last game mode. Usually you
should not worry about this RAM address, as the default value should
work normally. It requires 38 bytes of Free RAM if you're using a
regular ROM or 68 bytes if you're using SA-1 Pack. It requires more
bytes on SA-1 ROMs because SA-1 Pack's sprite table is 22 bytes long
unlike regular SMW which is 12 bytes long. If you plan to use sprites
early execution pointers, it's recommended to use a free BW-RAM
address range instead if you're using SA-1, so sprites can modify the
pointers without invoking SNES CPU.

 - rom: <ROM path>
   This defines what ROM file will be used, relative to the .exe file
location. If you don't specify a ROM file here, then you must run
UberASM Tool though command line and specify one manually.

 - "level:"
   This defines that we're now inserting LevelASM code. So any number
below this label will be considerated as LevelASM code. Example:

level:
105 yoshi_island_1.asm
106 yoshi_island_2.asm

The input is hexadecimal and valid range is 000-1FF. All files must
be on level folder. You can use it subfolders if you want, for example:

level:
105 world 1/level 1.asm
115 world 3/castle.asm

You can also use the same .asm file for multiple levels, allowing you
save more space.

 - "overworld:"
   This defines that we're now inserting OW code. It has the same
properties as "level:" label, except it applies to OW ASM code, uses
overworld subfolder and valid numbers are: 0 = Main map; 1 = Yoshi's
Island; 2 = Vanilla Dome; 3 = Forest of Illusion; 4 = Valley of
Bowser; 5 = Special World; and 6 = Star World.

 - "gamemode:"
   This defines that we're not inserting Game mode code. It has the
same properties as "level:" label, however it uses gamemode subfolder
and valid range is 00-FF.

";" means comment. They won't be processed by UberASM tool. Useful
for putting comments, notes, etc.

---------------------------------------------------------------------
-                        Multiple Bank Support                      -
---------------------------------------------------------------------

UberASM Tool was intentionally designed to every single level code,
global code, overworld code, game mode code, libraries, external
resources, etc., to have their own freespace. This means each code
will be on a different bank, allowing you to you put complex codes
without worrying about bank size limitations. That is, for every
library, code, etc., you can have a insert size up to 0x7FF8 bytes of
space to put whatever you want. As a downside, each struture will
generate a new RATS tag, which is 8 bytes big, slight reducing the
overall free space from the ROM. But that should not be a big issue.

Another thing you have to keep in mind that every code must return
with RTL and not RTS. Data bank is set up automatically, *except*
for NMI code, global code and library calls, since they're called
directly from your code.

---------------------------------------------------------------------
-                         NMI Code Support                          -
---------------------------------------------------------------------

To allow more flexible OW and level ASM advanced design, UberASM Tool
allows coders to run a different NMI code per level, overworld, game
mode and/or global code. To save v-blank time, UberASM Tool checks if
some level, ow, gm or global code actually has nmi label present, and
if so it automatically activates NMI feature support depending on the
demand.

---------------------------------------------------------------------
-                   "Load" Level Code Support                       -
---------------------------------------------------------------------

UberASM Tool also features the "load:" label for levelASM and global
code. That label is trigged before "init:", when the level has not
loaded yet.

It allows you to set up Lunar Magic's Conditional Direct Map16
feature and initialize other flags (ExAnimation triggers, etc.). As
a bonus, it allows you to write map16 data to the level table,
$7E/$7F:C800-FFFF (or $40/$41:C800-FFFF for SA-1 ROMs), making it
possible to add your own level loading blocks.

---------------------------------------------------------------------
-                 External Bank Resource Include                    -
---------------------------------------------------------------------

Sometimes when you're working with big chunks of data, like tilemaps
or graphics, you may want to use it in another bank. UberASM Tool
allows you to easily add external binary code using the macro
"prot_file". You can do the same for .asm files, using "prot_source".

%prot_file(<file to include>, <label name>)
%prot_source(<file to include>, <label name>)

The freespace is automatically set and UberASM Tool cleans your files
automatically, so you won't have to worry about freespace cleaning.

See the macro library to learn more how they work.

---------------------------------------------------------------------
-                   Shared Library Code Support                     -
---------------------------------------------------------------------

UberASM folder has a special folder called "library". You can insert
whatever you want, .asm file or whatever other extension. UberASM
Tool will insert or assemble all files inside that folder to your ROM
and will clean automatically too.

Non-ASM files will have its pointer saved to a label named with its
file name. In other words, if you put "tilemap.bin" on library folder,
you can access it in other level/gamemode/OW/etc. with the "tilemap"
label.

ASM files will have all labels exposed to the other ASM files however
prefixed with the filename. For example, if you put "math.asm" on the
library folder and there's a "sqrt" label inside the ASM file, you
will be able to access the function in other level/gamemode/OW/etc.
with the "math_sqrt" label.

With that, you can save lot of space on your ROM by putting generic
codes and data on the library folder and call them from your level,
OW or game mode code. For example, HDMA codes.

However there is two major problems with using the shared library
currently:

The first one is the included file will be inserted on ROM
regardless if it was used or not. So if you insert tons of libraries
and you never use it, you will be simply wasnting ROM space because
UberASM can't know if the label was actually used or not.

The second one is that you can't call other libraries codes from a
library file. For example, if you have a windowing HDMA code and you
need to call a sqrt routine, which is located on the math library, you
can't do that, because UberASM Tool can't guess what labels each file
will generate nor what labels each library .asm file will depend from
each other. So unfortunately the library files are pretty much
isolated from each one.

---------------------------------------------------------------------
-                         Other Information                         -
---------------------------------------------------------------------

You can use the following labels for LevelASM code:

load:
init:
main:
nmi:

If you don't use some of them, UberASM Tool will by default point
them to a "RTL", making your code slight cleaner.

Data bank is set up automatically for load, init and main. nmi is not
set up automatically to save v-blank time and usually you don't need
it anyway.

For OverworldASM and Game Mode code, the following labels are
available:

init:
main:
nmi:

Data bank is set up automatically for init and main labels.

For Global Code, the following labels are available:

load:
init:
main:
nmi:

However they should return with RTS and not RTL. Data bank is not set
up automatically.

For Status Code, the only label available is "main:". Data bank is
not set up automatically.

When UberASM Tool is executed, a .extmod file is automatically
generated. This file is used by Lunar Magic to know what external
program modified the ROM and is registered on LM Restored System.

UberASM Tool does support unheadered ROMs.

---------------------------------------------------------------------
-                             Credits                               -
---------------------------------------------------------------------

I'd like to thank:
 - edit1754 for the original LevelASM Tool idea;
 - p4plus2 for the original uberASM patch;
 - Alcaro/byuu/Raidenthequick for Asar; and
 - 33953YoShI/Mirann/Wakana for testing.
 - 33953YoShI again for giving me the LOAD label base hijack and idea.

---------------------------------------------------------------------
-                             Contact                               -
---------------------------------------------------------------------

If you need any help regarding this tool, feel free to ask the SMW
Central forums, usually they answer fast enough for your needs.

However if you want to contact me, then feel free to send me a PM to
"Vitor Vilela" on SMW Central (user id = 8251).
