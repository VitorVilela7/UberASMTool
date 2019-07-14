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
        private string romPath = null;
        private string globalFile = null;
        private string statusBarFile = null;
        private string macroLibraryFile = null;
        private readonly List<int>[][] list = new List<int>[3][] { new List<int>[512], new List<int>[7], new List<int>[256] };
        private readonly List<int>[] globalList = new List<int>[3];
        private int spriteCodeFreeRAM = 0;
        private int spriteCodeFreeBWRAM = 0;

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
                FileASMList = list.Select(c => c.Select(d => d?.ToArray()).ToArray()).ToArray(),
                GlobalASMList = globalList.Select(c => c?.ToArray()).ToArray(),
                SpriteCodeFreeRAM = spriteCodeFreeRAM,
                SpriteCodeFreeBWRAM = spriteCodeFreeBWRAM,
                CodeList = codeList.ToArray(),
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

                string valueHex = value;

                if (valueHex.StartsWith("$"))
                {
                    valueHex = valueHex.Substring(1);
                }
                else if (valueHex.StartsWith("0x"))
                {
                    valueHex = valueHex.Substring(2);
                }

                switch (split[0])
                {
                    case "verbose:":
                        verbose = value.ToLower() == "on";
                        continue;

                    case "rom:":
                        if (!ParseGlobalFileDeclaration(ref romPath, "ROM", value, i))
                            return false;
                        continue;

                    case "macrolib:":
                        if (!ParseGlobalFileDeclaration(ref macroLibraryFile, "Macro Library", value, i))
                            return false;
                        continue;

                    case "global:":
                        if (!ParseGlobalFileDeclaration(ref globalFile, "Global ASM", value, i)) return false;
                        continue;

                    case "statusbar:":
                        if (!ParseGlobalFileDeclaration(ref statusBarFile, "Status Bar ASM", value, i)) return false;
                        continue;

                    case "sprite-sa1:":
                        if (!ParseHexDefineDeclaration(ref spriteCodeFreeBWRAM,
                            "sprite code free SA-1 RAM address", valueHex, i)) return false;
                        continue;

                    case "sprite:":
                        if (!ParseHexDefineDeclaration(ref spriteCodeFreeRAM,
                            "sprite code free RAM address", valueHex, i)) return false;
                        continue;
                }

                int hexValue;

                if (split[0] == "*")
                {
                    hexValue = -1;
                }
                else
                {
                    try
                    {
                        hexValue = Convert.ToUInt16(split[0], 16);
                    }
                    catch
                    {
                        WriteLog("invalid hex number.", i);
                        return false;
                    }
                }

                switch (mode)
                {
                    case -1:
                        WriteLog("unspecified code type (level/overworld/gamemode).", i);
                        return false;

                    // level
                    case 0:
                        if (hexValue > 0x1FF)
                        {
                            WriteLog("level out of range (000 - 1FF).", i);
                            return false;
                        }
                        break;

                    // overworld
                    case 1:
                        if (hexValue > 6)
                        {
                            WriteLog("overworld number out of range (0-6).", i);
                            return false;
                        }
                        break;

                    // game mode
                    case 2:
                        if (hexValue > 0xFF)
                        {
                            WriteLog("game mode number out of range (00 - FF).", i);
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
                    WriteLog(ex.Message, i);
                    return false;
                }
            }

            return ValidateDefinitions();
        }

        private bool ParseHexDefineDeclaration(ref int defineDestination, string defineType, string valueHex, int i)
        {
            if (defineDestination == 0)
            {
                try
                {
                    defineDestination = Convert.ToInt32(valueHex, 16);
                    return true;
                }
                catch
                {
                    WriteLog($"invalid {defineType} hex number.", i);
                    return false;
                }
            }
            else
            {
                WriteLog($"{defineType} was already defined.", i, false);
                return true;
            }
        }

        private bool ParseGlobalFileDeclaration(ref string fileDefinition, string fileType, string value, int i)
        {
            if (fileDefinition == null)
            {
                if (File.Exists(value))
                {
                    fileDefinition = value;
                    return true;
                }
                else
                {
                    WriteLog("file does not exist.", i);
                    return false;
                }
            }
            else
            {
                WriteLog($"{fileType} file was already defined.", i, false);
                return true;
            }
        }

        private bool ValidateDefinitions()
        {
            if (macroLibraryFile == null)
            {
                WriteLog("macro library file was not defined.");
                return false;
            }
            if (statusBarFile == null)
            {
                WriteLog("status bar file was not defined.");
                return false;
            }
            if (globalFile == null)
            {
                WriteLog("global file was not defined.");
                return false;
            }
            return true;
        }

        private void WriteLog(string description, int line = -1, bool error = true)
        {
            if (line == -1)
            {
                parseLog.AppendLine($"{(error ? "Error" : "Warning")}: {description}");
            }
            else
            {
                parseLog.AppendLine($"{(error ? "Error" : "Warning")}: line {line + 1} - {description}");
            }
        }

        private void AddLevelCode(string path, int level, int type)
        {
            List<int> currentList;

            if (level == -1)
            {
                if (globalList[type] == null)
                {
                    currentList = globalList[type] = new List<int>();
                }
                else
                {
                    currentList = globalList[type];
                }
            }
            else
            {
                if (list[type][level] == null)
                {
                    currentList = list[type][level] = new List<int>();
                }
                else
                {
                    currentList = list[type][level];
                }
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
