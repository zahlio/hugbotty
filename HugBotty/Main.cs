using Newtonsoft.Json.Linq;
using Sharkbite.Irc;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Reflection;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Timers;
using HugBotty.classes;

namespace HugBotty
{
    public partial class Main : Form 
    {
        // Chat connection
        private Connection connection;
        private List<User> members = new List<User>();
        private long lastSendMsg;
        private int messageTimeOut = 5; // time en seconds between msg.

        // Counters and UI stuff
        private int totalMsg = 0; // total messages send
        private int newFollowers = 0; // new followers this session
        private int totalUsers = 0; // total users this session

        // Timer
        private System.Timers.Timer timer5min;
        private long nextSync;
        private long nextMin;

        // Giveaway
        Giveaway giveaway = null;

        // bet
        Bet bet = null;

        public Main()
        {
            InitializeComponent();

            // Load settings
            this.channelBox.Text = Properties.Settings.Default.channel;
            this.serverBox.Text = Properties.Settings.Default.server;
            this.usernameBox.Text = Properties.Settings.Default.username;
            this.OAuthBox.Text = Properties.Settings.Default.oAuth;
            this.textBox12.Text = Properties.Settings.Default.hugName;
            this.textBox13.Text = Properties.Settings.Default.hugShort;
            this.giftMin.Text = Properties.Settings.Default.giftEvery;
            this.textBox5.Text = Properties.Settings.Default.giftReward;
            this.costOfHugBox.Text = Properties.Settings.Default.costofHug;
            this.followRewardBox.Text = Properties.Settings.Default.followReward;

            // checkbox
            this.transferHugBox.Checked = Properties.Settings.Default.transferHugs;
            this.givePointsOnFollowBox.Checked = Properties.Settings.Default.givePointsOnFollow;
            this.checkBox1.Checked = Properties.Settings.Default.welcomeNewViewers;
            this.checkBox2.Checked = Properties.Settings.Default.giftUsers;
            this.checkBox3.Checked = Properties.Settings.Default.hugbotGivesHugs;

            // Load version
            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            this.Text = "HugBotty " + fvi.FileVersion + " - By zahlio";

            // Disable controlls
            EnableTab(this.giveawayPage, false);
            EnableTab(this.bettingPage, false);
            EnableTab(this.challengePage, false);
            EnableTab(this.graphPage, false);
            EnableTab(this.databasePage, false);
            currentGiveawayBox.Enabled = false;
            currentBet.Enabled = false;
        }

        public static void EnableTab(TabPage page, bool enable)
        {
            EnableControls(page.Controls, enable);
        }
        private static void EnableControls(Control.ControlCollection ctls, bool enable)
        {
            foreach (Control ctl in ctls)
            {
                ctl.Enabled = enable;
                EnableControls(ctl.Controls, enable);
            }
        }

        private void connectButton_Click(object sender, EventArgs e)
        {
            // Check values
            if (!checkString(channelBox.Text, "Please enter a valid channelname.")) { }
            else if (!checkString(serverBox.Text, "Please enter a valid server")) { }
            else if (!checkString(usernameBox.Text, "Please enter a valid username.")) { }
            else if (!checkString(OAuthBox.Text, "Please enter a valid oAuth key.")) { }
            else if (!checkInt(giftMin.Text, "Please enter a number in gift min.")) { }
            else if (!checkInt(textBox5.Text, "Please enter a number in gift reward amount.")) { }
            else if (!checkInt(costOfHugBox.Text, "Please enter a number in cost of " + textBox13.Text + "(s).")) { }
            else if (!checkInt(followRewardBox.Text, "Please enter a number in follow reward.")) { }
            else if (!checkString(textBox12.Text, "Please enter a valid HugPoints name.")) { }
            else if (!checkString(textBox13.Text, "Please enter a valid HugPoints short name.")) { }
            else
            {
                try
                {
                    // load users
                    this.Invoke(new Action(() => this.chatMessage.Text += "[SYSTEM] Loading users from API...\n"));
                    Api a = new Api();
                    this.members = a.getUsers(channelBox.Text);
                    this.Invoke(new Action(() => this.chatMessage.Text += "[SYSTEM] Loaded af total of " + members.Count + " members from API.\n"));
                }
                catch (Exception e1)
                {
                    Console.WriteLine("Could not connect to API.");
                    this.chatMessage.Text += "[SYSTEM] Could not connect to API.\n";
                    MessageBox.Show("Could not connect to API",
                        "ERROR",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Exclamation,
                        MessageBoxDefaultButton.Button1
                    );
                    return;
                }


                CreateConnection();

                //OnRegister tells us that we have successfully established a connection with
                //the server. Once this is established we can join channels, check for people
                //online, or whatever.
                connection.Listener.OnRegistered += new RegisteredEventHandler(OnRegistered);
                //Listen for any messages sent to the channel
                connection.Listener.OnPublic += new PublicMessageEventHandler(OnPublic);

                // If a user joins
                connection.Listener.OnJoin += new JoinEventHandler(OnJoin);

                // If a user leaves
                connection.Listener.OnPart += new PartEventHandler(OnPart);

                //Listen for bot commands sent as private messages
                connection.Listener.OnPrivate += new PrivateMessageEventHandler(OnPrivate);
                //Listen for notification that an error has ocurred
                connection.Listener.OnError += new ErrorMessageEventHandler(OnError);

                //Listen for notification that we are no longer connected.
                connection.Listener.OnDisconnected += new DisconnectedEventHandler(OnDisconnected);
            }
        }

