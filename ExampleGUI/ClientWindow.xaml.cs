using NamedPipeWrapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
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
    public partial class ClientWindow : Window
    {
        private readonly NamedPipeClient<string> _client = new NamedPipeClient<string>(Constants.PIPE_NAME);

        public ClientWindow()
        {
            InitializeComponent();

            _client.ServerMessage += OnServerMessage;
            _client.Disconnected += OnDisconnected;
            _client.Start();
        }


        private void OnServerMessage(NamedPipeConnection<string, string> connection, string message)
        {
            AddLine("Server", message);

        }

        private void OnDisconnected(NamedPipeConnection<string, string> connection)
        {
            AddLine("Disconnected from server", "");
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

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textBoxMessage.Text))
                return;

            _client.PushMessage(textBoxMessage.Text);
            textBoxMessage.Text = "";
        }
    }
}
