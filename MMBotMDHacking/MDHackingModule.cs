using System.Diagnostics;
using System.IO;
using MMBot;
using System.Globalization;

namespace MMBotMDHacking
{
    public class MDHackingModule : BotModule
    {
        public MDHackingModule() { }
        public override void Shutdown() { }

        void Asm68kCommand(IRC IrcObject, string channel, string user, string command)
        {
            ChangeDirectory();
            File.WriteAllText("tmp.asm", '\t' + command);
            Process asm68k = Process.Start(new ProcessStartInfo("asm68k.exe", "/k /p /o ae- tmp.asm, tmp.bin") { UseShellExecute = false, CreateNoWindow = true });
            asm68k.WaitForExit();
            if (File.Exists("tmp.bin"))
                IrcObject.WriteMessage(Module1.BytesToString(File.ReadAllBytes("tmp.bin")), channel);
            else
                IrcObject.WriteMessage("Code could not be assembled!", channel);
            RestoreDirectory();
        }

        void VdpcalcCommand(IRC IrcObject, string channel, string user, string command)
        {
            string[] Command = command.Split(' ');
            uint vdpcom;
            ushort vdpaddr = 0;
            byte vdpmode = 0;
            string vdpmsg = string.Empty;
            int vdpwrite = 0;
            if (Command.Length > 1)
            {
                switch (Command[1].ToLowerInvariant().Strip())
                {
                    case "read":
                        vdpwrite = 0;
                        break;
                    case "write":
                        vdpwrite = 1;
                        break;
                    case "dma":
                        vdpwrite = 2;
                        break;
                }
                switch (Command[0].ToLowerInvariant().Strip())
                {
                    case "vram":
                        vdpmode = new byte[] { 0x0, 0x1, 0x21 }[vdpwrite];
                        break;
                    case "cram":
                        vdpmode = new byte[] { 0x8, 0x3, 0x23 }[vdpwrite];
                        break;
                    case "vsram":
                        vdpmode = new byte[] { 0x4, 0x5, 0x25 }[vdpwrite];
                        break;
                }
                vdpaddr = ushort.Parse(Command[2].Strip().TrimStart('$'), NumberStyles.HexNumber);
                vdpcom = (uint)((vdpaddr & 0x3FFF) << 16);
                vdpcom |= (uint)((vdpaddr & 0xC000) >> 14);
                vdpcom |= (uint)((vdpmode & 0x3) << 30);
                vdpcom |= (uint)((vdpmode & 0x3C) << 2);
                IrcObject.WriteMessage("$" + vdpcom.ToString("X8"), channel);
            }
            else
            {
                vdpcom = uint.Parse(Command[0].Strip().TrimStart('$'), NumberStyles.HexNumber);
                vdpaddr = (ushort)((vdpcom & 0x3FFF0000) >> 16);
                vdpaddr |= (ushort)((vdpcom & 0x3) << 14);
                vdpmode = (byte)((vdpcom & 0xC0000000) >> 30);
                vdpmode |= (byte)((vdpcom & 0xF0) >> 2);
                switch (vdpmode)
                {
                    case 0x0:
                        vdpmsg = "VRAM Read from ";
                        break;
                    case 0x1:
                        vdpmsg = "VRAM Write to ";
                        break;
                    case 0x21:
                        vdpmsg = "VRAM DMA to ";
                        break;
                    case 0x8:
                        vdpmsg = "CRAM Read from ";
                        break;
                    case 0x3:
                        vdpmsg = "CRAM Write to ";
                        break;
                    case 0x23:
                        vdpmsg = "CRAM DMA to ";
                        break;
                    case 0x4:
                        vdpmsg = "VSRAM Read from ";
                        break;
                    case 0x5:
                        vdpmsg = "VSRAM Write to ";
                        break;
                    case 0x25:
                        vdpmsg = "VSRAM DMA to ";
                        break;
                    default:
                        vdpmsg = "Unknown command $" + vdpmode.ToString("X2") + ", address ";
                        break;
                }
                vdpmsg += "$" + vdpaddr.ToString("X4");
                IrcObject.WriteMessage(vdpmsg, channel);
            }
        }
    }
}