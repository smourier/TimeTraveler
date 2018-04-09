using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using TimeTraveler.Utilities;

namespace TimeTraveler
{
    public class Program
    {
        static DateTime _start;
        static TimeSpan _delta;
        static bool _frozen;

        [STAThread]
        static void Main(string[] args)
        {
            if (Debugger.IsAttached)
            {
                SafeMain(args);
                return;
            }

            try
            {
                SafeMain(args);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        static void SafeMain(string[] args)
        {
            Console.WriteLine("TimeTraveler - Copyright (c) 2017-" + DateTime.Now.Year + " Simon Mourier. All rights reserved.");
            Console.WriteLine("Portions of this software are Copyright (c) 2009-" + DateTime.Now.Year + " Tsuda Kageyu.");
            Console.WriteLine("Portions of this software are Copyright (c) 2008-" + DateTime.Now.Year + " Vyacheslav Patkov.");
            Console.WriteLine();
            if (CommandLine.HelpRequested || args.Length < 2)
            {
                Help();
                return;
            }

            string path = Path.GetFullPath(args[0]);
            string time = args[1];
            bool delta = CommandLine.GetArgument("tt_delta", false);
            _frozen = CommandLine.GetArgument("tt_frozen", false);

            if (delta)
            {
                if (!TimeSpan.TryParse(time, out TimeSpan ts))
                {
                    Console.WriteLine("'" + time + "' is not a valid time span.");
                    return;
                }

                _start = DateTime.Now + ts;
            }
            else
            {
                if (!DateTime.TryParse(time, out _start))
                {
                    Console.WriteLine("'" + time + "' is not a valid date.");
                    return;
                }
            }

            Console.WriteLine("Target Application Path : " + path);
            Console.WriteLine("Start Date              : " + _start);
            Console.WriteLine("Frozen mode             : " + _frozen);
            Console.WriteLine();

            _delta = DateTime.Now - _start;

            var appArgs = new List<string>();
            for (int i = 2; i < args.Length; i++)
            {
                var arg = args[i];
                if (arg.IndexOf("tt_delta", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    arg.IndexOf("tt_frozen", StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;
            }

            using (var hook = new MinHook())
            {
                var getDate = hook.CreateHook<GetSystemTimeAsFileTime>("kernel32.dll", "GetSystemTimeAsFileTime", GetSystemTimeAsFileTimeHook);
                hook.EnableHook(getDate);

                var setup = new AppDomainSetup();
                var domain = AppDomain.CreateDomain(Path.GetFileName(path), null, setup);
                domain.ExecuteAssembly(path, appArgs.ToArray());
                hook.DisableHook(getDate);
            }
        }

        static void GetSystemTimeAsFileTimeHook(out long lpSystemTimeAsFileTime)
        {
            if (_frozen)
            {
                lpSystemTimeAsFileTime = _start.ToFileTime();
            }
            else
            {
                GetSystemTimePreciseAsFileTime(out long st);
                var real = DateTime.FromFileTime(st);
                var dt = real - _delta;
                lpSystemTimeAsFileTime = dt.ToFileTime();
            }
            Console.WriteLine(DateTime.FromFileTime(lpSystemTimeAsFileTime));
        }

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate void GetSystemTimeAsFileTime(out long lpSystemTimeAsFileTime);

        [DllImport("kernel32")]
        private static extern void GetSystemTimePreciseAsFileTime(out long lpSystemTimeAsFileTime);

        static void Help()
        {
            Console.WriteLine(Assembly.GetEntryAssembly().GetName().Name.ToUpperInvariant() + " <input exe path> <value> [options]");
            Console.WriteLine();
            Console.WriteLine("Description:");
            Console.WriteLine("    This tool is used to run a .NET application and make it believe it runs in another time.");
            Console.WriteLine();
            Console.WriteLine("    <value> defines what's the time when application starts.");
            Console.WriteLine("    All unknown parameters will be passed to the application.");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("    /tt_frozen    time is frozen to what's set by <value>.");
            Console.WriteLine("    /tt_delta     <value> is a delta timespan from today.");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine();
            Console.WriteLine("    " + Assembly.GetEntryAssembly().GetName().Name.ToUpperInvariant() + " c:\\mypath\\myproject.exe " + new DateTime(1966, 3, 24).Date.ToString("d"));
            Console.WriteLine();
            Console.WriteLine("        Starts myproject.exe the 24th of march 1966.");
            Console.WriteLine();
            Console.WriteLine("    " + Assembly.GetEntryAssembly().GetName().Name.ToUpperInvariant() + " c:\\mypath\\myproject.exe -60 /tt_delta /tt_frozen customarg1 -customarg2");
            Console.WriteLine();
            Console.WriteLine("        Runs myproject.exe now - 60 days (" + DateTime.Now.AddDays(-60) + ") and freeze time.");
            Console.WriteLine("        myproject.exe will be started using 'customarg1' and '-customarg2' as arguments.");
            Console.WriteLine();
        }
    }
}
