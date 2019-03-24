using Microsoft.AspNet.SignalR.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ClientWinForm
{
    public partial class Form1 : Form
    {
        // 캔버스
        bool isDrag = false;
        Point previousPoint;
        Pen currentPen;
        Bitmap bitmap;
        Graphics g;

        //서버와 연결하는 프록시와 커넥션
        private IHubProxy HubProxy { get; set; }
        const string ServerURI = "http://localhost:2989/signalr";
        private HubConnection Connection { get; set; }


        public Form1()
        {
            InitializeComponent();
            ConnectAsync();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Student stu = new Student {
                Name = txtName.Text,
                Age = Convert.ToInt32(txtAge.Text),
                Id = txtId.Text
            };
            
            HubProxy.Invoke("Send", stu);
            txtName.Text = String.Empty;
            txtName.Focus();
        }

        //서버와 클라를 연결하는 매서드 먼저 "http://localhost:2989/signalr" 으로 허브 컨넥션을 만듬 
        // 그 커넥션을 서버의 MtHub와 허브프록시로 연결 이제부터 모든 것은 허브프록시로 호출
        // HubProxy.On을 이용해서 서버에서 1번째 인수 메서드가 호출되면 자동으로 해당 프록시 온 메서드가 클라이언트에서 수행됨
        // 두개의 매서드를 지정도 가능하고 각각이 새로운 쓰레드로 돌아간다고 생각하면 쉬움
        private async void ConnectAsync()
        {
            Connection = new HubConnection(ServerURI);
            HubProxy = Connection.CreateHubProxy("MyHub");

            //스튜던트 객체를 통째로 받는 중
            HubProxy.On<Student>("addMessage", (student) =>
                this.Invoke((Action)(() =>
                    textBox2.AppendText(String.Format("Name : {0}, Age : {1}, Id : {2}" + Environment.NewLine, student.Name, student.Age, student.Id))
                ))
            );

            //비트맵이미지와 숫자를 json형식으로 받아 컨버트함
            HubProxy.On<string>("draw", (jsonString) =>
                this.Invoke((Action)(() =>
                    pictureBox1.Image = ToBitmap(JsonConvert.DeserializeObject<NowImage>(jsonString).image)
                    )));
            // 커넥션을 비동적으로 연결
            try
            {
                await Connection.Start();
            }
            catch (HttpRequestException)
            {
                textBox2.Text = "Unable to connect to server: Start server before connecting clients.";
                return;
            }
            //커넥션 성공 메세지
            textBox2.AppendText("Connected to server at " + ServerURI + Environment.NewLine);
        }

        // 그림 그릴때 마우스 클리해서 시자작하는 부분
        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            isDrag = true;
            previousPoint = new Point(e.X, e.Y);
        }

        //그림 그릴때 마우스가 움직이는 부분
        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDrag == true)
            {
                Point currentPoint = new Point(e.X, e.Y);
                currentPen = new Pen(Color.Black);
                currentPen.Width = 1;

                g.DrawLine(currentPen, previousPoint, currentPoint);
                previousPoint = currentPoint;
                pictureBox1.Image = bitmap;

            }
        }

        // 그림이 다 그려지면서 마우스를 뗄 때 비트맵 이미지를 이미지 컨버터로 스트링으로 전환하여 NowIamge객체에 넣음
        // 그 객체를 json으로 직렬화하여 서버의 Picture메서드로 넘겨줌
        // Picture메서드는 draw메서드를 모든 클라이언트에 호출하고
        // 위에 HubProxy.On<string>("draw", (jsonString) => ~) 모든 클라에서 수행됨
        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            isDrag = false;
            pictureBox1.DrawToBitmap(bitmap, new Rectangle(0, 0, pictureBox1.Width, pictureBox1.Height));

            ImageConverter converter = new ImageConverter();
            var bitmapData = Convert.ToBase64String((byte[])converter.ConvertTo(bitmap, typeof(byte[])));

            MessageBox.Show("end draw");
            NowImage now = new NowImage
            {
                Number = 5,
                image = bitmapData
            };

            string jsonString = JsonConvert.SerializeObject(now);
            HubProxy.Invoke("Picture", jsonString);

           
        }

        //클라에 도착한 NowImage를 역직렬화하고 그 중에서 비트맵 스트링을 다시 비트맵으로 만들어주는 메서드
        //MemoryStream을 이용하여 반환한다.

        public Bitmap ToBitmap(string bitmapData)
        {
            var bytes = Convert.FromBase64String(bitmapData);
            Bitmap bitmap;
            using (var ms = new MemoryStream(bytes))
                 return bitmap = new Bitmap(Bitmap.FromStream(ms));
        }


        private void Form1_Load(object sender, EventArgs e)
        {
            bitmap = new Bitmap(this.pictureBox1.Width, this.pictureBox1.Height);
            g = Graphics.FromImage(bitmap);
        }
    }

    public class Student
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public string Id { get; set; }
    }

    public class NowImage
    {
        public int Number { get; set; }
        public string image { get; set; }
    }
}
