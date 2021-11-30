using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UberASMTool.Model
{
	class Code
	{
		public Code(string path)
		{
			this.Path = path;
			this.Inserted = false;
			this.Main = -1;
			this.Init = -1;
			this.Nmi = -1;
			this.Load = -1;
		}

		public string Path;
		public bool Inserted;
		public int Main;
		public int Init;
		public int Nmi;
		public int Load;
	}
}