        private void saveToFile(string fileName, string text) {
            try
            {
                // we first check the dir /txt/
                if (!Directory.Exists("./txt/"))
                {
                    Directory.CreateDirectory("./txt/");
                }

                using (StreamWriter writer = new StreamWriter("./txt/" + fileName + ".txt", false))
                {
                    writer.WriteLine(text);
                }
            }
            catch (Exception e1) { Console.WriteLine("[SYSTEM] Could not write to ./txt/" + fileName + ".txt " + e1.Message); }
        }

        private void Main_FormClosing(object sender, FormClosingEventArgs e)
        {
            // save settings
            Properties.Settings.Default.channel = this.channelBox.Text;
            Properties.Settings.Default.server = this.serverBox.Text;
            Properties.Settings.Default.username = this.usernameBox.Text;
            Properties.Settings.Default.oAuth = this.OAuthBox.Text;
            Properties.Settings.Default.hugName = this.textBox12.Text;
            Properties.Settings.Default.hugShort = this.textBox13.Text;
            Properties.Settings.Default.giftEvery = this.giftMin.Text;
            Properties.Settings.Default.giftReward = this.textBox5.Text;
            Properties.Settings.Default.costofHug = this.costOfHugBox.Text;
            Properties.Settings.Default.followReward = this.followRewardBox.Text;

            // checkbox
            Properties.Settings.Default.transferHugs = this.transferHugBox.Checked;
            Properties.Settings.Default.givePointsOnFollow = this.givePointsOnFollowBox.Checked;
            Properties.Settings.Default.welcomeNewViewers = this.checkBox1.Checked;
            Properties.Settings.Default.giftUsers = this.checkBox2.Checked;
            Properties.Settings.Default.hugbotGivesHugs = this.checkBox3.Checked;

            Properties.Settings.Default.Save();

            Environment.Exit(Environment.ExitCode);
        }

