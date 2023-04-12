using CliFx.Attributes;
using CliFx.Infrastructure;
using System;
using System.Threading.Tasks;
using CliFx;
using Renci.SshNet;
using System.Net.NetworkInformation;

namespace UFiber.Configurator.Commands
{
    //      new Option<string>(
    //        "--host",
    //        getDefaultValue: () => "192.168.1.1",
    //        "IP or hostname of the target UFiber device."),
    //    new Option<string>(
    //        "--user",
    //        getDefaultValue: () => "ubnt",
    //        "SSH user name."),
    //    new Option<string>(
    //        "--pw",
    //        getDefaultValue: () => "ubnt",
    //        "SSH password."),
    //    new Option<int>(
    //        "--port",
    //        getDefaultValue: () => 22,
    //        "SSH port of the target UFiber device."),

    [Command(Description = "Tests connectivity with the device.")]
    public class TestConnection : ICommand
    {
        //[CommandParameter(0, Description = Constants.HostDescription)]
        [CommandOption("host", 'h', Description = Constants.HostDescription)]
        public required string Host { get; init; } = "192.168.1.1"; // Load from appsettings.json

        [CommandOption("user", 'u', Description = Constants.UserDescription)]
        public required string User { get; init; } = "ubnt";

        [CommandOption("password", 'w', Description = Constants.PasswordDescription)]
        public required string Password { get; init; } = "ubnt";

        [CommandOption("port", 'p', Description = Constants.PortDescription)]
        public int Port { get; init; } = 22;

        public ValueTask ExecuteAsync(IConsole console)
        {
            try
            {
                var connectionInfo = new ConnectionInfo(Host, Port, User, new PasswordAuthenticationMethod(User, Password));

                console.Output.WriteLineAsync("Connecting to the device...");
                var ping = new Ping();
                
                console.Output.WriteLineAsync($"Sending ping to the device ({Host})");

                var response = ping.Send(Host);

                if (response.Status == IPStatus.Success)
                {
                    console.Output.WriteLineAsync($"Ping to {Host} [{response.Address}] Successful\n Response delay = {response.RoundtripTime} ms\n");
                }

                using (var client = new SshClient(connectionInfo))
                {
                    client.Connect();
                }
            } catch (Exception ex)
            {
                console.Error.WriteLineAsync($"Unable to connect to the target UFiber device. Please check the connection parameters and try again. Error: {ex.Message}");
            }
            //finally
            //{
            //    console.Output.WriteLineAsync("Connection OK, disconecting.");
            //    ssh.Disconnect();
            //}

            // If the execution is not meant to be asynchronous,
            // return an empty task at the end of the method.
            return default;
        }
    }
}
