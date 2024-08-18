using System;
using System.IO;
using Renci.SshNet;
using UFiber.Configurator;
using System.CommandLine;
using System.CommandLine.Invocation;

var rootCommand = new RootCommand("Apply configuration changes to UFiber devices")
{
    new Option<string>(
        "--host",
        getDefaultValue: () => "192.168.1.1",
        "IP or hostname of the target UFiber device."),
    new Option<string>(
        "--user",
        getDefaultValue: () => "ubnt",
        "SSH user name."),
    new Option<string>(
        "--pw",
        getDefaultValue: () => "ubnt",
        "SSH password."),
    new Option<int>(
        "--port",
        getDefaultValue: () => 22,
        "SSH port of the target UFiber device."),
    new Option<bool>(
        "--dry-run",
        "Don't apply the patched file to the target UFiber device. (i.e. dry-run)"),
    new Option<string>(
        "--slid",
        "The SLID (or PLOAM Password).", ArgumentArity.ZeroOrOne),
    new Option<string>(
        "--vendor",
        "4-digit Vendor Id (e.g. HWTC, MTSC, etc.). Combined with --serial, a GPON Serial Number is built.", ArgumentArity.ZeroOrOne),
    new Option<string>(
        "--serial",
        "8-digit (e.g. 01234567) serial number or 16-digit (e.g. 41-4C-43-4C-xx-xx-xx-xx) HEX serial number. Combined with --vendor, a GPON Serial Number is built. Note: If a 16-digit HEX value is provided, the first 4 bytes (8 digits) will replace whatever value was passed to Vendor Id with '--vendor'.", ArgumentArity.ZeroOrOne),
    new Option<string>(
        "--mac",
        "The desired MAC address to clone.", ArgumentArity.ZeroOrOne),
    new Option<string>(
        "--restore",
        "Restore a previous version of the firmware.", ArgumentArity.ZeroOrOne)
};

SshClient GetSSHClient(string userName, string password, string host, int port = 22)
{
    var client = new SshClient(host, port, userName, password);
    client.Connect();
    return client;
}

ScpClient GetSCPClient(string userName, string password, string host, int port = 22)
{
    var client = new ScpClient(host, port, userName, password);
    client.Connect();
    return client;
}

