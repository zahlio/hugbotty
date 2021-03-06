﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HugBotty.classes
{
    class Giveaway
    {
        public string name;
        public string slug;
        public int cost;
        public int maxEnterTimes;
        public long endTime;
        public long nextAnnouncement;
        public long timeAnnouncementWaiter;
        public List<User> members = new List<User>();

        public Giveaway(string _slug, string _name, int _cost, int _maxEnterTimes, long _endTime, long _timeAnnouncementWaiter)
        {
            this.slug = _slug;
            this.name = _name;
            this.cost = _cost;
            this.maxEnterTimes = _maxEnterTimes;
            this.endTime = _endTime;
            this.timeAnnouncementWaiter = _timeAnnouncementWaiter;
        }

        public bool addUser(User u){
            int count = 0;
            foreach (User u1 in members) {
                if (u1.nick == u.nick) {
                    count++;
                }
            }

            if (count <= maxEnterTimes - 1)
            {
                if (u.points - cost >= 0)
                {
                    members.Add(u);
                    u.points -= cost;
                    return true;
                }
                else {
                    return false;
                }
                
            }
            else {
                return false;
            }
        }

        public User getWinner() {
            Random rnd = new Random();
            int r = rnd.Next(members.Count);
            return (User)members[r];
        }

        public void refund() {
            foreach (User u1 in members)
            {
                u1.points += cost;
            }
        }
    }
}
