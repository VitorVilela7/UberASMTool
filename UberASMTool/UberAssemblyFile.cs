using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UberASMTool
{
    /*
     * Specialized UberASM Tool assembly file
     */
    class UberAssemblyFile
    {
        public string ProcessedContents { get; set; }
        public string FilePath { get; set; }
        public bool IsLibrary { get; set; }
    }
}
