using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UberASMTool
{
    class DataCollector
    {
        private readonly ROM rom;
        private readonly List<int> freespacePointerList;

        public DataCollector(ROM romData)
        {
            rom = romData;
            freespacePointerList = new List<int>();
        }

        public int[] GetCollectedPointers()
        {
            int[] list = freespacePointerList.ToArray();
            freespacePointerList.Clear();

            return list;
        }

        public void CheckPreviousUberData()
        {
            //gamemode - $009322+1
            //levelASM - $00A242+1
            //overworld - $00A1C3+1
            //global - $00804E+1

            CleanPointerTable(0x1323, 256, false);
            CleanPointerTable(0x2243, 512, true);
            CleanPointerTable(0x21C4, 7, false);

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
                }
                ptr -= 3;
            }
        }

        private bool CheckForUberSignature(int ptr)
        {
            var str = rom.ReadBlock(ptr, 4);
            return (str[0] == (byte)'u' && str[1] == (byte)'b' && str[2] == (byte)'e' && str[3] == (byte)'r');
        }

        private int CheckPointer(int offset)
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

        private void CleanPointerTable(int offset, int pointerCount, bool ext)
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
                }
            }
        }
    }
}
