using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HugBotty.classes
{
    class Bet
    {
        public List<Tuple<User, int>> membersOptionA;
        public List<Tuple<User, int>> membersOptionB;
        public string name;
        public string optionA;
        public string optionB;
        public int maxBet;
        public long betEnd;
        public long nextAnnouncement;
        public long betAnnouncementWaiter;
        public int maxBetEntries;
        public bool acceptBets = true;

        public Bet(string _name, string _a, string _b, int _maxBet, long _betEnd, long _nextAnnouncement, int _maxBetEntries)
        {
            this.name = _name;
            this.optionA = _a;
            this.optionB = _b;
            this.maxBet = _maxBet;
            this.betEnd = _betEnd;
            this.betAnnouncementWaiter = _nextAnnouncement;
            this.maxBetEntries = _maxBetEntries;
        }

        public bool addToBet(User u, int amount, bool optionA) {

            if (!acceptBets) {
                return false;
            }

            int count = 0;

            count = membersOptionA.Count(user => user.Item1.nick.Equals(u.nick)) + membersOptionB.Count(user => user.Item1.nick.Equals(u.nick));

            if (count >= maxBetEntries) {
                return false;
            }


            if (optionA)
            {
                if (u.points - amount >= 0 && amount < maxBet && amount > 0)
                {
                    membersOptionA.Add(new Tuple<User, int>(u, amount));
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else {
                if (u.points - amount >= 0 && amount < maxBet && amount > 0)
                {
                    membersOptionB.Add(new Tuple<User, int>(u, amount));
                    return true;
                }
                else
                {
                    return false;
                }
            }            
        }

        public List<User> settleBet(bool optionA, List<User> users)
        {
            int totalWinnings = 0;
            if (optionA)
            {
                foreach (Tuple<User, int> u in membersOptionA)
                {
                    int betAmount = u.Item2;
                    double winningPerc = betAmount / membersOptionA.Sum(t => t.Item2);
                    int winnings = Convert.ToInt16(Math.Ceiling(Convert.ToDouble((membersOptionB.Sum(t => t.Item2) / 100)) * winningPerc));
                    int index = users.FindLastIndex(user => user.nick == u.Item1.nick);
                    if (index != -1)
                    {
                        users[index].points += winnings;
                        totalWinnings += winnings;
                    }
                }
            }else {
                foreach (Tuple<User, int> u in membersOptionB)
                {
                    int betAmount = u.Item2;
                    double winningPerc = betAmount / membersOptionB.Sum(t => t.Item2);
                    int winnings = Convert.ToInt16(Math.Ceiling(Convert.ToDouble((membersOptionA.Sum(t => t.Item2) / 100)) * winningPerc));
                    int index = users.FindLastIndex(user => user.nick == u.Item1.nick);
                    if (index != -1)
                    {
                        users[index].points += winnings;
                        totalWinnings += winnings;
                    }
                }
            }

            Console.Write("Total winnings: " + totalWinnings);
            return users;
        }

        public List<User> refund(List<User> users) {

            foreach (Tuple<User, int> u in membersOptionA)
            {
                int index = users.FindLastIndex(user => user.nick == u.Item1.nick);
                if (index != -1)
                {
                    users[index].points += u.Item2;
                }
            }

            foreach (Tuple<User, int> u in membersOptionB)
            {
                int index = users.FindLastIndex(user => user.nick == u.Item1.nick);
                if (index != -1)
                {
                    users[index].points += u.Item2;
                }
            }

            return users;
        }
    }
}
