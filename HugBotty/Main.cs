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
using MySql.Data.MySqlClient;
using System.Reflection;
using System.Diagnostics;

namespace HugBotty
{
    public partial class Main : Form 
    {
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.Timer timer2;
        private System.Windows.Forms.Timer timer5;

        private int messageTimeOut = 5; // time en second between msg.

        private int thisSession = 0;
        private long lastSendMsg = 0;
        private int totalMsgs = 0;
        private int newFollowersSession = 0;

        List<string> chat = new List<string>(); // users who are in the chat
        List<bool> isFollowingList = new List<bool>(); // we have a local array wiht if a user from chat is following as it would take too much time to check ex. 200 users at the same time
        private int secLeft = 0;

        // Giveaway
        List<string> giveaway = new List<string>();
        private string giveawayName = "";
        private int giveawayCost = 0;
        private long giveawayEnd = 0;
        private string lastWinner = "";
        private int lastUpdate = 0;
        private int timeToEnd = 0;

        private bool giveawayActive = false;

        // Debug window
        public bool debugging = false;
        Debug debugWindow;

        // challenge
        List<string> challenge = new List<string>();
        private bool challengeActive = false;

        // MySQL stuff
        private Connection connection;

        public void startGiveAway() {

            if (debugging) {
                debugWindow.addText("Started giveaway.");
            }

            saveToFile("giveAwayWinner", ""); // we clear the file
            giveaway = new List<string>();
            this.giveawayName = textBox1.Text;
            this.giveawayCost = Convert.ToInt32(textBox2.Text);
            this.giveawayEnd = ((Convert.ToInt32(textBox3.Text) * 60) + UnixTimeNow()) * 1000;
            this.lastUpdate = (int)UnixTimeNow();
            timeToEnd = (int)((Convert.ToInt32(textBox3.Text) * 60) + UnixTimeNow());

            timer5 = new System.Windows.Forms.Timer();
            timer5.Tick += new EventHandler(timer5_Tick);
            timer5.Interval = 1000;
            timer5.Start();

            secLeft = Convert.ToInt32(textBox3.Text) * 60;
            progressBar1.Maximum = secLeft;

            connection.Sender.PublicMessage("#" + channelBox.Text, "Giveaway started! To enter the giveaway for: " + textBox11.Text + " enter: !" + giveawayName + ", cost is: " + giveawayCost + " " + textBox12.Text +"(s). Giveaway ends in: " + (((giveawayEnd / 1000) - UnixTimeNow()) / 60) + " min(s).");
            lastSendMsg = UnixTimeNow();
        }

        private void givePoints(string nick, int points) {
            string query = "UPDATE botdata SET points = points + " + points + " WHERE nick = '" + nick + "' AND channel = '" + channelBox.Text + "'";

            if (debugging)
            {
                debugWindow.addText("givePoints(): UPDATE botdata SET points = points + " + points + " WHERE nick = '" + nick + "' AND channel = '" + channelBox.Text + "'");
            }

            //open connection
             MySqlConnection tempCon = OpenConnection();
             if (tempCon != null)
             {
                //create command and assign the query and connection from the constructor
                MySqlCommand cmd = new MySqlCommand(query, tempCon);

                //Execute command
                cmd.ExecuteNonQuery();

                //close connection
                CloseConnection(tempCon);
            }
        }

        private string getUsers() {
            string query = "SELECT * FROM botdata WHERE channel = '" + channelBox.Text + "'";

            if (debugging)
            {
                debugWindow.addText("getUsers(): SELECT * FROM botdata WHERE channel = '" + channelBox.Text + "'");
            }

            string res = "";
            //Open connection
            MySqlConnection tempCon = OpenConnection();
            if (tempCon != null)
            {
                //create command and assign the query and connection from the constructor
                MySqlCommand cmd = new MySqlCommand(query, tempCon);

                MySqlDataReader dataReader = cmd.ExecuteReader();

                if (dataReader.HasRows)
                {
                    while (dataReader.Read())
                    {
                        res += "[" + dataReader["id"] + "] " + dataReader["nick"] + ", Points: " + dataReader["points"] + "\n";
                    }
                }
                else {
                    if (debugging)
                    {
                        debugWindow.addText("getUsers(): Could not find users for channel: " + channelBox.Text);
                    }
                }

                //close connection
                CloseConnection(tempCon);
            }
            return res;
        }

        private string getUser(string nick)
        {
            string query = "SELECT * FROM botdata WHERE nick = '" + nick + "' AND channel = '" + channelBox.Text + "'";

            if (debugging)
            {
                debugWindow.addText("getUser(): SELECT * FROM botdata WHERE nick = '" + nick + "' AND channel = '" + channelBox.Text + "'");
            }

            string res = "";
            //Open connection
            MySqlConnection tempCon = OpenConnection();
            if (tempCon != null)
            {
                //create command and assign the query and connection from the constructor
                MySqlCommand cmd = new MySqlCommand(query, tempCon);

                MySqlDataReader dataReader = cmd.ExecuteReader();

                if (dataReader.HasRows)
                {
                    while (dataReader.Read())
                    {
                        res += "[" + dataReader["id"] + "] " + dataReader["nick"] + ", Points: " + dataReader["points"] + "\n";
                    }
                }
                else
                {
                    if (debugging)
                    {
                        debugWindow.addText("getUser(): Could not find user: " + nick);
                    }
                }

                //close connection
                CloseConnection(tempCon);
            }
            return res;
        }

