using AsarCLR;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace UberASMTool
{
    class Program
    {
        const string labelLibraryFile = "asm/work/library.asm";

        private static string mainDirectory;
		private static string[] pathList;

		private static ROM rom;
        private static UberConfig config;

		private static int totalInsertSize;

		/// <summary>
		/// level, overworld, gamemode, global.
		/// </summary>
		private static bool[] enableNmi = new bool[4];

		private static bool error = false;

		/// <summary>
		/// list of pointers that must be manually removed later.
		/// </summary>
		private static List<int> freespacePointerList = new List<int>();

		private static List<Asarlabel> labelList = new List<Asarlabel>();

		/// <summary>
		/// list of pointers that should be protected.
		/// </summary>
		private static List<int> protPointerList = new List<int>();

		private static int[] GetPointers(bool load)
		{
			var labels = Asar.getlabels();
            bool set = false;
			int[] output = new int[4] { -1, -1, -1, -1 };

			foreach (var label in labels)
			{
				switch (label.Name.ToLower())
				{
					case "init": output[0] = label.Location; set = true; break;
					case "main": output[1] = label.Location; set = true; break;
					case "nmi": output[2] = label.Location; set = true; break;
					case "load": if (load) { output[3] = label.Location; set = true; } break;
				}
			}

            return set ? output : null;
		}

        private static string PrintPointer(int pointer)
        {
            if (pointer == -1)
            {
                return "NULL   ";
            }
            else
            {
                return $"${pointer:X6}";
            }
        }

		private static void BuildAsm(string asmPath, int maxItems, int mode,
			string initTable, string mainTable, string nmiTable, string loadTable)
		{
			if (error)
			{
				return;
			}

			int total = 0;

			if (config.VerboseMode)
			{
				Console.WriteLine("Building {0} ASM...", asmPath.ToUpper()[0] + asmPath.Substring(1));
				Console.WriteLine();
			}

			string baseFile = "asm/base/" + asmPath + ".asm";
			string baseTempFile = "asm/work/" + asmPath + ".asm";
			string baseFolder = asmPath + "/";

			string assemblyData = File.ReadAllText(baseFile);

			StringBuilder initPointerList = new StringBuilder();
			StringBuilder mainPointerList = new StringBuilder();
			StringBuilder nmiPointerList = new StringBuilder();
			StringBuilder loadPointerList = new StringBuilder();

			for (int i = 0; i < maxItems; ++i)
			{
				initPointerList.Append("dl ");
				mainPointerList.Append("dl ");
				nmiPointerList.Append("dl ");
				loadPointerList.Append("dl ");

				if (config.FileASMList[mode][i] == null || config.FileASMList[mode][i].Length == 0)
				{
					initPointerList.Append("null_pointer");
					mainPointerList.Append("null_pointer");
					nmiPointerList.Append("null_pointer");
					loadPointerList.Append("null_pointer");
				}
				else
				{
                    // TO DO: support for multi codes must be done here.
                    if (config.FileASMList[mode][i].Length > 1)
                    {
                        Console.WriteLine("Error, too many ASM files.");
                        error = true;
                        return;
                    }

					var code = config.CodeList[config.FileASMList[mode][i][0]];
					string levelContents;

					try
					{
						levelContents = File.ReadAllText(baseFolder + code.Path);
					}
					catch (Exception ex)
					{
						Console.WriteLine("Error while reading file: " + ex.Message);
						error = true;
						return;
					}

                    if (config.VerboseMode)
                    {
                        Console.WriteLine("Processing binary file '{0}':", code.Path);
                    }

                    if (!code.Inserted)
					{
						if (!CompileFile(levelContents, baseFolder, code.Path, baseFolder, true, out int startPc, out int endPc))
						{
							return;
						}

						if (endPc - startPc < 0)
						{
							Console.WriteLine("  {0}: error: Negative insert size. " +
								"Did you change program counter without pushpc/pullpc?", code.Path);
							error = true;
							return;
						}
						else if (endPc - startPc == 0)
						{
							Console.WriteLine("  {0}: error: Null (0 byte insert size) file.", code.Path);
							error = true;
							return;
						}

						if (config.VerboseMode)
						{
							Console.WriteLine("  Inserted at ${0:X6} (PC: 0x{1:x})", startPc,
								SNES.ToPCHeadered(startPc, rom.containsHeader));
							Console.WriteLine("  Insert size: {0} (0x{0:X}) bytes", endPc - startPc + 8);
						}

						totalInsertSize += endPc - startPc + 8;

						var pointers = GetPointers(loadTable == null ? false : true);

						if (pointers == null)
						{
							if (loadTable == null)
							{
								Console.WriteLine("  {0}: error: Missing init/main/nmi label.", code.Path);
							}
							else
							{
								Console.WriteLine("  {0}: error: Missing load/init/main/nmi label.", code.Path);
							}
							error = true;
							return;
						}

						int combo = 0;

						if (pointers[0] == -1)
						{
							initPointerList.Append("null_pointer");
						}
						else if (pointers[0] >= endPc || pointers[0] < startPc)
						{
							Console.WriteLine("  {0}: error: INIT label outside free space range.", code.Path);
							error = true;
							return;
						}
						else
						{
							code.Init = pointers[0];
							initPointerList.AppendFormat("${0:X6}", pointers[0]);
							combo |= 1;
						}

						if (pointers[1] == -1)
						{
							mainPointerList.Append("null_pointer");
						}
						else if (pointers[1] >= endPc || pointers[1] < startPc)
						{
							Console.WriteLine("  {0}: error: MAIN label outside free space range.", code.Path);
							error = true;
							return;
						}
						else
						{
							code.Main = pointers[1];
							mainPointerList.AppendFormat("${0:X6}", pointers[1]);
							combo |= 2;
						}

						if (pointers[3] == -1)
						{
							loadPointerList.Append("null_pointer");
						}
						else if (pointers[3] >= endPc || pointers[3] < startPc)
						{
							Console.WriteLine("  {0}: error: LOAD label outside free space range.", code.Path);
							error = true;
							return;
						}
						else
						{
							code.Load = pointers[3];
							loadPointerList.AppendFormat("${0:X6}", pointers[3]);
							combo |= 8;
						}

						if (pointers[2] == -1)
						{
							nmiPointerList.Append("null_pointer");
						}
						else if (pointers[2] >= endPc || pointers[2] < startPc)
						{
							Console.WriteLine("  {0}: error: NMI label outside free space range.", code.Path);
							error = true;
							return;
						}
						else
						{
							code.Nmi = pointers[2];
							nmiPointerList.AppendFormat("${0:X6}", pointers[2]);
							enableNmi[mode] = true;
							combo |= 4;
						}

						if (config.VerboseMode)
						{
                            Console.Write($"  INIT: {PrintPointer(code.Init)}");

                            if (combo >= 2)
                            {
                                Console.Write($" - MAIN: {PrintPointer(code.Main)}");
                            }
                            if (combo >= 4)
                            {
                                Console.Write($" - NMI: {PrintPointer(code.Nmi)}");
                            }
                            if (combo >= 8)
                            {
                                Console.Write($" - LOAD: {PrintPointer(code.Load)}");
                            }

                            Console.WriteLine();
						}

						code.Inserted = true;
					}
					else
					{
						if (code.Init == -1)
						{
							initPointerList.Append("null_pointer");
						}
						else
						{
							initPointerList.AppendFormat("${0:X6}", code.Init);
						}

						if (code.Main == -1)
						{
							mainPointerList.Append("null_pointer");
						}
						else
						{
							mainPointerList.AppendFormat("${0:X6}", code.Main);
						}

						if (code.Load == -1)
						{
							loadPointerList.Append("null_pointer");
						}
						else
						{
							loadPointerList.AppendFormat("${0:X6}", code.Load);
						}

						if (code.Nmi == -1)
						{
							nmiPointerList.Append("null_pointer");
						}
						else
						{
							nmiPointerList.AppendFormat("${0:X6}", code.Nmi);
							enableNmi[mode] = true;
						}

						if (config.VerboseMode)
						{
							Console.WriteLine("  Insert size: zero bytes");
						}
					}

					if (config.VerboseMode)
					{
						Console.WriteLine();
					}
					total++;
				}

				initPointerList.AppendLine();
				mainPointerList.AppendLine();
				nmiPointerList.AppendLine();
				loadPointerList.AppendLine();
			}

			InsertTable(ref assemblyData, initTable, initPointerList.ToString());
			InsertTable(ref assemblyData, mainTable, mainPointerList.ToString());
			if (loadTable != null)
			{
				InsertTable(ref assemblyData, loadTable, loadPointerList.ToString());
			}

			if (enableNmi[mode])
			{
				InsertTable(ref assemblyData, nmiTable, nmiPointerList.ToString());
			}

			for (int i = 0; i < 1000; ++i)
			{
				try
				{
					File.WriteAllText(baseTempFile, assemblyData);
					break;
				}
				catch
				{
					if (i == 999)
					{
						Console.WriteLine("  Error: access denied while creating base temp file.");
						error = true;
						throw;
					}

					Thread.Sleep(10);
				}
				if (i == 999)
				{
					throw new Exception();
				}
			}

			if (config.VerboseMode)
			{
				if (total == 0)
				{
					return;
				}
				else if (total == 1)
				{
					Console.WriteLine("Total one file processed.");
					Console.WriteLine();
				}
				else
				{
					Console.WriteLine("Total {0} files processed.", total);
					Console.WriteLine();
				}
			}
		}

		private static void InsertTable(ref string asmFile, string label, string table)
		{
			int index = asmFile.IndexOf(label + ":");

			if (index == -1)
			{
				throw new Exception("can't find label.");
			}

			index += label.Length + 1;

			asmFile = asmFile.Insert(index, "\r\n" + table);
		}

		private static int DirectoryDepth(string fileName, string directoryBase)
		{
			string path1 = Path.GetFullPath(directoryBase);
			string path2 = Path.GetFullPath(fileName);
			char[] separators = new[] { Path.PathSeparator, Path.AltDirectorySeparatorChar,
				Path.DirectorySeparatorChar, Path.VolumeSeparatorChar };

			return path2.Substring(path1.Length).Split(separators, StringSplitOptions.RemoveEmptyEntries).Length;
		}

		private static string FixPath(string fileName, string directoryBase)
		{
			int depth = DirectoryDepth(fileName, directoryBase);
			return String.Join("", Enumerable.Repeat("../", depth));
		}

		private static string GenerateBasefile(string input, bool library, string fileName, string directoryBase)
		{
			string fix = FixPath(fileName, directoryBase);
			StringBuilder output = new StringBuilder();

			if (library)
			{
				output.AppendFormat("incsrc \"{1}{0}\" : ", labelLibraryFile, fix);
			}
			output.AppendFormat("incsrc \"{1}{0}\" : ", config.MacroLibraryFile, fix);
			output.Append("freecode cleaned : ");
			output.AppendLine("print \"_startl \", pc");
			output.AppendLine(input);
			output.AppendLine("print \"_endl \", pc");
			return output.ToString();
		}

		private static bool CompileFile(string data, string baseFolder, string originalFile, string originFolder,
			bool library, out int startPc, out int endPc)
		{
			endPc = -1;
			startPc = -1;

			string realFile = mainDirectory + baseFolder + "__temp.asm";
			string compileFile = originalFile;

			for (int i = 0; i < 1000; ++i)
			{
				try
				{
					File.WriteAllText(realFile, GenerateBasefile(data, library, realFile, originFolder));
					break;
				}
				catch
				{
					if (i == 999)
					{
						Console.WriteLine("  Error: access denied while creating temporary file {0}.", realFile);
						error = true;
						throw;
					}

					Thread.Sleep(10);
				}
				if (i == 999)
				{
					throw new Exception();
				}
			}
			
			bool status = Asar.patch(realFile, ref rom.romData, pathList);

			for (int i = 0; i < 1000; ++i)
			{
				try
				{
					File.Delete(realFile);
					break;
				}
				catch
				{
					if (i == 999)
					{
						Console.WriteLine("  Warning: could not delete temporary file {0}.", realFile);
						throw;
					}

					Thread.Sleep(10);
				}
				if (i == 999)
				{
					throw new Exception();
				}
			}
			
			foreach (var warn in Asar.getwarnings())
			{
				Console.WriteLine("  {0}", warn.Fullerrdata.Replace(warn.Filename, compileFile));
			}
			foreach (var warn in Asar.geterrors())
			{
				Console.WriteLine("  {0}", warn.Fullerrdata.Replace(warn.Filename, compileFile));
			}

			if (!status)
			{
				Console.WriteLine();
				Console.WriteLine("Some errors occured while running UberASM tool. Process aborted.");
				Console.WriteLine("Your ROM wasn't modified.");
				error = true;
				return false;
			}

			// process prints. prot requests, internal things, tec.
			string[] prints = Asar.getprints();
			bool endl = false;
			bool startl = false;

			foreach (string print in prints)
			{
				string[] split = print.Trim().Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
				string command = split.Length > 0 ? split[0].ToLower() : "";
                string value = split.Length > 1 ? String.Join(" ", split, 1, split.Length - 1) : "";

                if (endl)
				{
					Console.WriteLine("  {0}: error: unexpected print after end of the file print mark.", originalFile);
					error = true;
					return false;
				}

				if (!startl && command != "_startl")
				{
					Console.WriteLine("  {0}: error: unexpected print before STARTL command.", originalFile);
					error = true;
					return false;
				}

				switch (command)
				{
					case "_prot":
						try
						{
							int ptr = Convert.ToInt32(value, 16);

							if (!protPointerList.Contains(ptr))
							{
								protPointerList.Add(ptr);
							}
							break;
						}
						catch
						{
							Console.WriteLine("  {0}: error: invalid PRINT PROT value.", originalFile);
							error = true;
							return false;
						}

					case "_startl":
						try
						{
							startPc = Convert.ToInt32(value, 16);
							startl = true;
							break;
						}
						catch
						{
							Console.WriteLine("  {0}: error: invalid PRINT STARTL value.", originalFile);
							error = true;
							return false;
						}

					case "_endl":
						try
						{
							endPc = Convert.ToInt32(value, 16);
							endl = true;
							break;
						}
						catch
						{
							Console.WriteLine("  {0}: error: invalid PRINT ENDL value.", originalFile);
							error = true;
							return false;
						}

					default:
						Console.WriteLine(print);
						break;
				}
			}

			if (startPc == endPc)
			{
				Console.WriteLine("  {0}: error: empty assembled file.", originalFile);
				error = true;
				return false;
			}

			return true;
		}

		static void BuildOther()
		{
			if (error)
			{
				return;
			}

			// prepare global file
			string current = File.ReadAllText(config.GlobalFile);
			byte[] tempBuffer = new byte[32768];

			var temp = current.Insert(0, "lorom\r\norg $108000");

			for (int i = 0; i < 1000; ++i)
			{
				try
				{
					File.WriteAllText("asm/work/temp.asm", temp);
					break;
				}
				catch
				{
					if (i == 999)
					{
						Console.WriteLine("  Error: access denied while creating temporary file {0}.", temp);
						error = true;
						throw;
					}

					Thread.Sleep(10);
				}
				if (i == 999)
				{
					throw new Exception();
				}
			}

			Asar.patch(mainDirectory + "asm/work/temp.asm", ref tempBuffer, pathList);
			var ptr = GetPointers(false);
			enableNmi[3] = ptr[2] != -1;

			current = current.Insert(0, "\r\nnamespace global\r\n");
			current += "\r\nnamespace off\r\n";

			string global = File.ReadAllText("asm/base/global.asm");
			global += current;

			// build prot list
			StringBuilder protList = new StringBuilder();

			foreach (int protPointer in protPointerList)
			{
				protList.AppendFormat("dl ${0:X6}", protPointer);
				protList.AppendLine();
			}

			InsertTable(ref global, "prot_table", protList.ToString());

			File.WriteAllText("asm/work/global.asm", global);

			// prepare status file
			current = File.ReadAllText(config.StatusBarFile);
			current = current.Insert(0, "\r\nnamespace status_bar\r\n");
			current += "\r\nnamespace off\r\n";

			global = File.ReadAllText("asm/base/statusbar.asm");
			global += current;

			File.WriteAllText("asm/work/statusbar.asm", global);

			// copy sprites.asm
			File.Copy("asm/base/sprites.asm", "asm/work/sprites.asm", true);

			// prepare main file
			StringBuilder mainFile = new StringBuilder();

			mainFile.AppendFormat("incsrc \"{0}\"", labelLibraryFile);
			mainFile.AppendLine();
			mainFile.AppendFormat("incsrc \"../../{0}\"", config.MacroLibraryFile);
			mainFile.AppendLine();
			mainFile.AppendFormat("!level_nmi\t= {0}\r\n", enableNmi[0] ? 1 : 0);
			mainFile.AppendFormat("!overworld_nmi\t= {0}\r\n", enableNmi[1] ? 1 : 0);
			mainFile.AppendFormat("!gamemode_nmi\t= {0}\r\n", enableNmi[2] ? 1 : 0);
			mainFile.AppendFormat("!global_nmi\t= {0}\r\n\r\n", enableNmi[3] ? 1 : 0);
			mainFile.AppendFormat("!sprite_RAM\t= ${0:X6}\r\n\r\n", GetSpriteRAMValue());

			foreach (int cleanPtr in freespacePointerList)
			{
				mainFile.AppendFormat("autoclean ${0:X6}\r\n", cleanPtr);
			}

			mainFile.AppendLine();

			mainFile.Append(File.ReadAllText("asm/base/main.asm"));

			mainFile.AppendLine();
			mainFile.AppendLine("print freespaceuse");

			File.WriteAllText("asm/work/main.asm", mainFile.ToString());
		}

        private static int GetSpriteRAMValue()
        {
            int result = rom.sa1 && config.SpriteCodeFreeBWRAM != 0
                ? config.SpriteCodeFreeBWRAM : config.SpriteCodeFreeRAM;

            if (result == 0)
            {
                Console.WriteLine("Error: sprite code free RAM address was not defined.");
                error = true;
            }

            return result;
        }

        private static void CheckPreviousData()
		{
			//gamemode - $009322+1
			//levelASM - $00A242+1
			//overworld - $00A1C3+1
			//global - $00804E+1

			CleanPointerTable(0x1323, 256, false);
			CleanPointerTable(0x2243, 512, true);
			CleanPointerTable(0x21C4, 7, false);

			int total = freespacePointerList.Count;

			if (config.VerboseMode)
			{
				if (total == 1)
				{
					Console.WriteLine("One main pointer cleaned.");
				}
				else
				{
					Console.WriteLine("{0} main pointers cleaned.", total);
				}

				Console.WriteLine();
			}

			// clear external pointer table
			int ptr = CheckPointer(0x4F);

			if (ptr == -1)
			{
				return;
			}

			ptr -= 3;

			while (!CheckForUberSignature(ptr - 1))
			{
				int pointer = rom.Read24(ptr);
				if (!freespacePointerList.Contains(pointer))
				{
					freespacePointerList.Add(pointer);

					if (config.VerboseMode)
					{
						Console.WriteLine("${1:X6} (PC: 0x{0:x})", SNES.ToPCHeadered(pointer, rom.containsHeader), pointer);
					}
				}
				ptr -= 3;
			}

			if (config.VerboseMode && freespacePointerList.Count - total > 0)
			{
				total = freespacePointerList.Count - total;

				if (total == 1)
				{
					Console.WriteLine("One external pointer cleaned.");
				}
				else
				{
					Console.WriteLine("{0} external pointers cleaned.", total);
				}

				Console.WriteLine();
			}
		}

		private static bool CheckForUberSignature(int ptr)
		{
			var str = rom.ReadBlock(ptr, 4);
			return (str[0] == (byte)'u' && str[1] == (byte)'b' && str[2] == (byte)'e' && str[3] == (byte)'r');
		}

		private static int CheckPointer(int offset)
		{
			int ptr = SNES.ToPC(rom.Read24(offset));
			
			if (ptr < 0x80000 || ptr > rom.romSize)
			{
				// does not seem to be a valid pointer.
				return -1;
			}

			ptr -= 4;

			var str = rom.ReadBlock(ptr, 4);
			if (!(str[0] == (byte)'t' && str[1] == (byte)'o' && str[2] == (byte)'o' && str[3] == (byte)'l'))
			{
				// okay, does not seem legit.
				return -1;
			}

			return ptr;
		}

		private static void CleanPointerTable(int offset, int pointerCount, bool ext)
		{
			int ptr = CheckPointer(offset);

			if (ptr == -1)
			{
				return;
			}
			
			ptr -= pointerCount * 6;

			bool nmi = false;
			bool load = false;

		scan:
			if (!CheckForUberSignature(ptr - 4))
			{
				if (load)
				{
					// does not seem legit.
					return;
				}
				else if (nmi)
				{
					if (ext)
					{
						load = true;
						ptr -= pointerCount * 3;
						goto scan;
					}
					else
					{
						//does not seem legit..
						return;
					}
				}
				else
				{
					nmi = true;
					ptr -= pointerCount * 3;
					goto scan;
				}
			}

			int total = pointerCount * (load ? 4 : (nmi ? 3 : 2));

			for (int i = 0; i < total; ++i, ptr += 3)
			{
				int pointer = rom.Read24(ptr);
				if (!freespacePointerList.Contains(pointer))
				{
					freespacePointerList.Add(pointer);
					
					if (config.VerboseMode)
					{
						Console.WriteLine("${1:X6} (PC: 0x{0:x})", SNES.ToPCHeadered(pointer, rom.containsHeader), pointer);
					}
				}
			}
		}

		private static void BuildLibrary()
		{
			if (config.VerboseMode)
			{
				Console.WriteLine("Building external library...");
				Console.WriteLine();
			}

			int fileCount = 0;

			bool binaryMode = false;
			var files = Directory.GetFiles("library/", "*.asm", SearchOption.AllDirectories);
			string fullPath = Path.GetFullPath("library/");

		repeat:
			foreach (var asmFile in files)
			{
				int start;
				int end;
				string baseFolder = Path.GetDirectoryName(asmFile) + "/";
				string fileName = Path.GetFileName(asmFile);
				string fileNameExt = Path.GetFileNameWithoutExtension(asmFile);
				string fullFilePath = Path.GetFullPath(asmFile);
				string labelLevel = fullFilePath.Substring(fullPath.Length)
					.Replace(" ", "_").Replace("/", "_").Replace("\\", "_");
				int s = labelLevel.LastIndexOf('.');

				if (config.VerboseMode && binaryMode)
				{
					Console.WriteLine("Processing binary file '{0}':", fileName);
				}
				else if (config.VerboseMode)
				{
					Console.WriteLine("Processing file '{0}':", fileName);
				}

				if (s != -1)
				{
					labelLevel = labelLevel.Substring(0, s);
				}

				if (!binaryMode)
				{
					if (!CompileFile(File.ReadAllText(asmFile), baseFolder, fileName, "library", false, out start, out end))
					{
						return;
					}
				}
				else
				{
					string baseAssembly = labelLevel +
						":\r\nincbin \"" + fileName + "\"\r\n";

					if (!CompileFile(baseAssembly, baseFolder, fileName, "library", false, out start, out end))
					{
						return;
					}
				}

				if (config.VerboseMode)
				{
					Console.WriteLine("  Inserted at ${0:X6} (PC: 0x{1:x})",
						start, SNES.ToPCHeadered(start, rom.containsHeader));
					Console.WriteLine("  Insert size: {0} (0x{0:X}) bytes", end - start + 8);
				}

				totalInsertSize += end - start + 8;

				var labels = Asar.getlabels().ToList();
				int copyl = 0;

				for (int i = 0; i < labels.Count; ++i)
				{
					if (labels[i].Name != labelLevel)
					{
						var copy = labels[i];
						copy.Name = labelLevel + "_" + copy.Name;

						labels[i] = copy;
					}

					if (labelList.Any(x => x.Name == labels[i].Name))
					{
						Console.WriteLine("  {0} - error: label redefinition [{1}].", fileName, labels[i].Name);
						error = true;
						return;
					}

					if (!labels[i].Name.Contains(":"))
					{
						labelList.Add(labels[i]);
						copyl++;
					}

					if (labels[i].Location >= end || labels[i].Location < start)
					{
						labels.RemoveAt(i);
						--i;
					}
				}

				if (config.VerboseMode)
				{
					if (copyl == 1)
					{
						Console.WriteLine("  Processed one label.");
					}
					else
					{
						Console.WriteLine("  Processed {0} labels.", copyl);
					}
				}

				if (labels.Count == 0)
				{
					Console.WriteLine("  {0}: error: file contains no label within freespace area.", fileName);
					error = true;
					return;
				}

				if (!protPointerList.Contains(labels[0].Location))
				{
					protPointerList.Add(labels[0].Location);
				}
				else
				{
					Console.WriteLine("  {0}: wtf error: Library included file is already protected.", fileName);
					error = true;
					return;
				}

				if (config.VerboseMode)
				{
					Console.WriteLine();
				}
				fileCount++;
			}

			if (!binaryMode)
			{
				binaryMode = true;
				files = Directory.GetFiles("library/", "*.*", SearchOption.AllDirectories)
					.Where(x => !x.EndsWith(".asm", StringComparison.InvariantCultureIgnoreCase)).ToArray();
				goto repeat;
			}

			if (fileCount == 0)
			{
				return;
			}

			if (fileCount == 1 && config.VerboseMode)
			{
				Console.WriteLine("Processed one library file.");
			}
			else if (config.VerboseMode)
			{
				Console.WriteLine("Processed {0} library files.", fileCount);
			}
		}

		private static void GenerateLibrary()
		{
			StringBuilder sb = new StringBuilder();

			foreach (var label in labelList)
			{
				sb.AppendFormat("{0} = ${1:X6}", label.Name, label.Location);
				sb.AppendLine();
			}

			File.WriteAllText(labelLibraryFile, sb.ToString());

			if (labelList.Count == 0 || !config.VerboseMode)
			{
				return;
			}
			else if (labelList.Count == 1)
			{
				Console.WriteLine("Total one library label generated.");
			}
			else
			{
				Console.WriteLine("Total {0} library labels generated.", labelList.Count);
			}

			Console.WriteLine();
		}

		private static void Pause()
		{
			Console.Write("Press any key to continue...");

			try
			{
				Console.ReadKey(true);
			}
			catch
			{

			}
		}

		private static void WriteRestoreComment()
		{
			// create LM restore information
			try
			{
				string restorePath = Path.GetFullPath(config.ROMPath);
				restorePath = restorePath.Substring(0, restorePath.LastIndexOf(Path.GetFileName(restorePath)));
				restorePath += Path.GetFileNameWithoutExtension(config.ROMPath);
				restorePath += ".extmod";

                string fileContents = File.Exists(restorePath) ? File.ReadAllText(restorePath) : "";
                string appendText = "UberASM Tool v1.5";

                if (!fileContents.EndsWith(appendText))
                {
                    if (fileContents.Length > 0)
                    {
                        fileContents = fileContents.TrimEnd() + " ";
                    }

                    File.WriteAllText(restorePath, fileContents + appendText);
                }
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.ToString());
			}
		}

		static void Main(string[] args)
		{
			Directory.SetCurrentDirectory(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
			mainDirectory = Environment.CurrentDirectory + "/";

			pathList = new string[3];
			pathList[0] = mainDirectory;
			pathList[1] = mainDirectory + "asm/work/";
			pathList[2] = mainDirectory + "asm/"; // this is for compatibility with old patches.

			if (!Asar.init())
			{
				Console.WriteLine("Could not initialize or find asar.dll");
				Console.WriteLine("Please redownload the program.");
				Pause();
				return;
			}

			if (args.Length == 0 || args.Length > 2)
			{
				Console.WriteLine("Usage: UberASMTool [code list] [ROM]");
				Console.WriteLine("If ROM is not specified, UberASM Tool will search for the one in the code list.");
				Console.WriteLine("If code list is not specified, UberASM Tool will try loading 'list.txt'.");
				Console.WriteLine();
				
				if (args.Length > 2)
				{
					Pause();
					return;
				}
			}

            UberConfigParser parser = new UberConfigParser();

			if (args.Length == 2)
			{
                parser.OverrideROM = args[1];

				try
				{
                    parser.LoadListFile(args[0]);
				}
				catch (Exception ex)
				{
					Console.WriteLine("Can't read {0}: {1}.", args[0], ex.Message);
					Pause();
					return;
				}
			}
			else if (args.Length == 1)
            {
                parser.OverrideROM = null;

                try
                {
                    parser.LoadListFile(args[0]);
                }
				catch(Exception ex)
				{
					Console.WriteLine("Can't read {0}: {1}.", args[0], ex.Message);
					Pause();
					return;
				}
			}
			else if (File.Exists("list.txt"))
            {
                parser.LoadListFile("list.txt");
            }
			else
			{
				Console.WriteLine("list.txt not found.");
				Pause();
				return;
			}

			Console.WriteLine();

            if (!parser.ParseList())
            {
                Console.WriteLine("Could not parse list file!");
                Console.WriteLine(parser.GetLogs());
                Pause();
                return;
            }

            config = parser.Build();

			if (config.ROMPath == null)
			{
				Console.WriteLine("ROM file not given.");
				Pause();
				return;
			}

			try
			{
				rom = new ROM(config.ROMPath);
				rom.Init();
				SNES.DetectAddressing(rom.romType & 255);
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error: {0}", ex.Message);
				Pause();
				return;
			}

			if (config.VerboseMode)
			{
				Console.WriteLine("Cleaning up previous runs: ");
			}

			CheckPreviousData();

			BuildLibrary();

			if (error)
			{
				Pause();
				return;
			}

			GenerateLibrary();

			BuildAsm("overworld", 7, 1, "OW_init_table", "OW_asm_table", "OW_nmi_table", null);
			BuildAsm("gamemode", 256, 2, "gamemode_init_table", "gamemode_main_table", "gamemode_nmi_table", null);
			BuildAsm("level", 512, 0, "level_init_table", "level_asm_table", "level_nmi_table", "level_load_table");
			BuildOther();

			if (error)
			{
				Pause();
				return;
			}

			if (config.VerboseMode)
			{
				Console.WriteLine("Total files insert size: {0} (0x{0:X4}) bytes", totalInsertSize);
			}

			if (Asar.patch(mainDirectory + "asm/work/main.asm", ref rom.romData, pathList))
			{
				foreach (var warn in Asar.getwarnings())
				{
					Console.WriteLine(warn.Fullerrdata);
				}

				var prints = Asar.getprints();
                bool printed = false;

                for (int i = 0; i < prints.Length; ++i)
				{
					if (i + 1 != prints.Length)
					{
						Console.WriteLine(prints[i]);
					}
					else if (int.TryParse(prints[i], out int insertSize) && config.VerboseMode)
					{
						Console.WriteLine("Main patch insert size: {0} (0x{0:X4}) bytes", insertSize);
						Console.WriteLine();
						Console.WriteLine("Total: {0} (0x{0:X4}) bytes", insertSize + totalInsertSize);
						Console.WriteLine();
						printed = true;
					}
				}

				if (config.VerboseMode && !printed)
				{
					Console.WriteLine();
				}

				Console.WriteLine("Codes inserted successfully.");
				rom.Save();

				WriteRestoreComment();
			}
			else
			{
				foreach (var warn in Asar.getwarnings())
				{
					Console.WriteLine(warn.Fullerrdata.Replace(warn.Filename, ""));
				}
				foreach (var warn in Asar.geterrors())
				{
					Console.WriteLine(warn.Fullerrdata.Replace(warn.Filename, ""));
				}

				Console.WriteLine("Some errors occured while applying main patch. Process aborted.");
				Console.WriteLine("Your ROM wasn't modified.");
			}

			Pause();
		}
	}
}
