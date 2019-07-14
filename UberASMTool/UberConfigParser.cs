using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace UberASMTool
{
    class UberConfigParser
    {
        public string OverrideROM { get; set; }

        private string[] listFile;
        private StringBuilder parseLog;

        private readonly List<Code> codeList = new List<Code>();
        private bool verbose = false;
        private string romPath;
        private string globalFile;
        private string statusBarFile;
        private string macroLibraryFile;
        private readonly List<int>[][] list = new List<int>[3][] { new List<int>[512], new List<int>[7], new List<int>[256] };
        private int spriteCodeFreeRAM;

        public string GetLogs()
        {
            return parseLog.ToString();
        }

        public UberConfig Build()
        {
            return new UberConfig()
            {
                VerboseMode = verbose,
                ROMPath = OverrideROM ?? romPath,
                GlobalFile = globalFile,
                StatusBarFile = statusBarFile,
                MacroLibraryFile = macroLibraryFile,
                FileASMList = list,
                SpriteCodeFreeRAM = spriteCodeFreeRAM,
                CodeList = codeList,
            };
        }

        public void LoadListFile(string path)
        {
            listFile = File.ReadAllLines(path);
        }

        public bool ParseList()
        {
            parseLog = new StringBuilder();

            // 0 = level, 1 = ow, 2 = gamemode
            int mode = -1;

            for (int i = 0; i < listFile.Length; ++i)
            {
                string line = listFile[i].Trim();

                if (line.StartsWith(";") || line == "")
                {
                    continue;
                }

                string lw = line.ToLower();

                switch (lw)
                {
                    case "level:": mode = 0; continue;
                    case "overworld:": mode = 1; continue;
                    case "gamemode:": mode = 2; continue;
                }

                string[] split = line.Split(new string[] { " ", "\t" }, StringSplitOptions.RemoveEmptyEntries);

                if (split.Length < 2)
                {
                    parseLog.AppendLine($"Line {i + 1} - error: missing file name or command.");
                    return false;
                }

                split[0] = split[0].ToLower();

                string value = String.Join(" ", split, 1, split.Length - 1);

                int index = value.IndexOf(';');

                if (index != -1)
                {
                    value = value.Substring(0, index).Trim();
                }

                switch (split[0])
                {
                    case "verbose:":
                        verbose = value.ToLower() == "on";
                        continue;

                    case "rom:":
                        if (romPath == null)
                        {
                            if (File.Exists(value))
                            {
                                romPath = value;
                                continue;
                            }
                            else
                            {
                                parseLog.AppendLine($"Line {i + 1} - error: ROM file does not exist.");
                                return false;
                            }
                        }
                        else
                        {
                            parseLog.AppendLine($"Line {i + 1} - warning: ROM file already specified.");
                            continue;
                        }

                    case "macrolib:":
                        if (macroLibraryFile == null)
                        {
                            if (File.Exists(value))
                            {
                                macroLibraryFile = value;
                                continue;
                            }
                            else
                            {
                                parseLog.AppendLine($"Line {i + 1} - error: file does not exist.");
                                return false;
                            }
                        }
                        else
                        {
                            parseLog.AppendLine($"Line {i + 1} - warning: macro library file already defined.");
                            continue;
                        }

                    case "global:":
                        if (globalFile == null)
                        {
                            if (File.Exists(value))
                            {
                                globalFile = value;
                                continue;
                            }
                            else
                            {
                                parseLog.AppendLine($"Line {i + 1} - error: file does not exist.");
                                return false;
                            }
                        }
                        else
                        {
                            parseLog.AppendLine($"Line {i + 1} - warning: global file already defined.");
                            continue;
                        }

                    case "statusbar:":
                        if (statusBarFile == null)
                        {
                            if (File.Exists(value))
                            {
                                statusBarFile = value;
                                continue;
                            }
                            else
                            {
                                parseLog.AppendLine($"Line {i + 1} - error: file does not exist.");
                                return false;
                            }
                        }
                        else
                        {
                            parseLog.AppendLine($"Line {i + 1} - warning: status bar file already defined.");
                            continue;
                        }

                    case "sprite:":
                        if (spriteCodeFreeRAM == 0)
                        {
                            if (value.StartsWith("$"))
                            {
                                value = value.Substring(1);
                            }
                            else if (value.StartsWith("0x"))
                            {
                                value = value.Substring(2);
                            }
                            try
                            {
                                spriteCodeFreeRAM = Convert.ToInt32(value, 16);
                                continue;
                            }
                            catch
                            {
                                parseLog.AppendLine($"Line {i + 1} - error: invalid sprite code free RAM address.");
                                return false;
                            }
                        }
                        else
                        {
                            parseLog.AppendLine($"Line {i + 1} - warning: sprite code free RAM address already defined.");
                            continue;
                        }
                }

                if (value.StartsWith("$"))
                {
                    value = value.Substring(1);
                }
                else if (value.StartsWith("0x"))
                {
                    value = value.Substring(2);
                }

                int hexValue;

                try
                {
                    hexValue = Convert.ToInt32(split[0], 16);
                }
                catch
                {
                    parseLog.AppendLine($"Line {i + 1} - error: invalid number.");
                    return false;
                }

                if (hexValue < 0)
                {
                    parseLog.AppendLine($"Line {i + 1} - error: invalid number.");
                    return false;
                }

                switch (mode)
                {
                    case -1:
                        parseLog.AppendLine($"Line {i + 1} - error: unspecified code type (level/overworld/gamemode).");
                        return false;

                    // level
                    case 0:
                        if (hexValue > 0x1FF)
                        {
                            parseLog.AppendLine($"Line {i + 1} - error: level out of range (000 - 1FF).");
                            return false;
                        }
                        break;

                    // overworld
                    case 1:
                        if (hexValue > 6)
                        {
                            parseLog.AppendLine($"Line {i + 1} - error: overworld number out of range (0-6).");
                            return false;
                        }
                        break;

                    // game mode
                    case 2:
                        if (hexValue > 0xFF)
                        {
                            parseLog.AppendLine($"Line {i + 1} - game mode number out of range (00 - FF).");
                            return false;
                        }
                        break;
                }

                try
                {
                    AddLevelCode(value, hexValue, mode);
                    continue;
                }
                catch (Exception ex)
                {
                    parseLog.AppendLine(ex.Message);
                    return false;
                }
            }

            if (macroLibraryFile == null)
            {
                parseLog.AppendLine("Error: macro library file was not defined.");
                return false;
            }
            if (statusBarFile == null)
            {
                parseLog.AppendLine("Error: status bar file was not defined.");
                return false;
            }
            if (globalFile == null)
            {
                parseLog.AppendLine("Error: global file was not defined.");
                return false;
            }
            if (spriteCodeFreeRAM == 0)
            {
                parseLog.AppendLine("Error: sprite code free RAM address was not defined.");
                return false;
            }

            return true;
        }

        private void AddLevelCode(string path, int level, int type)
        {
            List<int> currentList;

            if (list[type][level] == null)
            {
                currentList = list[type][level] = new List<int>();
            }
            else
            {
                currentList = list[type][level];
            }

            if (currentList.Count > 1)
            {
                throw new Exception("Number is already used.");
            }

            // TO DO: use hashes or anything better than path matching.
            int codeIdentifier = codeList.FindIndex(x => x.Path == path);

            if (codeIdentifier == -1)
            {
                // add new ASM file
                codeIdentifier = codeList.Count;
                codeList.Add(new Code(path));
            }

            currentList.Add(codeIdentifier);
        }
    }
}