        private void addFollow(string nick, string time) {
            string query = "UPDATE botdata SET follow_date = '" + time + "' WHERE nick = '" + nick + "' AND channel = '" + channelBox.Text + "'";

            if (debugging)
            {
                debugWindow.addText("addFollow(): UPDATE botdata SET follow_date = '" + time + "' WHERE nick = '" + nick + "' AND channel = '" + channelBox.Text + "'");
            }

            //open connection
            MySqlConnection tempCon = OpenConnection();
            if (tempCon != null)
            {
                //create command and assign the query and connection from the constructor
                MySqlCommand cmd = new MySqlCommand(query, tempCon);

                //Execute command
                cmd.ExecuteNonQuery();

                //close connection
                CloseConnection(tempCon);
            }
            givePoints(nick, Convert.ToInt32(followRewardBox.Text));
        }

        private void addpoints_recieved(string nick)
        {
            string query = "UPDATE botdata SET points_recieved = points_recieved + 1 WHERE nick = '" + nick + "' AND channel = '" + channelBox.Text + "'";

            if (debugging)
            {
                debugWindow.addText("addpoints_recieved(): UPDATE botdata SET points_recieved = points_recieved + 1 WHERE nick = '" + nick + "' AND channel = '" + channelBox.Text + "'");
            }

            //open connection
            MySqlConnection tempCon = OpenConnection();
            if (tempCon != null)
            {
                //create command and assign the query and connection from the constructor
                MySqlCommand cmd = new MySqlCommand(query, tempCon);

                //Execute command
                cmd.ExecuteNonQuery();

                //close connection
                CloseConnection(tempCon);
            }
        }

        private void addpoints_given(string nick)
        {
            string query = "UPDATE botdata SET points_given = points_given + 1 WHERE nick = '" + nick + "' AND channel = '" + channelBox.Text + "'";

            if (debugging)
            {
                debugWindow.addText("addpoints_given(): UPDATE botdata SET points_given = points_given + 1 WHERE nick = '" + nick + "' AND channel = '" + channelBox.Text + "'");
            }

            //open connection
            MySqlConnection tempCon = OpenConnection();
            if (tempCon != null)
            {
                //create command and assign the query and connection from the constructor
                MySqlCommand cmd = new MySqlCommand(query, tempCon);

                //Execute command
                cmd.ExecuteNonQuery();

                //close connection
                CloseConnection(tempCon);
            }
        }

        private bool userExists(string nick) {
            string query = "SELECT points FROM botdata WHERE nick = '" + nick + "' AND channel = '" + channelBox.Text + "'";

            if (debugging)
            {
                debugWindow.addText("userExists(): SELECT points FROM botdata WHERE nick = '" + nick + "' AND channel = '" + channelBox.Text + "'");
            }

            //Open connection
            MySqlConnection tempCon = OpenConnection();
            if (tempCon != null)
            {
                //create command and assign the query and connection from the constructor
                MySqlCommand cmd = new MySqlCommand(query, tempCon);

                MySqlDataReader dataReader = cmd.ExecuteReader();

                if (dataReader.HasRows) {
                    if (debugging)
                    {
                        debugWindow.addText("userExists(): Checking if user is in DB: " + nick + " RES: TRUE");
                    }
                    return true;
                }

                //close connection
                CloseConnection(tempCon);
            }
            if (debugging)
            {
                debugWindow.addText("userExists(): Checking if user is in DB: " + nick + " RES: FALSE");
            }
            return false;
        }

        private int getUsersInDB()
        {
            int res = 0;
            string query = "SELECT nick FROM botdata WHERE channel = '" + channelBox.Text + "'";
            if (debugging)
            {
                debugWindow.addText("getUsersInDB(): SELECT nick FROM botdata WHERE channel = '" + channelBox.Text + "'");
            }

            //Open connection
            MySqlConnection tempCon = OpenConnection();
            if (tempCon != null)
            {
                //Create Command
                MySqlCommand cmd = new MySqlCommand(query, tempCon);
                //Create a data reader and Execute the command
                MySqlDataReader dataReader = cmd.ExecuteReader();

                //Read the data and store them in the list
                while (dataReader.Read())
                {
                    res++;
                }

                //close Data Reader
                dataReader.Close();

                //close Connection
                CloseConnection(tempCon);
            }

            if (debugging)
            {
                debugWindow.addText("getUsersInDB(): Found a total of " + res + " users.");
            }
            return res;
        }

        private int getPointsGiven(string nick) {
            int res = 0;
            string query = "SELECT points_given FROM botdata WHERE nick = '" + nick + "' AND channel = '" + channelBox.Text + "'";

            if (debugging)
            {
                debugWindow.addText("getPointsGiven(): SELECT points_given FROM botdata WHERE nick = '" + nick + "' AND channel = '" + channelBox.Text + "'");
            }

            //Open connection
            MySqlConnection tempCon = OpenConnection();
            if (tempCon != null)
            {
                //Create Command
                MySqlCommand cmd = new MySqlCommand(query, tempCon);
                //Create a data reader and Execute the command
                MySqlDataReader dataReader = cmd.ExecuteReader();

                //Read the data and store them in the list
                while (dataReader.Read())
                {
                    res = Convert.ToInt32(dataReader["points_given"]);
                }

                //close Data Reader
                dataReader.Close();

                //close Connection
                CloseConnection(tempCon);
            }
            return res;
        }

