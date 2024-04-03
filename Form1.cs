using Newtonsoft.Json;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;

namespace HW_4_2_server_NP
{
    public partial class Form1 : Form
    {
        private TcpListener listener;
        private TcpListener listenerConnection;
        private List<TcpClient> clients = new List<TcpClient>();
        private List<TcpClient> forMessage = new List<TcpClient>();
        private List<string> userlists = new List<string>();
        private object locker = new object();
        public Form1()
        {
            InitializeComponent();
            listener = new TcpListener(IPAddress.Any, 49220);
            listenerConnection = new TcpListener(IPAddress.Any, 49200);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            listenerConnection.Start();
            StartRecievingClients();
            listener.Start();
            StartResending();
        }

        async void StartRecievingClients()
        {
            await Task.Run(() => 
            {
                while(true)
                {
                    TcpClient client = listenerConnection.AcceptTcpClient();
                    lock(locker)
                    {
                        clients.Add(client);
                    }
                    ClientsControl(client);
                }
                
            });
        }
        async void ClientsControl(TcpClient client)
        {
            await Task.Run(() => 
            {
                NetworkStream stream = client.GetStream();
                string user = null;
                while (client.Connected)
                {
                    byte[] buffer = new byte[1024];
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                    {
                        break;
                    }
                    user = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                    userlists.Add(user);
                    string jsonstring = JsonConvert.SerializeObject(userlists);
                    foreach(var cl in clients)
                    {
                        NetworkStream stream1 = cl.GetStream();
                        byte[] buffer1 = Encoding.ASCII.GetBytes(jsonstring);
                        stream1.Write(buffer1, 0, buffer1.Length);
                    }
                        
                    
                }
                clients.Remove(client);
                userlists.Remove(user);
                //string jsonstringdeleted = JsonConvert.SerializeObject(userlists);
                //    byte[] buffer2 = Encoding.ASCII.GetBytes(jsonstringdeleted);
                //    stream.Write(buffer2, 0, buffer2.Length);
                stream.Close();
                client.Close();
                

            });
        }
        async void StartResending()
        {
            await Task.Run(() =>
            {
                while (true)
                {
                    TcpClient client = listener.AcceptTcpClient();
                    forMessage.Add(client);
                    HandleClients(client);
                }
            });
        }
        async void HandleClients(TcpClient client)
        {
            await Task.Run(() =>
            {
                NetworkStream stream = client.GetStream();
                while(client.Connected)
                {
                    byte[] buffer = new byte[1024];
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                    {
                        break;
                    }
                    string message = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                    foreach (TcpClient cl in forMessage)
                    {
                        NetworkStream stream1 = cl.GetStream();
                        byte[] buffer1 = Encoding.ASCII.GetBytes(message);
                        stream1.Write(buffer1, 0, buffer1.Length);
                    }

                }
                stream.Close();
                client.Close();
                clients.Remove(client);
            });
        }
        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            foreach (TcpClient client in clients) 
            {
            client.Close();
            }
            clients.Clear();
            listener.Stop();
        }
    }
}
