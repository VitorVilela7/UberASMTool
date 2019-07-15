using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace UberASMTool
{
    static class FileUtils
    {
        const int maxAttempts = 1000;
        const int idleTimeBetweenAttempt = 10;

        public static bool ForceCreate(string fileName, string contents)
        {
            for (int i = 0; i < maxAttempts; ++i)
            {
                try
                {
                    File.WriteAllText(fileName, contents);
                    return true;
                }
                catch
                {
                    Thread.Sleep(idleTimeBetweenAttempt);
                }
            }

            return false;
        }

        public static bool ForceDelete(string fileName)
        {
            if (!File.Exists(fileName))
            {
                return true;
            }

            for (int i = 0; i < maxAttempts; ++i)
            {
                try
                {
                    File.Delete(fileName);
                    return true;
                }
                catch
                {
                    Thread.Sleep(idleTimeBetweenAttempt);
                }
            }

            return false;
        }

        public static int DirectoryDepth(string fileName, string directoryBase)
        {
            string path1 = Path.GetFullPath(directoryBase);
            string path2 = Path.GetFullPath(fileName);
            char[] separators = new[] { Path.PathSeparator, Path.AltDirectorySeparatorChar,
                Path.DirectorySeparatorChar, Path.VolumeSeparatorChar };

            return path2.Substring(path1.Length).Split(separators, StringSplitOptions.RemoveEmptyEntries).Length;
        }

        public static string FixPath(string fileName, string directoryBase)
        {
            int depth = DirectoryDepth(fileName, directoryBase);
            return String.Join("", Enumerable.Repeat("../", depth));
        }
    }
}
