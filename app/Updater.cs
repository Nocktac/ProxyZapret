using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace ProxyZapret.Updater
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            try
            {
                if (args.Length == 1 && args[0] == "--self-test")
                {
                    RunSelfTest();
                    Console.WriteLine("ProxyZapret updater self-test passed.");
                    return 0;
                }

                if (args.Length != 4)
                    throw new ArgumentException("Expected: <pid> <downloaded-exe> <target-exe> <restart-args>");

                var processId = Int32.Parse(args[0]);
                var downloaded = Path.GetFullPath(args[1]);
                var target = Path.GetFullPath(args[2]);
                var restartArgs = args[3];
                WaitForExit(processId);
                ReplaceFile(downloaded, target);
                Process.Start(new ProcessStartInfo
                {
                    FileName = target,
                    Arguments = restartArgs,
                    WorkingDirectory = Path.GetDirectoryName(target),
                    UseShellExecute = true
                });
                return 0;
            }
            catch (Exception exception)
            {
                try
                {
                    var runtime = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "runtime");
                    Directory.CreateDirectory(runtime);
                    File.WriteAllText(
                        Path.Combine(runtime, "updater.error.log"),
                        exception.ToString()
                    );
                }
                catch { }
                return 1;
            }
        }

        private static void WaitForExit(int processId)
        {
            try
            {
                using (var process = Process.GetProcessById(processId))
                    process.WaitForExit(30000);
            }
            catch (ArgumentException) { }
        }

        private static void ReplaceFile(string downloaded, string target)
        {
            if (!File.Exists(downloaded))
                throw new FileNotFoundException("Downloaded update is missing.", downloaded);

            var backup = target + ".previous";
            if (File.Exists(backup)) File.Delete(backup);
            if (File.Exists(target)) File.Copy(target, backup, true);

            try
            {
                File.Copy(downloaded, target, true);
                File.Delete(downloaded);
            }
            catch
            {
                if (File.Exists(backup)) File.Copy(backup, target, true);
                throw;
            }
        }

        private static void RunSelfTest()
        {
            var directory = Path.Combine(Path.GetTempPath(), "ProxyZapret-Updater-Test-" + Guid.NewGuid());
            Directory.CreateDirectory(directory);
            try
            {
                var source = Path.Combine(directory, "source.exe");
                var target = Path.Combine(directory, "target.exe");
                File.WriteAllText(source, "new-version");
                File.WriteAllText(target, "old-version");
                ReplaceFile(source, target);
                if (File.ReadAllText(target) != "new-version")
                    throw new InvalidOperationException("Updater did not replace the target file.");
                if (File.ReadAllText(target + ".previous") != "old-version")
                    throw new InvalidOperationException("Updater did not preserve the previous version.");
            }
            finally
            {
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
        }
    }
}