        private int getPointsRecieved(string nick)
        {
            int res = 0;
            string query = "SELECT points_recieved FROM botdata WHERE nick = '" + nick + "' AND channel = '" + channelBox.Text + "'";

            //Open connection
            MySqlConnection tempCon = OpenConnection();
            if (tempCon != null)
            {
                //Create Command
                MySqlCommand cmd = new MySqlCommand(query, tempCon);
                //Create a data reader and Execute the command
                MySqlDataReader dataReader = cmd.ExecuteReader();

                //Read the data and store them in the list
                while (dataReader.Read())
                {
                    res = Convert.ToInt32(dataReader["points_recieved"]);
                }

                //close Data Reader
                dataReader.Close();

                //close Connection
                CloseConnection(tempCon);
            }
            return res;
        }

        private int getPoint(string nick) {
            int res = 0;
            string query = "SELECT points FROM botdata WHERE nick = '" + nick + "' AND channel = '" + channelBox.Text + "'";

            //Open connection
            MySqlConnection tempCon = OpenConnection();
            if (tempCon != null)
            {
                //Create Command
                MySqlCommand cmd = new MySqlCommand(query, tempCon);
                //Create a data reader and Execute the command
                MySqlDataReader dataReader = cmd.ExecuteReader();

                //Read the data and store them in the list
                while (dataReader.Read())
                {
                    res = Convert.ToInt32(dataReader["points"]);
                }

                //close Data Reader
                dataReader.Close();

                //close Connection
                CloseConnection(tempCon);
            }
            Console.WriteLine("Getting points for user: " + nick + " is: " + res);
            return res;
        }

        private bool isFollowing(string nick)
        {
            string query = "SELECT follow_date FROM botdata WHERE nick = '" + nick + "' AND channel = '" + channelBox.Text + "'";

            //Open connection
            MySqlConnection tempCon = OpenConnection();
            if (tempCon != null)
            {
                //Create Command
                MySqlCommand cmd = new MySqlCommand(query, tempCon);
                //Create a data reader and Execute the command
                MySqlDataReader dataReader = cmd.ExecuteReader();

                //Read the data and store them in the list
                if (dataReader.HasRows)
                {
                    while (dataReader.Read())
                    {
                        if (!dataReader["follow_date"].Equals("0"))
                        {
                            return true;
                        }
                    }
                }

                //close Data Reader
                dataReader.Close();

                //close Connection
                CloseConnection(tempCon);
            }
            return false;
        }

        private bool giveHug(string nick, string giver) {

            if (!userExists(giver))
            {
                addUser(giver);
            }

            Console.WriteLine("User: " + giver + ", is hugging: " + nick);
            if (getPoint(giver) - Convert.ToInt32(costOfHugBox.Text) >= 0 && userExists(nick))
            {
                givePoints(nick, Convert.ToInt32(costOfHugBox.Text));
                givePoints(giver, -Convert.ToInt32(costOfHugBox.Text));
                addpoints_recieved(nick);
                addpoints_given(giver);
                return true;
            }
            else {
                return false;
            }
        }

        private void addUser(string nick) {
            Console.WriteLine("Adding user: " + nick + ", to DB");
            string query = "INSERT INTO botdata (channel, nick) VALUES ('" + channelBox.Text + "', '" + nick + "')";

            //open connection
            MySqlConnection tempCon = OpenConnection();
            if (tempCon != null)
            {
                //create command and assign the query and connection from the constructor
                MySqlCommand cmd = new MySqlCommand(query, tempCon);

                //Execute command
                cmd.ExecuteNonQuery();

                //close connection
                this.CloseConnection(tempCon);
            }
        }

        //open connection to database
        private MySqlConnection OpenConnection()
        {
            try
            {
                string connectionString = "SERVER=ftp.zahlio.com;DATABASE=hugbotty;UID=hugbotty;PASSWORD=hugbotty123;Pooling=false;";
                MySqlConnection newCon = new MySqlConnection(connectionString);
                newCon.Open();
                return newCon;
            }
            catch (MySqlException ex)
            {
                //When handling errors, you can your application's response based 
                //on the error number.
                //The two most common error numbers when connecting are as follows:
                //0: Cannot connect to server.
                //1045: Invalid user name and/or password.
                switch (ex.Number)
                {
                    case 0:
                        Console.WriteLine("Cannot connect to server.  Error: " + ex);
                        break;

                    case 1045:
                        Console.WriteLine("Invalid username/password, please try again. Error: " + ex);
                        break;
                }
                if (debugging)
                {
                    debugWindow.addText("[ERROR] OpenConnection(): MySqlException: " + ex);
                }
                return null;
            }
            catch (Exception e2) {
                debugWindow.addText("[ERROR] OpenConnection(): Exception: " + e2);
                return null;
            }
        }

