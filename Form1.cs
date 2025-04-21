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
                // +250421 jwshin ServerPort int로 변환 test2
                // +250421 jwshin ServerPort int로 변환 test3
                using (var client = new TcpClient(ServerIp, int.Parse(ServerPort)))
                {
                    var stream = client.GetStream();
                    byte[] queryBytes = Encoding.UTF8.GetBytes(query);
                    stream.Write(queryBytes, 0, queryBytes.Length);

                    // 총 크기 수신 (4바이트)
                    byte[] sizeBuffer = new byte[4];
                    stream.Read(sizeBuffer, 0, 4);
                    int totalSize = BitConverter.ToInt32(sizeBuffer, 0);


                    byte[] buffer = new byte[8192];
                    StringBuilder sb = new StringBuilder();
                    int bytesReceived = 0;
                    int bytes;

                    /*
                    while ((bytes = stream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        string part = Encoding.UTF8.GetString(buffer, 0, bytes);
                        sb.Append(part);
                        if (part.Contains("<EOF>"))
                            break;
                    }
                    */

                    // ProgressBar 초기화
                    progressBar1.Minimum = 0;
                    progressBar1.Maximum = 100;
                    progressBar1.Value = 0;

                    while (bytesReceived < totalSize)
                    {
                        bytes = stream.Read(buffer, 0, buffer.Length);
                        if (bytes == 0) break;

                        string part = Encoding.UTF8.GetString(buffer, 0, bytes);
                        sb.Append(part);
                        bytesReceived += bytes;

                        // 진행률 계산
                        int percent = (int)((bytesReceived / (float)totalSize) * 100);
                        progressBar1.Value = Math.Min(percent, 100);
                        label4.Text = $"{percent}% ({bytesReceived / 1024} KB / {totalSize / 1024} KB)";
                        Application.DoEvents();  // UI 업데이트 강제
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
