using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UberASMTool.Model;

namespace UberASMTool
{
    class UberConfig
    {
        public bool VerboseMode { get; set; }
        public string ROMPath { get; set; }
        public string GlobalFile { get; set; }
        public string StatusBarFile { get; set; }
        public string MacroLibraryFile { get; set; }
        public int[][][] FileASMList { get; set; }
        public int[][] GlobalASMList { get; set; }
        public int SpriteCodeFreeRAM { get; set; }
        public int SpriteCodeFreeBWRAM { get; set; }
        public Code[] CodeList { get; set; }
    }
}