rootCommand.Handler = CommandHandler
    .Create<string, string, string, int, bool, string, string, string, string, string>(
        (host, user, pw, port, dryRun, slid, vendor, serial, mac, restore) =>
        {
            var fwToRestore = restore; 
            if (string.IsNullOrWhiteSpace(host))
            {
                Console.Error.WriteLine("Host is a required parameter and can't be empty.");
                Environment.ExitCode = -1;
                return;
            }

            SshClient ssh = default!;
            ScpClient scp = default!;

            try
            {
                // Connect to SSH and SCP
                ssh = GetSSHClient(user, pw, host, port);
                scp = GetSCPClient(user, pw, host, port);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Unable to connect to the target UFiber device. Please check the connection parameters and try again. Error: {ex.Message}");
                Environment.ExitCode = -1;
                return;
            }

            var imgName = $"fw-{DateTime.UtcNow.ToString("ddMMyyyy-hhmmss")}.bin";

            // Dump the image file
            var cmd = ssh.RunCommand($"cat /dev/mtdblock3 > /tmp/{imgName}");
            if (cmd.ExitStatus != 0)
            {
                Console.Error.WriteLine($"Failute to dump the image file. Error: {cmd.Error}");
                Environment.ExitCode = cmd.ExitStatus;
                return;
            }

            const string localDumps = "./dumps";

            if (!Directory.Exists(localDumps))
            {
                Directory.CreateDirectory(localDumps);
            }

            // Download the dump
            try
            {
                scp.Download($"/tmp/{imgName}", new DirectoryInfo(localDumps));
                ssh.RunCommand($"rm /tmp/{imgName}");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failure downloading original image file from the UFiber device. Error: {ex.Message}.");
                Environment.ExitCode = -1;
                return;
            }

            if (!string.IsNullOrWhiteSpace(fwToRestore))
            {
                const string targetFileToRestore = "/tmp/restore.bin";
                Console.WriteLine("Uploading original file to the target UFiber device...");
                try
                {
                    scp.Upload(new FileInfo(fwToRestore), targetFileToRestore);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Failure uploading original image file to the UFiber device. Error: {ex.Message}.");
                    Environment.ExitCode = -1;
                    return;
                }
                Console.WriteLine("Uploaded!");
                Console.WriteLine("### Applying original file on the target UFiber device...");
                cmd = ssh.RunCommand($"dd if={targetFileToRestore} of=/dev/mtdblock3 && rm {targetFileToRestore}");
                if (cmd.ExitStatus != 0)
                {
                    Console.Error.WriteLine($"Failure to apply original image file. Error: {cmd.Error}");
                    Environment.ExitCode = cmd.ExitStatus;
                    return;
                }
                Console.WriteLine("### Applied patch! Please reboot your UFiber device to load the new image.");
                return;
            }

            var ram = new NVRAM(File.ReadAllBytes(Path.Combine(localDumps, imgName)));
            Console.WriteLine("### Original Image ###");
            Console.WriteLine(ram);

            Console.WriteLine($"### Patching {imgName}...");

            if (!string.IsNullOrWhiteSpace(vendor) && string.IsNullOrWhiteSpace(serial))
            {
                Console.Error.WriteLine($"To set the GPON Serial Number, you must pass both --vendor and --serial. You can skip the --vendor if you provide the full serial as HEX to the --serial.");
                Environment.ExitCode = -1;
                return;
            }
            else if (!string.IsNullOrWhiteSpace(vendor) && !string.IsNullOrWhiteSpace(serial))
            {
                ram.SetGponId(vendor);
                ram.SetGponSerialNumber(serial);
            }
            else if (string.IsNullOrWhiteSpace(vendor) && !string.IsNullOrWhiteSpace(serial))
            {
                ram.SetGponSerialNumber(serial);
            }

            if (!string.IsNullOrWhiteSpace(mac))
            {
                ram.SetBaseMacAddress(mac);
            }

            if (!string.IsNullOrWhiteSpace(slid))
            {
                ram.SetGponPassword(slid);
            }

            var patched = ram.CompletePatch();

            const string localPatched = "./patched";

            if (!Directory.Exists(localPatched))
            {
                Directory.CreateDirectory(localPatched);
            }

            var patchedFileName = $"patched-{imgName}";
            File.WriteAllBytes(Path.Combine(localPatched, patchedFileName), patched);
            Console.WriteLine($"### Patched {imgName}!");
            Console.WriteLine(ram);

            if (!dryRun)
            {
                Console.WriteLine("Uploading patched file to the target UFiber device...");
                try
                {
                    scp.Upload(new FileInfo(Path.Combine(localPatched, patchedFileName)), $"/tmp/{patchedFileName}");
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Failure uploading patched image file to the UFiber device. Error: {ex.Message}.");
                    Environment.ExitCode = -1;
                    return;
                }
                Console.WriteLine("Uploaded!");
                Console.WriteLine("### Applying patched file on the target UFiber device...");
                cmd = ssh.RunCommand($"dd if=/tmp/{patchedFileName} of=/dev/mtdblock3 && rm /tmp/{patchedFileName}");
                if (cmd.ExitStatus != 0)
                {
                    Console.Error.WriteLine($"Failure to apply patched image file. Error: {cmd.Error}");
                    Environment.ExitCode = cmd.ExitStatus;
                    return;
                }
                Console.WriteLine("### Applied patch! Please reboot your UFiber device to load the new image.");
            }
            else
            {
                Console.WriteLine($"### Dry-run completed. The patched file can be found at '{Path.Combine(localDumps, patchedFileName)}'.");
            }
        });

return rootCommand.Invoke(args);












