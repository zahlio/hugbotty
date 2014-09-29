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
        public int id;
        public string channel;
        public string nick;
        public int points;
        public int pointsRecieved;
        public int pointsGiven;
        public bool isFollower;
        public bool isDonater;
        public bool isOnline;
        public bool isNew;

        public User(string _channel, string _nick, int _points = 0, int _pointsRecieved = 0, int _pointsGiven = 0, bool _isFollower = false, bool _isDonator = false, bool _isNew = false)
        {
            this.channel = _channel;
            this.nick = _nick;
            this.isOnline = true;
            this.isNew = _isNew;
        }
    }
}