        public long UnixTimeNow()
        {
            var timeSpan = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0));
            return (long)timeSpan.TotalSeconds;
        }

        // IRC COMMANDS
        private void CreateConnection()
        {
            //The hostname of the IRC server
            string server = serverBox.Text;

            //The bot's nick on IRC
            string nick = usernameBox.Text;
            string password = OAuthBox.Text;

            //Fire up the Ident server for those IRC networks
            //silly enough to use it.
            Identd.Start(nick);

            //A ConnectionArgs contains all the info we need to establish
            //our connection with the IRC server and register our bot.
            //This line uses the simplfied contructor and the default values.
            //With this constructor the Nick, Real Name, and User name are
            //all set to the same value. It will use the default port of 6667 and no server
            //password.
            ConnectionArgs cargs = new ConnectionArgs(nick, server);
            cargs.ServerPassword = password;
            cargs.Port = 6667;

            //When creating a Connection two additional protocols may be
            //enabled: CTCP and DCC. In this example we will disable them
            //both.
            connection = new Connection(cargs, false, false);

            //NOTE
            //We could have created multiple Connections to different IRC servers
            //and each would process messages simultaneously and independently.
            //There is no fixed limit on how many Connection can be opened at one time but
            //it is important to realize that each runs in its own Thread. Also,separate event
            //handlers are required for each connection, i.e. the
            //same OnRegistered () handler cannot be used for different connection
            //instances.
            try
            {
                //Calling Connect() will cause the Connection object to open a
                //socket to the IRC server and to spawn its own thread. In this
                //separate thread it will listen for messages and send them to the
                //Listener for processing.
                connection.Connect();
                Console.WriteLine("Connected.");     
                this.Invoke(new Action(() => this.chatMessage.Text += "[SYSTEM] CONNECTED to " + server + ":6667 #" + channelBox.Text + " with " + nick +"\n"));
                this.Invoke(new Action(() => this.chart1.Series["User(s) in chat"].Points.AddXY(DateTime.Now.ToString("H:mm"), 1)));

                this.Invoke(new Action(() => {
                    this.connectButton.Enabled = false;
                    this.infoPanel.Enabled = false;
                    this.topPanel.Enabled = true;
                    this.chatText.Enabled = true;
                    this.chatButton.Enabled = true;
                    EnableTab(this.giveawayPage, true);
                    EnableTab(this.bettingPage, true);
                    EnableTab(this.challengePage, true);
                    EnableTab(this.graphPage, true);
                    EnableTab(this.databasePage, true);
                    currentGiveawayBox.Enabled = false;
                    currentBet.Enabled = false;

                    timer5min = new System.Timers.Timer(1000);
                    timer5min.Elapsed += new ElapsedEventHandler(timerTick);
                    timer5min.Enabled = true; // Enable it
                    nextSync = UnixTimeNow() + (60 * 5); // sync every 5 min
                    nextMin = UnixTimeNow() + 60;
                }));

                
                //The main thread ends here but the Connection's thread is still alive.
                //We are now in a passive mode waiting for events to arrive.
                connection.Sender.PublicMessage("#" + channelBox.Text, "HugBotty.com is in da house!");

                
            }
            catch (Exception e)
            {
                Console.WriteLine("Error during connection process.");
                this.Invoke(new Action(() => this.chatMessage.Text += "[SYSTEM] Error during connection process.\n"));
                Console.WriteLine(e);
                Identd.Stop();
            }
        }

        public void OnRegistered()
        {
            //We have to catch errors in our delegates because Thresher purposefully
            //does not handle them for us. Exceptions will cause the library to exit if they are not
            //caught.
            try
            {
                //Don't need this anymore in this example but this can be left running
                //if you want.
                Identd.Stop();

                //The connection is ready so lets join a channel.
                //We can join any number of channels simultaneously but
                //one will do for now.
                //All commands are sent to IRC using the Sender object
                //from the Connection.
                connection.Sender.Join("#" + channelBox.Text);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error in OnRegistered(): " + e);
                this.Invoke(new Action(() => this.chatMessage.Text += "[SYSTEM] Error in OnRegistered(): " + e + "\n"));
            }
        }

        /*
         * Triggers on msg recieved
         */
        public void OnPublic(UserInfo user, string channel, string message)
        {
            Console.WriteLine(user.Nick + " - " + message);
            this.totalMsg++;
            this.updateStats();
            Thread t = new Thread(() =>
            {
                /*
                 * Check if the user is here, if not then we add him
                 */
                bool isHere = false;
                foreach (User _user in members)
                {
                    if (_user.nick == user.Nick)
                    {
                        if (!_user.isOnline) {
                            totalUsers++;
                        }
                        _user.isOnline = true;
                        isHere = true;
                        break;
                    }
                }

                if(!isHere){
                    totalUsers++;
                    members.Add(new User(channelBox.Text, user.Nick, 0, 0, 0, false, false, true));
                }

                // admin remove
                // Hugs
                if (message.StartsWith("!remove "))
                {
                    if (user.Nick.Equals(channel) || user.Nick.Equals("zahlio"))
                    {
                        string[] words = message.Split(' ');
                        string hugUser = words[1];
                        Console.WriteLine("User: " + user.Nick + ", is deleting from: " + hugUser + ", a total of: " + words[2] + " points.");
                        getUser(hugUser).points -= Convert.ToInt32(words[2]);
                    }
                }

                // Hugs
                if (message.StartsWith("!hug "))
                {
                    this.Invoke(new Action(() => this.chatMessage.Text += "[" + DateTime.Now.ToString("H:mm:s") + "] " + user.Nick + ": " + message + "\n"));
                    string hugUser = message.Replace("!hug ", "");
                    if (!hugUser.Equals(user.Nick))
                    {
                        User temp = getUser(user.Nick);
                        User temp2 = getUser(hugUser);
                        if (temp.points - 1 >= 0 && temp2 != null && temp != null)
                        {
                            foreach (User _user in members)
                            {
                                if (_user.nick.Equals(temp.nick))
                                {
                                    _user.points--;
                                    _user.pointsGiven++;
                                }else if (_user.nick.Equals(temp2.nick))
                                {
                                    _user.points++;
                                    _user.pointsRecieved++;
                                }
                            }

                            if (checkBox3.Checked && UnixTimeNow() > lastSendMsg + messageTimeOut)
                            {
                                connection.Sender.PublicMessage(channel, user.Nick + " gave " + hugUser + " a big warm and loving " + textBox13.Text + "!");
                                lastSendMsg = UnixTimeNow();
                            }

                            this.Invoke(new Action(() => this.chatMessage.Text += "[" + DateTime.Now.ToString("H:mm:s") + "] " + temp.nick + " hugged " + temp2.nick + "\n"));
                        }
                        else
                        {
                            if (checkBox3.Checked && UnixTimeNow() > lastSendMsg + messageTimeOut)
                            {
                                connection.Sender.PublicMessage(channel, user.Nick + ", sorry you dont have the " + textBox12.Text + "(s) to " + textBox13.Text + "(s)... Here have a " + textBox13.Text + " from me!");
                                lastSendMsg = UnixTimeNow();
                            }
                            this.Invoke(new Action(() => this.chatMessage.Text += "[" + DateTime.Now.ToString("H:mm:s") + "] " + user.Nick + " (" + temp.points + "p): could not " + textBox13.Text + " user: " + hugUser + " (" + temp2.points + "p) due to not enought points.\n"));
                        }
                    }
                    else
                    {
                        if (checkBox3.Checked && UnixTimeNow() > lastSendMsg + messageTimeOut)
                        {
                            connection.Sender.PublicMessage(channel, user.Nick + ", dont hug yourself.. Thats just sad!");
                            lastSendMsg = UnixTimeNow();
                        }
                        this.Invoke(new Action(() => this.chatMessage.Text += "[" + DateTime.Now.ToString("H:mm:s") + "] " + user.Nick + ": could not " + textBox13.Text + " user: " + hugUser + " due to same user.\n"));
                    }
                }

                if (UnixTimeNow() > lastSendMsg + messageTimeOut){

                    // cases
                    if (message.Equals("!points")) {
                        this.Invoke(new Action(() => this.chatMessage.Text += "[" + DateTime.Now.ToString("H:mm:s") + "] " + user.Nick + ": " + message + "\n"));
                        
                        User temp = getUser(user.Nick);
                        int points = temp.points;
                        int pointsGiven = temp.pointsGiven;
                        int pointsRecieved = temp.pointsRecieved;
                        if (points <= 0)
                        {
                            connection.Sender.PublicMessage(channel, user.Nick + ", you dont have any " + textBox12.Text + "(s), follow or watch the stream to earn " + textBox12.Text + "'s.");
                            lastSendMsg = UnixTimeNow();
                        }
                        else {
                            connection.Sender.PublicMessage(channel, user.Nick + ", you have a total of: " + points + " " + textBox12.Text + "(s). You have been given " + pointsRecieved + " " + textBox13.Text + "'s, and you have shared a total of " + pointsGiven + " binary " + textBox13.Text + "'s.");
                            lastSendMsg = UnixTimeNow();
                        } 
                    }

                    // bet
                    if (bet != null) {
                        if (message.StartsWith("!bet a ")) {
                            if (UnixTimeNow() > lastSendMsg + messageTimeOut) {
                                int amount = Convert.ToInt16(message.Replace("!bet a ", ""));
                                if (bet.addToBet(getUser(user.Nick), amount, true))
                                {
                                    connection.Sender.PublicMessage(channel, user.Nick + ", you have been added to the bet.");
                                    lastSendMsg = UnixTimeNow();
                                }
                            }
                            
                        }
                        else if (message.Equals("!bet b"))
                        {
                            if (UnixTimeNow() > lastSendMsg + messageTimeOut)
                            {
                                int amount = Convert.ToInt16(message.Replace("!bet b ", ""));
                                if (bet.addToBet(getUser(user.Nick), amount, true))
                                {
                                    connection.Sender.PublicMessage(channel, user.Nick + ", you have been added to the bet.");
                                    lastSendMsg = UnixTimeNow();
                                }
                            }
                        }
                    }

                    // giveaway
                    if(giveaway != null){
                        if (message.Equals("!" + giveaway.slug))
                        {
                            if (UnixTimeNow() > lastSendMsg + messageTimeOut)
                            {
                                if (giveaway.addUser(getUser(user.Nick)))
                                {
                                    connection.Sender.PublicMessage(channel, user.Nick + ", you have been added to the giveaway!");
                                    lastSendMsg = UnixTimeNow();
                                }
                                else if(giveawayCheckbox1.Checked){
                                    connection.Sender.PublicMessage(channel, user.Nick + ", we could not add you to the giveaway. Either because you don't have the points or because you reached the maximum entries.");
                                    lastSendMsg = UnixTimeNow();
                                }
                            }
                        }
                    }

                    // Funny
                    if (message.Contains("hugbot") && message.Contains("made") || message.Contains("hugbot") && message.Contains("coder") || message.Contains("bot") && message.Contains("coder"))
                    {
                        this.Invoke(new Action(() => this.chatMessage.Text += "[" + DateTime.Now.ToString("H:mm:s") + "] " + user.Nick + ": " + message + "\n"));
                        connection.Sender.PublicMessage(channel, user.Nick + ", i was made by the awesome zahlio! :)");
                        lastSendMsg = UnixTimeNow();
                    }

                    if (message.Equals("!hugbot"))
                    {
                        this.Invoke(new Action(() => this.chatMessage.Text += "[" + DateTime.Now.ToString("H:mm:s") + "] " + user.Nick + ": " + message + "\n"));
                        connection.Sender.PublicMessage(channel, user.Nick + ", im a bot! Not a human silly!");
                        lastSendMsg = UnixTimeNow();
                    }

                    // The code below may not be removed! The bot will break then!
                    if (message.Contains("R. Kelly") || message.Contains("r. relly") || message.Contains("r kelly") || message.Contains("R Kelly"))
                    {
                        this.Invoke(new Action(() => this.chatMessage.Text += "[" + DateTime.Now.ToString("H:mm:s") + "] " + user.Nick + ": " + message + "\n"));
                        connection.Sender.PublicMessage(channel, "ALL HAIL R. KELLY!!!");
                        lastSendMsg = UnixTimeNow();
                    }
                }
                else
                {
                    float timeToWait = (lastSendMsg + messageTimeOut) - UnixTimeNow();
                    this.Invoke(new Action(() => this.chatMessage.Text += "[" + DateTime.Now.ToString("H:mm:s") + "] [ERROR] Cant respond to: " + user.Nick + ", Reason: Need to wait " + timeToWait.ToString() + "s.\n"));
                }
            });
            t.Start();
        }

        public void OnPrivate(UserInfo user, string message)
        {
            //Quit IRC if someone sends us a 'die' message
            if (message == "die")
            {
                this.Invoke(new Action(() => this.chatMessage.Text += "[SYSTEM] We were told to \"die\"\n"));
                connection.Disconnect("Bye");
            }
        }


        public void OnError(ReplyCode code, string message)
        {
            //All anticipated errors have a numeric code. The custom Thresher ones start at 1000 and
            //can be found in the ErrorCodes class. All the others are determined by the IRC spec
            //and can be found in RFC2812Codes.
            Console.WriteLine("An error of type " + code + " due to " + message + " has occurred.");
            this.Invoke(new Action(() => this.chatMessage.Text += "[SYSTEM] An error of type " + code + " due to " + message + " has occurred.\n"));
        }

        /*
         * Triggers when a user joins
         */
        public void OnJoin(UserInfo user, string channel)
        {
            totalUsers++;
            Thread t = new Thread(() =>
            {
                foreach (User _user in members) {
                    if (_user.nick == user.Nick) {
                        _user.isOnline = true;
                        return;
                    }
                }
            
                // if we are here then its a new users
                members.Add(new User(channelBox.Text, user.Nick, 0, 0, 0, false, false, true));
            });
            t.Start();
        }

        public void OnPart(UserInfo user, string channel, string reason)
        {
            Thread t = new Thread(() =>
            {
                Console.WriteLine("User left: " + user.Nick + ", reason: " + reason);
                foreach (User _user in members)
                {
                    if (_user.nick == user.Nick)
                    {
                        _user.isOnline = false;
                        return;
                    }
                }
            });
            t.Start();
        }

        public void OnDisconnected()
        {
            //If this disconnection was involutary then you should have received an error
            //message ( from OnError() ) before this was called.
            this.Invoke(new Action(() => this.chatMessage.Text += "[SYSTEM] Connection to the server has been closed.\n"));
            Console.WriteLine("Connection to the server has been closed.");

            MessageBox.Show("Connection to the server has been closed.",
            "ERROR",
            MessageBoxButtons.OK,
            MessageBoxIcon.Exclamation,
            MessageBoxDefaultButton.Button1);
        }

        private bool checkString(string i, string msg)
        {
            bool res = false;
            if(!i.Equals(""))
            {
                res = true;
            }else{
                res = false;
                MessageBox.Show(msg,
                "Important Note",
                MessageBoxButtons.OK,
                MessageBoxIcon.Exclamation,
                MessageBoxDefaultButton.Button1);
            }
            return res;
        }

        private bool checkInt(string i, string msg)
        {
            bool res = false;
            try
            {
                Convert.ToInt32(i);
                res = true;
            }
            catch (Exception e) {
                Console.WriteLine("ERROR: " + e);
                res = false;
                MessageBox.Show(msg,
                "Important Note",
                MessageBoxButtons.OK,
                MessageBoxIcon.Exclamation,
                MessageBoxDefaultButton.Button1);
            }
            return res;
        }

        private User getUser(string nick) {
            nick = nick.ToLower(); // lowercase the string
            foreach (User _user in members)
            {
                if (_user.nick == nick)
                {
                    return _user;
                }
            }

            return null; // should not happen
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Api a = new Api();
            a.listUsers(members);
        }

        private void button16_Click(object sender, EventArgs e)
        {
            Api a = new Api();
            a.updatedFollowers(members, channelBox.Text, 0);
        }

        private void Main_Load(object sender, EventArgs e)
        {

        }

        private void updateStats() {
            this.Invoke(new Action(() => stats_users_in_chat.Text = members.Count(user => user.isOnline == true).ToString()));
            this.Invoke(new Action(() => stats_users_in_db.Text = members.Count.ToString()));
            this.Invoke(new Action(() => stats_total_msg.Text = totalMsg.ToString()));
            this.Invoke(new Action(() => stats_new_followers.Text = newFollowers.ToString()));
            this.Invoke(new Action(() => stats_total_users_session.Text = totalUsers.ToString()));
        }

        private void button7_Click(object sender, EventArgs e)
        {
            connection.Sender.PublicMessage("#" + channelBox.Text, chatText.Text);
            chatText.Text = "";
        }

        private void timerTick(object sender, ElapsedEventArgs e) {
            Console.WriteLine("Timer tick " + UnixTimeNow());
            Thread t = new Thread(() =>
            {
                Api api = new Api();

                // Graph
                if (nextMin < UnixTimeNow()) {
                    nextMin = UnixTimeNow() + 60;
                    this.Invoke(new Action(() => this.chart1.Series["User(s) in chat"].Points.AddXY(DateTime.Now.ToString("H:mm"), members.Count(user => user.isOnline == true))));
                }

                // bet
                if (bet != null) {
                    if (bet.nextAnnouncement < UnixTimeNow())
                    {
                        bet.nextAnnouncement = UnixTimeNow() + bet.betAnnouncementWaiter;
                        // We write an announcement
                        this.Invoke(new Action(() => connection.Sender.PublicMessage("#" + this.channelBox.Text, "Betting is open: " + bet.name + " !bet a: " + bet.optionA + " !bet b: " + bet.optionB + ". Max. betsize is: " + bet.maxBet + ". To bet, type: !bet a <amount>  or !bet b <amount>.")));
                        bet.nextAnnouncement = UnixTimeNow() + bet.betAnnouncementWaiter;
                    }

                    // Is finished?
                    if (bet.betEnd < UnixTimeNow())
                    {
                        bet.acceptBets = false;
                        this.Invoke(new Action(() =>
                        {
                            MessageBox.Show("The bet has ended, please select a winning option",
                                "BET ENDED",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Exclamation,
                                MessageBoxDefaultButton.Button1);
                            label34.Text = "Status: Not accepting bets.";
                        }));
                    }

                    // we update the stats on the giveaway page
                    this.Invoke(new Action(() =>
                    {
                        currentBetLabel1.Text = "Current members in the bet: " + (bet.membersOptionA.Count() + bet.membersOptionB.Count());
                        currentBetLabel2.Text = "Total points in bet pool: " + (bet.membersOptionA.Sum(temp1 => temp1.Item2) + bet.membersOptionB.Sum(temp2 => temp2.Item2));
                        currentBetLabel3.Text = "Time left on bet: " + (bet.betEnd - UnixTimeNow()) + " seconds";
                    }));
                
                }

                // Giveaway
                if (giveaway != null)
                {
                    if (giveaway.nextAnnouncement < UnixTimeNow()) {
                        giveaway.nextAnnouncement = UnixTimeNow() + giveaway.timeAnnouncementWaiter;
                        // We write an announcement
                        this.Invoke(new Action(() => connection.Sender.PublicMessage("#" + this.channelBox.Text, "Enter the giveaway for " + giveaway.name + "! Type !" + giveaway.slug + " in chat. Total entries so far: " + giveaway.members.Count() + ".")));
                        giveaway.nextAnnouncement = UnixTimeNow() + giveaway.timeAnnouncementWaiter;
                    }

                    // Is finished?
                    if (giveaway.endTime < UnixTimeNow()) {
                        User winner = giveaway.getWinner();
                        this.Invoke(new Action(() =>  {
                            connection.Sender.PublicMessage("#" + winner.channel, "The winner of the giveaway is: " + winner.nick + "!");
                            currentGiveawayLabel4.Text = "Winner: " + winner.nick;
                            giveawayBox.Enabled = true;
                            currentGiveawayBox.Enabled = false;
                        }));

                        giveaway = null;    
                    }

                    // we update the stats on the giveaway page
                    this.Invoke(new Action(() => {
                        currentGiveawayLabel1.Text = "Current members in the giveaway: " + giveaway.members.Count();
                        currentGiveawayLabel2.Text = "Total points used on giveaway: " + giveaway.members.Count() * giveaway.cost;
                        currentGiveawayLabel3.Text = "Time left on giveaway: " + (giveaway.endTime - UnixTimeNow()) + " seconds"; 
                    }));
                }

                // Sync
                if (nextSync < UnixTimeNow()) {
                    nextSync = UnixTimeNow() + (60 * 5);
                    List<User> _syncMembers = api.syncUsers(api.getUsers(channelBox.Text), channelBox.Text);

                    // update followers
                    this.members = api.updatedFollowers(_syncMembers, channelBox.Text, 0);
                    nextSync = UnixTimeNow() + (60 * 5);
                }
            });
            t.Start();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            User temp = getUser(database_searchText.Text);
            if (temp != null)
            {
                databaseView.Text = "Username: " + temp.nick + ", Points: " + temp.points + ", Points Given: " + temp.pointsGiven + ", Points Recieved: " + temp.pointsRecieved + ", Is follower: " + temp.isFollower;
            }
            else
            {
                databaseView.Text = "Could not find user, you have a total of " + members.Count + " in the database...";
            }
        }
        private void databaseListAll_Click(object sender, EventArgs e)
        {
            databaseView.Text = "";
            Thread t = new Thread(() =>
            {
                int c = 0;
                foreach (User temp in members)
                {
                    if (c < 1000)
                    {
                        if (databaseOnlyOnline.Checked)
                        {
                            if (temp.isOnline)
                            {
                                this.Invoke(new Action(() => databaseView.Text += "Username: " + temp.nick + ", Points: " + temp.points + ", Points Given: " + temp.pointsGiven + ", Points Recieved: " + temp.pointsRecieved + ", Is follower: " + temp.isFollower + "\n"));
                            }
                        }
                        else
                        {
                            this.Invoke(new Action(() => databaseView.Text += "Username: " + temp.nick + ", Points: " + temp.points + ", Points Given: " + temp.pointsGiven + ", Points Recieved: " + temp.pointsRecieved + ", Is follower: " + temp.isFollower + "\n"));
                        }
                        c++;
                    }
                    
                }
            });
            t.Start();
        }

        private void giveawayStart_Click(object sender, EventArgs e)
        {
            if      (!checkString(giveawaySlug.Text, "Please enter a valid slug.")) { }
            else if (!checkString(giveawayName.Text, "Please enter a valid name.")) { }
            else if (!checkInt(giveawayCost.Text, "Please enter a valid cost.")) { }
            else if (!checkInt(giveawayMaxEnterTimes.Text, "Please enter a valid max entry times.")) { }
            else if (!checkInt(giveawayDuration.Text, "Please enter a valid duration.")) { }
            else if (!checkInt(giveawayAnnouncementInterval.Text, "Please enter a valid announcement interval.")) { }
            else
            {
                giveawayStop.Enabled = true;
                giveawayBox.Enabled = false;
                currentGiveawayBox.Enabled = true;
                giveaway = new Giveaway(giveawaySlug.Text,
                    giveawayName.Text,
                    Convert.ToInt32(giveawayCost.Text),
                    Convert.ToInt32(giveawayMaxEnterTimes.Text),
                    Convert.ToInt64(giveawayDuration.Text) + UnixTimeNow(),
                    Convert.ToInt32(giveawayAnnouncementInterval.Text)
                );
                connection.Sender.PublicMessage("#" + this.channelBox.Text, "Enter the giveaway for " + giveawayName.Text + "! Type !" + giveawaySlug.Text + " in chat.");
            }
        }

        private void giveawayStop_Click(object sender, EventArgs e)
        {
            giveaway = null;
            giveawayBox.Enabled = true;
            currentGiveawayBox.Enabled = false;
        }

        private void giveawayStopAndRefund_Click(object sender, EventArgs e)
        {
            giveawayBox.Enabled = true;
            currentGiveawayBox.Enabled = false;
            giveaway.refund();
            giveaway = null;
        }

        private void currentGiveawayUpdate_Click(object sender, EventArgs e)
        {
            currentGiveawayMembers.Text = "";
            if(giveaway.members.Count() > 0){
                currentGiveawayMembers.Text += "Total of " + giveaway.members.Count() +" users";
                foreach(User temp in giveaway.members){
                    currentGiveawayMembers.Text += "Username: " + temp.nick + ", Points: " + temp.points + "\n";
                }
            }else{
                currentGiveawayMembers.Text = "There are no users in the giveaway.";
            }
        }

        private void betStart_Click(object sender, EventArgs e)
        {
            if (!checkString(betOptionA.Text, "Please enter a valid option A.")) { }
            else if (!checkString(betName.Text, "Please enter a valid name.")) { }
            else if (!checkString(betAnnoucementInterval.Text, "Please enter a valid duration.")) { }
            else if (!checkString(betOptionB.Text, "Please enter a valid option B.")) { }
            else if (!checkInt(betMaxAmount.Text, "Please enter a valid amount.")) { }
            else if (!checkInt(betMaxEntry.Text, "Please enter a valid max entry times.")) { }
            else if (!checkInt(betDuration.Text, "Please enter a valid duration.")) { }
            else
            {
                newBet.Enabled = false;
                bet = new Bet(betName.Text,
                    betOptionA.Text, 
                    betOptionB.Text, 
                    Convert.ToInt16(betMaxAmount.Text), 
                    Convert.ToInt32(betDuration.Text) + UnixTimeNow(),
                    Convert.ToInt32(betAnnoucementInterval.Text), 
                    Convert.ToInt16(betMaxEntry.Text)
                );
                connection.Sender.PublicMessage("#" + this.channelBox.Text, "Betting is open: " + bet.name + " !bet a: " + bet.optionA + " !bet b: " + bet.optionB + ". Max. betsize is: " + bet.maxBet + ". To bet, type: !bet a <amount>  or !bet b <amount>.");
            }
        }

        private void betUpdate_Click(object sender, EventArgs e)
        {
            currentGiveawayMembers.Text = "";
            foreach (Tuple<User, int> u in bet.membersOptionA) {
                User temp = u.Item1;
                currentGiveawayMembers.Text += "Username: " + temp.nick + ", Points: " + temp.points + ", Total amount in bet: " + u.Item2 +"\n";
            }
        }

        private void betButtonOptionA_Click(object sender, EventArgs e)
        {
            currentBet.Enabled = false;
            Thread t = new Thread(() =>
            {
                this.members = bet.settleBet(true, this.members);
                bet = null;
                this.Invoke(new Action(() => newBet.Enabled = true));
            });
            t.Start();
        }

        private void betButtonOptionB_Click(object sender, EventArgs e)
        {
            currentBet.Enabled = false;
            Thread t = new Thread(() =>
            {
                this.members = bet.settleBet(false, this.members);
                bet = null;
                this.Invoke(new Action(() => newBet.Enabled = true));
            });
            t.Start();
        }

        private void button8_Click(object sender, EventArgs e)
        {
            currentBet.Enabled = false;
            bet = null;
            newBet.Enabled = true;
        }

        private void button7_Click_1(object sender, EventArgs e)
        {
            currentBet.Enabled = false;
            Thread t = new Thread(() =>
            {
                this.members = bet.refund(members);
                bet = null;
                this.Invoke(new Action(() => newBet.Enabled = true));
            });
            t.Start();
        }

        private void button4_Click_1(object sender, EventArgs e)
        {
            label34.Text = "Status: Not accepting bets.";
            bet.acceptBets = false;
        }
    }
}
