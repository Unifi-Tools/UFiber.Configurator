using Renci.SshNet;

namespace UFiber.Configurator.Clients
{
    public class Clients
    {
        public ScpClient GetSCPClient(string userName, string password, string host, int port = 22)
        {
            var client = new ScpClient(host, port, userName, password);
            client.Connect();
            return client;
        }

        public SshClient GetSSHClient(string userName, string password, string host, int port = 22)
        {
            var client = new SshClient(host, port, userName, password);
            client.Connect();
            return client;
        }
    }
}
