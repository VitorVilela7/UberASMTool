using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UberASMTool
{
    /*
     * Specialized in processing input assembly files and generating UberAssemblyFile
     */
    class UberAssemblyProcessor
    {
        public UberAssemblyFile LoadAndProcessFile(string input, bool library, string fileName, string directoryBase, string macroLibraryFile)
        {
            var processed = ProcessAndGenerateFile(input, library, fileName, directoryBase, macroLibraryFile);

            return new UberAssemblyFile
            {
                ProcessedContents = processed,
                IsLibrary = library,
                FilePath = fileName
            };
        }

        private string ProcessAndGenerateFile(string input, bool library, string fileName, string directoryBase, string macroLibraryFile)
        {
            string fix = FileUtils.FixPath(fileName, directoryBase);
            StringBuilder output = new StringBuilder();

            if (library)
            {
                output.AppendFormat("incsrc \"{1}{0}\" : ", Program.LabelLibraryFile, fix);
            }
            output.AppendFormat("incsrc \"{1}{0}\" : ", macroLibraryFile, fix);
            output.Append("freecode cleaned : ");
            output.AppendLine("print \"_startl \", pc");
            output.AppendLine(input);
            output.AppendLine("print \"_endl \", pc");
            return output.ToString();
        }
    }
}
