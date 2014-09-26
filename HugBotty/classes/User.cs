using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HugBotty.classes;

namespace HugBotty.classes
{
    class User
    {
        public string channel;
        public string nick;
        public int points;
        public int pointsRecieved;
        public int pointsGiven;
        public bool isFollower;
        public bool isDonater;
        public bool isOnline;

        public User(string _channel, string _nick){
            this.channel = _channel;
            this.nick = _nick;
            this.isOnline = true;
            syncWithServer();
        }

        public void syncWithServer(){
            SQL s = new SQL(this.channel);
            User thisUser = s.getUser(this.nick);

            if(thisUser == null){
                this.points = 0;
                this.pointsRecieved = 0;
                this.pointsGiven = 0;
                this.isFollower = false;
                this.isDonater = false;
            }else{
                this.points = thisUser.points;
                this.pointsRecieved = thisUser.pointsRecieved;
                this.pointsGiven = thisUser.pointsRecieved;
                this.isFollower = thisUser.isFollower;
                this.isDonater = thisUser.isDonater;
            }
        }
    }
}
