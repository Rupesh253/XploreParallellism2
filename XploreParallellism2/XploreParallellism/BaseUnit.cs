using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Security.Policy;
using System.Text.Json.Nodes;
using System.Threading.Channels;

namespace XploreParallellism2 {
    [SetUpFixture]
    public class BaseUnit {
        public static string configFile = @"C:\Users\git\source\XploreParallellism2\Config2.json";
        public int number1;
        public int number2;
        public string appiumPID;
        public Dictionary<string, string> emu;
        public string emulatorName = null;
        public string appiumPort = null;
        //public BaseUnit(int number1, int number2) {
        //    this.number1 = number1;
        //    this.number2 = number2;
        //}
        [OneTimeSetUp]
        public void OneTimeSetUp() {
            emu = new Dictionary<string, string>();
            Console.WriteLine("BaseUnit.OneTimeSetUp");
            JObject jo = JObject.Parse(File.ReadAllText(configFile));
            emulatorName = jo["emulatorName"].ToString();
            appiumPort = jo["appiumPort"].ToString();
            string appiumCommand = jo["appiumCMD"].ToString().Replace("{0}", @"C:\Users\git\source\XploreParallellism2\appiumServerLogs.json").Replace("{1}", appiumPort);
            ProcessStartInfo appiumStartInfo = new ProcessStartInfo {
                CreateNoWindow = true,
                FileName = "cmd.exe",
                Arguments = appiumCommand,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false
            };
            Process appiumProcess = new Process();
            appiumProcess.StartInfo = appiumStartInfo;
            appiumProcess.Start();
            appiumProcess.BeginOutputReadLine();
            appiumProcess.BeginErrorReadLine();
            appiumProcess.StandardInput.WriteLine(appiumCommand);
            Thread.Sleep(15000); //Min 15 sec for starting the appium server 


            string scriptArguments = $"netstat -aon | findstr \":{appiumPort}\"";
            var processStartInfo = new ProcessStartInfo("powershell.exe", scriptArguments);
            processStartInfo.RedirectStandardOutput = true;
            processStartInfo.RedirectStandardError = true;

            using var process1 = new Process();
            process1.StartInfo = processStartInfo;
            process1.Start();
            string output = process1.StandardOutput.ReadToEnd();
            if (output.Contains("LISTENING")) {
                appiumPID = output.Split("LISTENING")[1].Trim();
            } else {
                Console.WriteLine($"Appium server didn't started on port {appiumPort}");
            }
            process1.Kill();

            string emulatorCommand = jo["emulatorCMD"].ToString().Replace("{0}", emulatorName);
            ProcessStartInfo emulatorStartInfo = new ProcessStartInfo {
                CreateNoWindow = true,
                FileName = "cmd.exe",
                Arguments = emulatorCommand,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false
            };
            Process emulatorProcess = new Process();
            emulatorProcess.StartInfo = emulatorStartInfo;
            emulatorProcess.Start();
            emulatorProcess.BeginErrorReadLine();
            emulatorProcess.BeginOutputReadLine();
            emulatorProcess.StandardInput.WriteLine(emulatorCommand);
            Thread.Sleep(70000); //Min 40 seconds for cold boot
            EmulatorStatus();
        }
        public void EmulatorStatus() {
            ProcessStartInfo emuStatusStartInfo = new ProcessStartInfo {
                CreateNoWindow = true,
                FileName = "powershell.exe",
                Arguments = "adb devices",
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false
            };
            Process emuStatusProcess = new Process();
            emuStatusProcess.StartInfo = emuStatusStartInfo;
            emuStatusProcess.Start();

            emuStatusProcess.BeginErrorReadLine();
            emuStatusProcess.StandardInput.WriteLine("adb devices");

            string adbDevices = emuStatusProcess.StandardOutput.ReadToEnd();
            adbDevices = adbDevices.Replace("List of devices attached", "");
            string[] serailsList = adbDevices.Split("\n");
            foreach (string serial in serailsList) {
                if (!string.IsNullOrEmpty(serial.Trim())) {
                    string temp = serial.Replace("device", "").Trim();
                    string scriptArguments = $"adb -s {temp} emu avd name";
                    var processStartInfo = new ProcessStartInfo("powershell.exe", scriptArguments);
                    processStartInfo.RedirectStandardOutput = true;
                    processStartInfo.RedirectStandardError = true;
                    using var process1 = new Process();
                    process1.StartInfo = processStartInfo;
                    process1.Start();
                    string output = process1.StandardOutput.ReadToEnd();
                    string emulatorName = output.Split("\n")[0].Replace("OK", "").Trim(); ;
                    emu.Add(emulatorName, temp);
                }
            }
            string avdStatusCommand = "adb -s {0} emu avd status";
            foreach (var item in emu) {
                if (item.Key == emulatorName) {
                    avdStatusCommand = avdStatusCommand.Replace("{0}", item.Value);
                }
            }
            var avdStatusProcessStartInfo = new ProcessStartInfo("powershell.exe", avdStatusCommand);
            avdStatusProcessStartInfo.RedirectStandardInput = true;
            avdStatusProcessStartInfo.RedirectStandardOutput = true;
            using var avdStatusProcess = new Process();
            avdStatusProcess.StartInfo = avdStatusProcessStartInfo;
            avdStatusProcess.Start();
            string outputavdStatus = avdStatusProcess.StandardOutput.ReadToEnd();
            if (outputavdStatus.Contains("virtual device is running")) {
                Console.WriteLine("Jupiter is running & proceeding with test execution");
            } else { Console.WriteLine("Emulator doesn't started well"); }
        }
        [OneTimeTearDown]
        public void OneTimeTearDown() {
            Console.WriteLine("BaseUnit.OneTimeTearDown");
            string scriptArguments = $"netstat -aon | findstr \":{appiumPort}\"";
            var processStartInfo = new ProcessStartInfo("powershell.exe", scriptArguments);
            processStartInfo.RedirectStandardOutput = true;
            processStartInfo.RedirectStandardError = true;

            using var process1 = new Process();
            process1.StartInfo = processStartInfo;
            process1.Start();
            string output = process1.StandardOutput.ReadToEnd();
            appiumPID = output.Split("LISTENING")[1].Trim();
            ProcessStartInfo emuStatusStartInfo = new ProcessStartInfo {
                CreateNoWindow = true,
                FileName = "cmd.exe",
                Arguments = $"taskkill /F /PID {appiumPID}",
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false
            };
            Process emuStatusProcess = new Process();
            emuStatusProcess.StartInfo = emuStatusStartInfo;
            emuStatusProcess.Start();
            emuStatusProcess.StandardInput.WriteLine($"taskkill /F /PID {appiumPID}");
            string killAvdCommand = "adb -s {0} emu kill";
            foreach (var item in emu) {
                if (item.Key == emulatorName) {
                    killAvdCommand = killAvdCommand.Replace("{0}", item.Value);
                }
            }

            ProcessStartInfo killAvdStartInfo = new ProcessStartInfo {
                FileName = "powershell.exe",
                Arguments = killAvdCommand,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using var killAvdProcess = new Process();
            killAvdProcess.StartInfo = killAvdStartInfo;
            killAvdProcess.Start();
            string output1 = killAvdProcess.StandardOutput.ReadToEnd();
            if (output1.Contains("OK: killing emulator, bye bye")) {
                Console.WriteLine("Killed emulator");
            }
            Console.WriteLine("All done");
            Console.Beep();
        }
        public static void Consume() {
            JObject jo = JObject.Parse(File.ReadAllText(configFile));
            Console.WriteLine($"platformName:{jo["platformName"]}\t platformVersion:{jo["platformVersion"]}\t deviceName:{jo["deviceName"]}");
        }

    }
}