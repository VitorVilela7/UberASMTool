using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UberASMTool
{
    class Logger
    {
        private StringBuilder aggregator;

        public static Logger GetLogger()
        {
            return new Logger(new StringBuilder());
        }

        private Logger(StringBuilder baseBuilder)
        {
            this.aggregator = baseBuilder;
        }

        public string GetOutput()
        {
            return aggregator.ToString();
        }

        public void Error(string description, int line = -1)
        {
            WriteLog(description, line, true);
        }

        public void Warning(string description, int line = -1)
        {
            WriteLog(description, line, false);
        }

        public void WriteLog(string description, int line = -1, bool error = true)
        {
            if (line == -1)
            {
                this.aggregator.AppendLine($"{(error ? "Error" : "Warning")}: {description}");
            }
            else
            {
                this.aggregator.AppendLine($"{(error ? "Error" : "Warning")}: line {line + 1} - {description}");
            }
        }

    }
}
