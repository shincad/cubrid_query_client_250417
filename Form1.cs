using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
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

        private async void button1_Click(object sender, EventArgs e)
        {
            string query = textBox1.Text;

            if (string.IsNullOrWhiteSpace(query))
            {
                MessageBox.Show("쿼리를 입력하세요.");
                return;
            }

            try
            {
                using (var client = new TcpClient())
                {
                    await client.ConnectAsync(ServerIp, int.Parse(ServerPort));
                    var stream = client.GetStream();
                    byte[] queryBytes = Encoding.UTF8.GetBytes(query);
                    await stream.WriteAsync(queryBytes, 0, queryBytes.Length);

                    // 총 크기 수신 (4바이트)
                    byte[] sizeBuffer = new byte[4];
                    await stream.ReadAsync(sizeBuffer, 0, 4);
                    int totalSize = BitConverter.ToInt32(sizeBuffer, 0);

                    byte[] buffer = new byte[8192];
                    StringBuilder sb = new StringBuilder();
                    int bytesReceived = 0;

                    // ProgressBar 초기화
                    progressBar1.Minimum = 0;
                    progressBar1.Maximum = 100;
                    progressBar1.Value = 0;

                    while (bytesReceived < totalSize)
                    {
                        int bytes = await stream.ReadAsync(buffer, 0, buffer.Length);
                        if (bytes == 0) break;

                        string part = Encoding.UTF8.GetString(buffer, 0, bytes);
                        sb.Append(part);
                        bytesReceived += bytes;

                        // 진행률 갱신
                        int percent = (int)((bytesReceived / (float)totalSize) * 100);
                        progressBar1.Value = Math.Min(percent, 100);
                        label4.Text = $"{percent}% ({bytesReceived / 1024} KB / {totalSize / 1024} KB)";
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

        private async void button2_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Excel 파일|*.xlsx";
            saveFileDialog.Title = "결과를 Excel로 저장";

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    string[] lines = textBox2.Text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

                    await Task.Run(() =>
                    {
                        using (var workbook = new ClosedXML.Excel.XLWorkbook())
                        {
                            var worksheet = workbook.Worksheets.Add("결과");

                            for (int i = 0; i < lines.Length; i++)
                            {
                                // | 기준으로 분리
                                string[] fields = lines[i].Split('|');

                                for (int j = 0; j < fields.Length; j++)
                                {
                                    // 각 셀에 값 채우기 (공백 트림)
                                    worksheet.Cell(i + 1, j + 1).Value = fields[j].Trim();
                                }
                            }
                            workbook.SaveAs(saveFileDialog.FileName);
                        }
                    });

                    MessageBox.Show("Excel 파일로 저장 완료!");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("저장 실패: " + ex.Message);
                }
            }
        }

        private async void button3_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "CSV 파일|*.csv";
            saveFileDialog.Title = "결과를 CSV로 저장";

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    string[] lines = textBox2.Text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

                    var csvLines = new List<string>();

                    // 헤더 추가
                    string header = "br,devid,devname,devtype,ipaddr,connport,macaddr,etc";
                    csvLines.Add(header);

                    foreach (var line in lines)
                    {
                        // | 기준으로 분리하고 ,로 조합
                        string[] fields = line.Split('|');
                        string csvLine = string.Join(",", fields.Select(f => f.Trim()));
                        csvLines.Add(csvLine);
                    }

                    // Use StreamWriter with async support instead of File.WriteAllLinesAsync
                    using (var writer = new StreamWriter(saveFileDialog.FileName, false, Encoding.UTF8))
                    {
                        foreach (var csvLine in csvLines)
                        {
                            await writer.WriteLineAsync(csvLine);
                        }
                    }

                    MessageBox.Show("CSV 파일로 저장 완료!");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("저장 실패: " + ex.Message);
                }
            }
        }
    }
}