        //Close connection
        private bool CloseConnection(MySqlConnection con)
        {
            try
            {
                con.Close();
                return true;
            }
            catch (MySqlException ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            save();
        }

        private void updateButton_Click(object sender, EventArgs e)
        {
            // check values
            if (!checkString(textBox1.Text, "Please enter a valid giveaway name.")) { }
            else if (!checkString(textBox11.Text, "Please enter a valid reward name.")) { }
            else if (!checkInt(textBox2.Text, "Please enter a number in giveaway cost.")) { }
            else if (!checkInt(textBox3.Text, "Please enter a number in giveaway end time.")) { }
            else if (!checkInt(textBox4.Text, "Please enter a number in giveaway max entry.")) { }
            else
            {
                giveawayActive = !giveawayActive;
                if (giveawayActive)
                {
                    updateButton.Text = "STOP";
                    Console.WriteLine("Giveaway started.");
                    this.Invoke(new Action(() => this.chatMessage.Text += "[SYSTEM] Giveaway started.\n"));
                    startGiveAway();
                    this.Invoke(new Action(() => this.groupBox3.Enabled = false));
                }
                else
                {
                    connection.Sender.PublicMessage("#" + channelBox.Text, "Giveaway stopped!");
                    Console.WriteLine("Giveaway stopped.");
                    this.Invoke(new Action(() => this.chatMessage.Text += "[SYSTEM] Giveaway stopped.\n"));
                    updateButton.Text = "START";
                }
            }
        }

        /*
         * Adds (or removes) points to a user.
         */
        private void button5_Click(object sender, EventArgs e)
        {
            if (!checkInt(textBox7.Text, "Please enter a valid amount in amount we should give the user.")) { }
            else if (!checkString(textBox8.Text, "Please enter a valid username.")) { }
            else
            {
                Thread t = new Thread(() =>
                {
                    givePoints(textBox8.Text, Convert.ToInt32(textBox7.Text));
                });
                t.Start();
            }
        }

        public bool enterChallenge(string username, string channel) {
            bool res = false;
            int challengeCost =Convert.ToInt32(textBox15.Text);
            if (userExists(username))
            {
                if ((getPoint(username) - challengeCost) >= 0)
                {
                    givePoints(username, -challengeCost);
                    this.Invoke(new Action(() => this.chatMessage.Text += "[SYSTEM] " + username + " entered the challenge.\n"));
                    challenge.Add(username);
                    lastSendMsg = UnixTimeNow();

                    // update the list
                    int id = 0;
                    string temp = "";
                    foreach (String ch in challenge) {
                        id++;
                        temp += "[" + id + "] " + ch + "\n";
                    }
                    this.Invoke(new Action(() => this.richTextBox2.Text = temp));
                    res = true;
                }
                else
                {
                    this.Invoke(new Action(() => this.chatMessage.Text += "[SYSTEM] " + username + " could NOT enter the challenge.\n"));
                    res = false;
                }
            }
            else {
                res = false;   
            }

            return res;
        }

        public void enterGiveAway(string username, string channel) {
            int max_entry = Convert.ToInt32(textBox6.Text);

            Thread t = new Thread(() =>
            {
                int thisEntryTimes = 0;
                for (int j = 0; j < giveaway.Count; j++)
                {
                    if (giveaway[j].Equals(username))
                    {
                        thisEntryTimes++;
                    }
                }

                if ((thisEntryTimes + 1) > max_entry)
                {
                    connection.Sender.PublicMessage("#" + channel, username + " sorry, you cannot have more then " + max_entry + " entry(s) in this giveaway. (You have: " + thisEntryTimes + ")");
                    Console.WriteLine(username + " sorry, you cannot have more then " + max_entry + " entry(s) in this giveaway. (You have: " + thisEntryTimes + ")");
                    this.Invoke(new Action(() => this.chatMessage.Text += "[SYSTEM] " + username + " cannot have more then " + max_entry + " entry(s) in this giveaway. (Have: " + thisEntryTimes + ")\n"));
                    lastSendMsg = UnixTimeNow();
                }


                if(userExists(username)){
                    if((getPoint(username) - giveawayCost) >= 0){
                        givePoints(username, -giveawayCost);
                        connection.Sender.PublicMessage("#" + channel, username + " you have entered the giveaway.");
                        Console.WriteLine(username + " you have entered the giveaway.");
                        this.Invoke(new Action(() => this.chatMessage.Text += "[SYSTEM] " + username + " entered the giveaway.\n"));
                        giveaway.Add(username);
                        lastSendMsg = UnixTimeNow();
                    }else{
                        Console.WriteLine(username + " sorry you don't have the " + textBox12.Text + "(s) to enter this giveaway.");
                        connection.Sender.PublicMessage("#" + channel, username + " sorry you don't have the " + textBox12.Text + "(s) to enter this giveaway.");
                        this.Invoke(new Action(() => this.chatMessage.Text += "[SYSTEM] " + username + " could NOT enter the giveaway.\n"));
                        lastSendMsg = UnixTimeNow();
                    }
                }

                this.Invoke(new Action(() => this.label31.Text = (giveaway.Count * giveawayCost).ToString()));
                this.Invoke(new Action(() => this.label30.Text = giveaway.Count.ToString()));
            });
            t.Start();
        }

        public Main()
        {
            InitializeComponent();
            textBox9.KeyDown += new KeyEventHandler(tb_KeyDown);

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

            // Donate box
            Donate d = new Donate(this);
            d.Show();
            this.Enabled = false;

            // Load version
            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            this.Text = "HugBotty " + fvi.FileVersion + " - By zahlio";
        }

        private void tb_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                button7_Click(null, null);
            }
        }

