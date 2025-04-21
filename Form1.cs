using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace cubrid_query_client_250417
{
    public partial class Form1 : Form
    {
        string ServerIp = Properties.Settings.Default.ServerIp;
        string ServerPort = Properties.Settings.Default.ServerPort;
        public Form1()
        {
            InitializeComponent();
            //Console.OutputEncoding = Encoding.UTF8;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string query = textBox1.Text;

            if (string.IsNullOrWhiteSpace(query))
            {
                MessageBox.Show("쿼리를 입력하세요.");
                return;
            }

            try
            {
                // +250421 jwshin ServerPort int로 변환
                using (var client = new TcpClient(ServerIp, int.Parse(ServerPort)))
                {
                    var stream = client.GetStream();
                    byte[] queryBytes = Encoding.UTF8.GetBytes(query);
                    stream.Write(queryBytes, 0, queryBytes.Length);

                    byte[] buffer = new byte[8192];
                    StringBuilder sb = new StringBuilder();
                    int bytes;

                    while ((bytes = stream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        string part = Encoding.UTF8.GetString(buffer, 0, bytes);
                        sb.Append(part);
                        if (part.Contains("<EOF>"))
                            break;
                    }

                    // <EOF> 제거
                    string result = sb.ToString().Replace("<EOF>", "");
                    textBox2.Text = result;
                }
            }
            catch (Exception ex)
            {
                textBox2.Text = "ERROR: " + ex.Message;
            }
        }
    

    }
}
