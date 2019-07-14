using System;
using System.IO;

// This controls ROM operations
// Read/Write, RATS, FreeSpace Detection,
// etc.


namespace UberASMTool
{
    class ROM
    {
        const int maxRomSize = 8389120;     // 8mb + header
        const int minRomSize = 524288;      // 512kb
        const int minRomExSize = 1048576;   // 1mb

        public byte[] romData;
		public byte[] header;

        public bool canOperate;
        public ushort romType;
        public bool containsHeader;
        public string romLocation;
        public int romSize;
        public bool sa1;

        public ROM(string filename)
        {
            this.romData = File.ReadAllBytes(filename);
            this.canOperate = false;
            this.romType = 0;
            this.containsHeader = false;
            this.romLocation = filename;
        }

        public void Init()
        {
            int romLength = romData.Length;

            if (romLength > maxRomSize) throw new Exception("ROM too large!");
            if (romLength < minRomSize) throw new Exception("ROM too small for Super Mario World!");
            if (romLength < minRomExSize) throw new Exception("ROM must be expanded to 1MB first!");

            this.containsHeader = (romLength % 0x8000) == 512;

            if ((romLength % 0x8000) != 0 && !this.containsHeader)
                throw new Exception("Invalid ROM block align! ROM corrupted?");

            // 21 bytes long
            // aaaaaaaaaaa123456789012345678901
            string smw = "SUPER MARIOWORLD     ";

            int position = 0x7FC0 + (this.containsHeader ? 512 : 0);

            foreach (char character in smw)
            {
                if (romData[position++] != character)
                    throw new Exception("This isn't a Super Mario World ROM!");
            }

            romType &= 0xffff;
            romType |= romData[position++];
            romType |= (ushort)(romData[position++] << 8);

            romSize = romLength - (this.containsHeader ? 512 : 0);
			header = new byte[512];

            if (this.containsHeader)
            {
                byte[] dupe = new byte[romSize];
				Array.Copy(romData, 0, header, 0, 512);
                Array.Copy(romData, 512, dupe, 0, romSize);
                romData = null;
                romData = dupe;
            }

            canOperate = true;
            sa1 = (romType & 255) == 0x23;
        }

        public void Close()
        {
            romData = null;
            romType = 0;
            canOperate = false;
            containsHeader = false;
            romLocation = "";
            romSize = 0;
        }

        public void Save()
        {
            byte[] final = new byte[romSize + (containsHeader ? 512 : 0)];
            Array.Copy(romData, 0, final, containsHeader ? 512 : 0, romSize);
			
			if (containsHeader)
			{
				Array.Copy(header, romData, 512);
			}

            File.WriteAllBytes(romLocation, final);
        }

        public bool WriteBlock(byte[] block, int position)
        {
            try
            {
                if (!this.canOperate) throw new ArgumentNullException();
                Array.Copy(block, 0, romData, position, block.Length);
                block = null;
                return true;
            }

            catch
            {
                return false;
            }
        }

        public int Read24(int pos)
        {
            try
            {
                if (!this.canOperate) throw new ArgumentNullException();
                return romData[pos] | (romData[pos + 1] << 8) | (romData[pos + 2] << 16);
            }

            catch
            {
                return 0;
            }
        }

        public byte[] ReadBlock(int position, int length)
        {
            try
            {
                if (!this.canOperate) throw new ArgumentNullException();
                byte[] output = new byte[length];
                Array.Copy(romData, position, output, 0, length);
                return output;
            }

            catch
            {
                return null;
            }
        }
    }
}
