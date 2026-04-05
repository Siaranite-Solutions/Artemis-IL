using System;
using System.Collections.Generic;
using System.Text;

namespace Artemis_IL.StandardLib
{
    public static class SoftwareInterrupts
    {
        public static VM ParentVM;

        public static void HandleInterrupt(int command)
        {
            // SWI 0x01 — String utilities
            // AL selects operation; registers X, Y, B used for address/length.
            if (command == 0x01)
            {
                if (ParentVM.AL == 0x01)
                {
                    // Strlen: count bytes at address X until a zero byte; store length in B.
                    int addr = ParentVM.X;
                    int limit = ParentVM.ram.memory.Length;
                    int len = 0;
                    while (addr + len < limit && ParentVM.ram.memory[addr + len] != 0x00)
                        len++;
                    ParentVM.SetSplit('B', len);
                }
                else if (ParentVM.AL == 0x02)
                {
                    // Strcpy: copy B bytes from address X to address Y.
                    int src = ParentVM.X;
                    int dst = ParentVM.Y;
                    int count = ParentVM.GetSplit('B');
                    int limit = ParentVM.ram.memory.Length;
                    if (src < 0 || src + count > limit || dst < 0 || dst + count > limit)
                    {
                        Globals.console.WriteLine("SWI 0x01: strcpy address out of range\nHalting for protection of data");
                        ParentVM.Halt();
                        return;
                    }
                    byte[] data = ParentVM.ram.GetSection(src, count);
                    ParentVM.ram.SetSection(dst, data);
                }
                else
                {
                    Globals.console.WriteLine("SWI 0x01: unknown mode 0x" + ParentVM.AL.ToString("X2") + "\nHalting for protection of data");
                    ParentVM.Halt();
                }
            }
            else
            {
                Globals.console.WriteLine("Undocumented SWI: " + command + "\nHalting for protection of data");
                ParentVM.Halt();
            }
        }
    }
}

