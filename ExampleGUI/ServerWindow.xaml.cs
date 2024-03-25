using NamedPipeWrapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ExampleGUI
{
    public partial class ServerWindow : Window
    {
        private readonly NamedPipeServer<string> _server = new NamedPipeServer<string>(Constants.PIPE_NAME);
        private readonly ISet<string> _clients = new HashSet<string>();
        public ServerWindow()
        {
            InitializeComponent();

            _server.ClientConnected += OnClientConnected;
            _server.ClientDisconnected += OnClientDisconnected;
            _server.ClientMessage += (client, message) => AddLine(client.Name, message);
            _server.Start();
        }
        private void OnClientConnected(NamedPipeConnection<string, string> connection)
        {
            _clients.Add(connection.Name);
            AddLine(connection.Name, "connected!");
            UpdateClientList();
            connection.PushMessage("Welcome!  You are now connected to the server.");
        }

        private void OnClientDisconnected(NamedPipeConnection<string, string> connection)
        {
            _clients.Remove(connection.Name);
            AddLine(connection.Name, "disconnected!");
            UpdateClientList();
        }

        private void AddLine(string header, string content)
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                var paragraph = new Paragraph();
                paragraph.Inlines.Add(new Bold(new Run(header)));
                paragraph.Inlines.Add(new Run(" : "));
                paragraph.Inlines.Add(new Run(content));
                richTextBoxMessages.Document.Blocks.Add(paragraph);
                //richTextBoxMessages.AppendText(Environment.NewLine + "<div>" + content + "</div>");
            }));
        }

        private void UpdateClientList()
        {
            this.Dispatcher.Invoke(() =>
            {
                UpdateClientListImpl();
            });
        }

        private void UpdateClientListImpl()
        {
            listBoxClients.Items.Clear();
            foreach (var client in _clients)
            {
                listBoxClients.Items.Add(client);
            }
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textBoxMessage.Text))
                return;

            _server.PushMessage(textBoxMessage.Text);
            textBoxMessage.Text = "";
        }
    }
}