        private void getOAuthButton_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("www.twitchapps.com/tmi");
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
                panel2.Enabled = true;
                panel3.Enabled = false;
                challengePanel.Enabled = true;
            }
        }

        private void timer5_Tick(object sender, EventArgs e)
        {
            Thread t = new Thread(() =>
            {
                if (timeToEnd < UnixTimeNow())
                {
                    Console.WriteLine("timeToEnd TICK");
                    // pick a winner
                    Random r = new Random();
                    int winner = r.Next(giveaway.Count);

                    if (giveaway.Count > 0)
                    {
                        connection.Sender.PublicMessage("#" + channelBox.Text, "THE WINNER IS: " + giveaway[winner] + "!!!");
                        saveToFile("giveAwayWinner", "The winner is: " + giveaway[winner]);
                        this.Invoke(new Action(() => this.lastWinner = giveaway[winner]));
                    }
                    else
                    {
                        connection.Sender.PublicMessage("#" + channelBox.Text, "There is no winner of the giveaway as noone entered...");
                        this.Invoke(new Action(() => this.updateButton.Text = "START"));
                    }
                    
                    this.Invoke(new Action(() => this.groupBox3.Enabled = true));
                    this.Invoke(new Action(() => this.giveawayActive = false));
                    this.Invoke(new Action(() => this.timer5.Stop()));
                    this.Invoke(new Action(() => this.updateButton.Text = "START"));
                    MessageBox.Show("Giveaway ended!\n\nWinner is: " + giveaway[winner]);

                }else if (lastUpdate + 60 < UnixTimeNow())
                {
                    Console.WriteLine("lastUpdate TICK");
                    this.Invoke(new Action(() => this.lastUpdate = (int)UnixTimeNow()));
                    Console.WriteLine("TIMER 3 TICK");
                    connection.Sender.PublicMessage("#" + channelBox.Text, "To enter the giveaway for: " + textBox11.Text + ", enter: !" + giveawayName + ", cost is: " + giveawayCost + " " + textBox12.Text + "(s).");
                    this.Invoke(new Action(() => this.lastSendMsg = UnixTimeNow()));
                }
                if (giveawayActive)
                {
                    this.Invoke(new Action(() => this.secLeft--));
                    this.Invoke(new Action(() => this.label33.Text = secLeft.ToString()));
                    this.Invoke(new Action(() => this.progressBar1.Value = (this.progressBar1.Maximum - secLeft)));
                    string timeLeft = string.Format("{0:00}:{1:00}:{2:00}", secLeft / 3600, (secLeft / 60) % 60, secLeft % 60);

                    // we save in .txt that can be used later
                    saveToFile("giveAwayCountDown", "Giveaway ends in: " + timeLeft.ToString());
                }
            });
            t.Start();
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

        /*
         * Should give all users in chat hugpoints that is stated in Convert.ToInt32(textBox5.Text)).ToString()
         */
        private void timer1_Tick(object sender, EventArgs e)
        {
            Console.WriteLine("Timer 1 tick");
            Thread t = new Thread(() =>
            {
                if (checkBox2.Checked)
                {
                    // add hugs points to users
                    connection.Sender.PublicMessage("#" + channelBox.Text, "Time for some " + textBox12.Text + "(s) " + textBox5.Text + " to each!");
                    lastSendMsg = UnixTimeNow();

                    for (int i = 0; i < chat.Count; i++)
                    {
                        givePoints(chat[i], Convert.ToInt32(textBox5.Text));
                    }
                }
            });
            t.Start();
        }

        /*
         * Ticks every 1min
         * Should update followers (so give points to new ones)
         * Should also update the chart with how many users we have in chat.
         */
        private void timer2_Tick(object sender, EventArgs e)
        {
            // we check all user who are in chat if they just have followed!
            Thread t = new Thread(() =>
            {
                try
                {
                    this.Invoke(new Action(() => this.label16.Text = getUsersInDB().ToString())); // Total users in DB
                    this.Invoke(new Action(() => this.label14.Text = chat.Count().ToString())); // Users currently in chat
                    this.Invoke(new Action(() => this.label20.Text = thisSession.ToString())); // total users in this session (so all the users who have been here this session)
                }
                catch (Exception e1)
                {
                    Console.WriteLine("ERROR: " + e1.Message);
                }


                this.Invoke(new Action(() => this.chart1.Series["User(s) in chat"].Points.AddXY(DateTime.Now.ToString("H:mm"), chat.Count)));

                for (int i = 0; i < chat.Count; i++)
                {
                    if (!isFollowingList[i])
                    {
                        if (!isFollowing(chat[i]))
                        {
                            newFollowers(chat[i], i);
                        }
                        else
                        {
                            Console.WriteLine(chat[i] + " is following (from remote db)");
                            isFollowingList[i] = true;
                        }
                    }
                    else {
                        Console.WriteLine(chat[i] + " is following (from local db)");
                        isFollowingList[i] = true;
                    }               
                }
            });
            t.Start();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // we do this in a new thread
            timer2_Tick(null, null);
        }
         
        /*
         * Gives all users in chat an amount of points
         */
        private void button2_Click(object sender, EventArgs e)
        {
            // check valus
            if (!checkInt(textBox4.Text, "Please enter a valid amount in amount we should give the users.")) { }
            else
            {
                Thread t = new Thread(() =>
                {
                    // add hugs points to users
                    connection.Sender.PublicMessage("#" + channelBox.Text, "Time for some " + textBox12.Text + "(s) " + textBox4.Text + " to each! Reason: " + textBox14.Text);
                    lastSendMsg = UnixTimeNow();

                    for (int i = 0; i < chat.Count; i++)
                    {
                        givePoints(chat[i], Convert.ToInt32(textBox4.Text));
                    }
                });
                t.Start();
            }
        }

        public void newFollowers(string user, int id) {
            Console.WriteLine("Checking if user: " + user + ", is following...");
            String json = "";
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://api.twitch.tv/kraken/users/" + user + "/follows/channels/" + channelBox.Text);
            request.KeepAlive = true;
            request.Headers.Set(HttpRequestHeader.CacheControl, "max-age=0");
            request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
            request.UserAgent = "Mozilla/5.0 (Windows NT 6.2; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/32.0.1700.107 Safari/537.36";
            HttpWebResponse response = null;
            try
            {
                response = (HttpWebResponse)request.GetResponse();
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
            }

            try
            {
                Stream streamWeb = response.GetResponseStream();
                StreamReader readStreamWeb = new StreamReader(streamWeb);
            
                while (readStreamWeb.Peek() >= 0)
                {
                    json = readStreamWeb.ReadToEnd();
                }

                // Parse the json
                if (!json.Equals(""))
                {
                    JObject obj = JObject.Parse(json);
                    string created_at = obj.SelectToken("created_at").ToString();
                    if (!created_at.Equals("0"))
                    {
                        Console.WriteLine("User " + user + ", followed at: " + created_at);
                        if (givePointsOnFollowBox.Checked)
                        {
                            if (!userExists(user))
                            {   // we check if the users is in our DB
                                // if he is not then we insert him
                                addUser(user); // we add the user to the db
                            }
                            givePoints(user, Convert.ToInt32(textBox5.Text));
                            addFollow(user, created_at);
                            isFollowingList[id] = true;
                            newFollowersSession++;
                            this.Invoke(new Action(() => this.label46.Text = newFollowersSession.ToString()));
                            this.Invoke(new Action(() => this.chatMessage.Text += "[SYSTEM] NEW FOLLOWER: " + user + ", Rewarded: " + Convert.ToInt32(followRewardBox.Text) + "\n"));
                            saveToFile("latestFollower", "Latest follower: " + user);
                        }
                    }
                }
            }
            catch (Exception e) {
                Console.WriteLine("ERROR: " + e.Message);
            }
        }

        private void save()
        {
            // No need for this as we live edit the DB
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

        public void setupTimers() {
            timer1 = new System.Windows.Forms.Timer();
            timer1.Tick += new EventHandler(timer1_Tick);

            int interval = 1;
            if (Convert.ToInt32(giftMin.Text) >= 1) {
                interval = Convert.ToInt32(giftMin.Text);
            }
            timer1.Interval = interval * 60000; // min to miliseconds
            timer1.Start();

            timer2 = new System.Windows.Forms.Timer();
            timer2.Tick += new EventHandler(timer2_Tick);
            timer2.Interval = 60000; // min to miliseconds
            timer2.Start();
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
                this.Invoke(new Action(() => this.connectButton.Enabled = false));
                this.Invoke(new Action(() => this.panel1.Enabled = true));
                this.Invoke(new Action(() => this.button7.Enabled = true));
                this.Invoke(new Action(() => this.textBox9.Enabled = true));
                this.Invoke(new Action(() => this.chatMessage.Text += "[SYSTEM] CONNECTED to " + server + ":6667 #" + channelBox.Text + " with " + nick +"\n"));
                this.Invoke(new Action(() => this.chart1.Series["User(s) in chat"].Points.AddXY(DateTime.Now.ToString("H:mm"), 1)));
                
                //The main thread ends here but the Connection's thread is still alive.
                //We are now in a passive mode waiting for events to arrive.

                setupTimers();
                connection.Sender.PublicMessage("#" + channelBox.Text, "HugBotty is in da house!");
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

        public void OnPublic(UserInfo user, string channel, string message)
        {
            Thread t = new Thread(() =>
            {
                // add to db
                bool isHere = false;
                for (int i = 0; i < chat.Count; i++)
                {
                    if (chat[i].Equals(user.Nick))
                    {
                        isHere = true;
                    }
                }

                if (!isHere)
                {
                    // add the user
                    chat.Add(user.Nick);
                    isFollowingList.Add(false);
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
                        givePoints(hugUser, -Convert.ToInt32(words[2]));
                    }
                }

                // Hugs
                if (message.StartsWith("!hug "))
                {
                    this.Invoke(new Action(() => this.chatMessage.Text += "[" + DateTime.Now.ToString("H:mm:s") + "] " + user.Nick + ": " + message + "\n"));
                    string hugUser = message.Replace("!hug ", "");
                    if (!hugUser.Equals(user.Nick))
                    {
                        if (giveHug(hugUser, user.Nick))
                        {
                            if (checkBox3.Checked && UnixTimeNow() > lastSendMsg + messageTimeOut)
                            {
                                connection.Sender.PublicMessage(channel, user.Nick + " gave " + hugUser + " a big warm and loving " + textBox13.Text + "!");
                                lastSendMsg = UnixTimeNow();
                            }
                        }
                        else
                        {
                            if (checkBox3.Checked && UnixTimeNow() > lastSendMsg + messageTimeOut)
                            {
                                connection.Sender.PublicMessage(channel, user.Nick + ", sorry you dont have the " + textBox12.Text + "(s) to " + textBox13.Text + "(s)... Here have a " + textBox13.Text + " from me!");
                                lastSendMsg = UnixTimeNow();
                            }
                        }
                    }
                    else
                    {
                        if (checkBox3.Checked && UnixTimeNow() > lastSendMsg + messageTimeOut)
                        {
                            connection.Sender.PublicMessage(channel, user.Nick + ", dont hug yourself.. Thats just sad!");
                            lastSendMsg = UnixTimeNow();
                        }
                    }
                }


                totalMsgs++;
                //Echo back any public messages
                this.Invoke(new Action(() => this.label25.Text = totalMsgs.ToString()));

                if (UnixTimeNow() > lastSendMsg + messageTimeOut)
                {

                    // cases
                    if (message.Equals("!points")) {
                        this.Invoke(new Action(() => this.chatMessage.Text += "[" + DateTime.Now.ToString("H:mm:s") + "] " + user.Nick + ": " + message + "\n"));
                        int points = getPoint(user.Nick);
                        if (points == 0)
                        {
                            connection.Sender.PublicMessage(channel, user.Nick + ", you dont have any " + textBox12.Text + "(s), follow or watch the stream to earn " + textBox12.Text + "'s.");
                            lastSendMsg = UnixTimeNow();
                        }
                        else {
                            connection.Sender.PublicMessage(channel, user.Nick + ", you have a total of: " + points + " " + textBox12.Text + "(s). You have been given " + getPointsRecieved(user.Nick) + " " + textBox13.Text + "'s, and you have shared a total of " + getPointsGiven(user.Nick) + " binary " + textBox13.Text + "'s.");
                            lastSendMsg = UnixTimeNow();
                        } 
                    }

                    if (giveawayActive) {
                        if (message.Equals("!" + giveawayName))
                        {
                            this.Invoke(new Action(() => this.chatMessage.Text += "[" + DateTime.Now.ToString("H:mm:s") + "] " + user.Nick + ": " + message + "\n"));
                            this.Invoke(new Action(() => this.chatMessage.Text += "[" + DateTime.Now.ToString("H:mm:s") + "] User: " + user.Nick + ", trying to enter giveaway.\n"));
                            enterGiveAway(user.Nick, channelBox.Text);
                        }
                    }

                    if (challengeActive) {
                        // price textBox15
                        if (message.Equals("!challenge"))
                        {
                            this.Invoke(new Action(() => this.chatMessage.Text += "[" + DateTime.Now.ToString("H:mm:s") + "] " + user.Nick + ": " + message + "\n"));
                            this.Invoke(new Action(() => this.chatMessage.Text += "[" + DateTime.Now.ToString("H:mm:s") + "] User: " + user.Nick + ", trying to enter challenge\n"));

                            Thread tChallenge = new Thread(() =>
                            {
                                if (enterChallenge(user.Nick, channelBox.Text))
                                {
                                    connection.Sender.PublicMessage(channel, user.Nick + ", you have entered the challenge. Please wait for your turn!");
                                    lastSendMsg = UnixTimeNow();
                                }
                            });
                            tChallenge.Start();
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

        public void OnJoin(UserInfo user, string channel)
        {
            Thread t = new Thread(() =>
            {
                thisSession++;
                chat.Add(user.Nick);
                isFollowingList.Add(false);
            
                // user join
                Console.WriteLine("User joined: " + user.Nick);

                // we check if he is in our record if not we add him

                if (!userExists(user.Nick)) { // we check if the users is in our DB
                    // if he is not then we insert him
                    addUser(user.Nick); // we add the user to the db
                    if (checkBox1.Checked) // If this is checked then we should greed him welcome
                    {
                        connection.Sender.PublicMessage(channel, "Hello " + user.Nick + ", welcome to the stream!");
                        lastSendMsg = UnixTimeNow();
                    }
                }
            });
            t.Start();
        }

        public void OnPart(UserInfo user, string channel, string reason)
        {
            Thread t = new Thread(() =>
            {
                try
                {
                    int indexOfUser = chat.IndexOf(user.Nick);
                    isFollowingList.RemoveAt(indexOfUser);
                    chat.RemoveAt(indexOfUser);
                    this.Invoke(new Action(() => this.label14.Text = chat.Count.ToString()));
                }
                catch (Exception e) {
                    Console.WriteLine("ERROR: " + e);
                }

                // User leave
                Console.WriteLine("User left: " + user.Nick + ", reason: " + reason);
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
            save();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            OAuthBox.Visible = !OAuthBox.Visible;
        }

        private void button7_Click(object sender, EventArgs e)
        {
            connection.Sender.PublicMessage("#" + channelBox.Text, textBox9.Text);
            textBox9.Text = "";
            lastSendMsg = UnixTimeNow();
        }

        /*
         * Should refresh the this.richTextBox1.Text with a list of all users in the db
         */
        private void button8_Click(object sender, EventArgs e)
        {
            if (!checkString(channelBox.Text, "Please enter a valid channelname.")) { }else
            {
                Thread t = new Thread(() =>
                {
                    string temp = getUsers();
                    this.Invoke(new Action(() => this.richTextBox1.Text = temp));
                });
                t.Start();
            }
        }

        /*
         * Should search for a user and display the stats for him
         */
        private void button9_Click(object sender, EventArgs e)
        {
            // check 
            if (!checkString(textBox10.Text, "Please enter a valid name first.") || !checkString(channelBox.Text, "Please enter a valid channelname.")) {
            
            }
            else
            {
                Thread t = new Thread(() =>
                {
                    string temp = getUser(textBox10.Text);
                    this.Invoke(new Action(() => this.richTextBox1.Text = temp));
                });
                t.Start();
            }
        }

        private void button10_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("http://www.twitch.tv/message/compose?to=" + lastWinner);
        }

        private bool checkString(string i, string msg)
        {
            bool res = false;
            if(!i.Equals(""))
            {
                res = true;
            }else
            {
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

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            // mailto:christoffer.mikkelsen@hotmail.com
            linkLabel1.LinkVisited = true;
            System.Diagnostics.Process.Start("mailto:christoffer.mikkelsen@hotmail.com?subject=Regarding HugBotty");
        }

        private void Main_Load(object sender, EventArgs e)
        {

        }

        private void label45_Click(object sender, EventArgs e)
        {

        }

        private void button3_Click(object sender, EventArgs e)
        {

        }

        private void serverBox_TextChanged(object sender, EventArgs e)
        {

        }

        private void tabPage4_Click(object sender, EventArgs e)
        {

        }

        private void button3_Click_1(object sender, EventArgs e)
        {
            debugWindow = new Debug(this);
            debugWindow.Show();
        }

        private void button4_Click_1(object sender, EventArgs e)
        {
            if (checkInt(textBox15.Text, "Please enter a number in the challenge cost"))
            { 
                this.challengeActive = !this.challengeActive;
                if (this.challengeActive)
                {
                    button4.Text = "Stop challenge";
                    textBox15.Enabled = false;
                    button11.Enabled = true;
                    button12.Enabled = true;
                    connection.Sender.PublicMessage("#" + channelBox.Text, "Challenge has started, type !challenge to enter, the entry fee is " + textBox15.Text + " " + textBox12.Text + "(s)");
                }
                else {
                    button4.Text = "Start challenge";
                    textBox15.Enabled = true;
                    button11.Enabled = false;
                    button12.Enabled = false;
                    connection.Sender.PublicMessage("#" + channelBox.Text, "Challenge has ended!");
                }
            }
        }

        private void button11_Click(object sender, EventArgs e)
        {
            if (challenge.Count > 0)
            {
                // delete first in list

                // update the list
                int id = 0;
                string temp = "";
                foreach (String ch in challenge)
                {
                    if (id == 0)
                    {
                        label48.Text = ch;
                    }
                    id++;
                    temp += "[" + id + "] " + ch + "\n";
                }
                challenge.RemoveAt(0);

                // update the list
                int id2 = 0;
                string temp2 = "";
                foreach (String ch2 in challenge)
                {
                    id2++;
                    temp2 += "[" + id2 + "] " + ch2 + "\n";
                }

                int id3 = 0;
                string temp3 = "";
                foreach (String ch3 in challenge)
                {
                    id3++;
                    if (id3 != 3)
                    {
                        temp3 += ch3 + "\n";
                    }
                }

                saveToFile("currentChallenge", channelBox.Text + " vs " + label48.Text);
                saveToFile("3NextChallenge", temp3);

                this.Invoke(new Action(() => this.richTextBox2.Text = temp2));
            }
        }

        private void button12_Click(object sender, EventArgs e)
        {
            if (challenge.Count > 0)
            {
                // update the list
                int id = 0;
                string temp = "";
                foreach (String ch in challenge)
                {
                    id++;
                    temp += "[" + id + "] " + ch + "\n";
                }

                // update the list
                int id2 = 0;
                string temp2 = "";
                foreach (String ch2 in challenge)
                {
                    id2++;
                    temp2 += "[" + id2 + "] " + ch2 + "\n";
                }

                int id3 = 0;
                string temp3 = "";
                foreach (String ch3 in challenge)
                {
                    id3++;
                    if (id3 != 3)
                    {
                        temp3 += ch3 + "\n";
                    }
                }

                saveToFile("currentChallenge", channelBox.Text + " vs " + label48.Text);
                saveToFile("3NextChallenge", temp3);

                this.Invoke(new Action(() => this.richTextBox2.Text = temp2));
            }
        }

        private void chart1_Click(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void groupBox3_Enter(object sender, EventArgs e)
        {

        }

        private void label59_Click(object sender, EventArgs e)
        {

        }

        private void label55_Click(object sender, EventArgs e)
        {

        }

        private void label53_Click(object sender, EventArgs e)
        {

        }
    }
}
