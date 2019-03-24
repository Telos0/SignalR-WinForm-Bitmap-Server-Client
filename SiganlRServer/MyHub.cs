using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;

namespace SignalREx1.Hubs
{
    // 웹 서버의 허브
    public class MyHub : Hub
    {


        //스튜던트 객체를 보내는 연습
        public void Send(Student student)
        {
            Clients.All.addMessage(student);
        }

        //json으로 변환된 객체를 보내는 연습
        public void Picture(string jsonString)
        {
            Clients.All.draw(jsonString);
        }
    }

    


    public class Student
    {
        public string   Name { get; set; }
        public int Age { get; set; }
        public string Id { get; set; }
    }

    //비트맵 이미지와 숫자가 담긴 객체
    public class NowImage
    {
        public int Number { get; set; }
        public string image { get; set; }
    }
}