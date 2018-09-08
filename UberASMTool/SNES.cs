using System;

namespace UberASMTool
{
    static class SNES
    {
        public static int[] snesbanks = new int[] {
            // banks to be mapped for each megabyte
            // sa1rom : 0x00, 0x20, 0x80, 0xA0
            // lorom  : 0x00, 0x20, 0x40, 0x60
            // fastrom: 0x80, 0xA0, 0xC0, 0xE0
            0x80, 0xA0, 0xC0, 0xE0,
            0, 0, 0, 0, // 8mb roms
        };

        public static int[] pcbanks = new int[] {
            // inversed to return to pc address
            0, // banks 00-1F
            1, // banks 20-3F
            2, // banks 40-5F
            3, // banks 60-7F
            0, // banks 80-9F
            1, // banks A0-BF
            2, // banks C0-DF
            3, // banks E0-FF
        };

        public static void DetectAddressing(int romheader)
        {
            switch (romheader)
            {
                case 0x23:
                    // SA-1
                    snesbanks[0] = 0x00;
                    snesbanks[1] = 0x20;
                    snesbanks[2] = 0x80;
                    snesbanks[3] = 0xA0;
                    snesbanks[4] = 0xA0; // map rest to 0xA0, but i'm not sure...
                    snesbanks[5] = 0xA0;
                    snesbanks[6] = 0xA0;
                    snesbanks[7] = 0xA0;
                    pcbanks[0] = 0; // 0-1F
                    pcbanks[1] = 1; // 20-3F
                    pcbanks[2] = 0; // 40-5F \ this is SA-1/SNES RAM
                    pcbanks[3] = 0; // 60-7F /
                    pcbanks[4] = 2; // 80-9F
                    pcbanks[5] = 3; // A0-BF <-- May variable.
                    pcbanks[6] = 0; // C0-DF \ this is SA-1 HiROM
                    pcbanks[7] = 0; // E0-FF /
                    break;

				default:
                    // FastROM
                    snesbanks[0] = 0x80;
                    snesbanks[1] = 0xA0;
                    snesbanks[2] = 0xC0;
                    snesbanks[3] = 0xE0;
                    snesbanks[4] = 0x00; // a... FastROM don't go up to 8MB...
                    snesbanks[5] = 0x00;
                    snesbanks[6] = 0x00;
                    snesbanks[7] = 0x00;
                    pcbanks[0] = 0;
                    pcbanks[1] = 1;
                    pcbanks[2] = 2;
                    pcbanks[3] = 3;
                    pcbanks[4] = 0;
                    pcbanks[5] = 1;
                    pcbanks[6] = 2;
                    pcbanks[7] = 3;
                    break;
            }
        }

		public static int ToPC(int snes)
		{
			return ((pcbanks[(snes & 0xe00000) >> 21]) << 20) | ((snes & 0x1f0000) >> 1) | (snes & 0x7fff);
		}

		public static int ToPCHeadered(int snes, bool header)
		{
			int val = ((pcbanks[(snes & 0xe00000) >> 21]) << 20) | ((snes & 0x1f0000) >> 1) | (snes & 0x7fff);
			return val + (header ? 512 : 0);
		}

        public static int FromPC(int pc)
        {
            return (snesbanks[(pc & 0xe00000) >> 20] << 16) | ((pc & 0x1f8000) << 1) | (pc & 0x7fff) | 0x8000;
        }
    }
}
