using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HugBotty.classes
{
    class SQL
    {

        private string _channelName;

        public SQL(string channelName) {
            this._channelName = channelName;
        }

        public User getUser(string nick)
        {
            string query = "SELECT * FROM botdata WHERE nick = '" + nick + "' AND channel = '" + _channelName + "'";

            // User var
            User user = null;

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
                        user = new User(
                            (string)dataReader["channel"],
                            (string)dataReader["nick"], 
                            Convert.ToInt32(dataReader["points"]),
                            Convert.ToInt32(dataReader["points_recieved"]),
                            Convert.ToInt32(dataReader["points_given"]),
                            false,
                            false
                        );
                    }
                }

                //close connection
                CloseConnection(tempCon);
            }

            // if the user is null then we need to create it
            if (user == null) {
                this.createUser(nick);
            }

            return user;
        }

        public void createUser(string nick) {
            string query = "INSERT INTO botdata (channel, nick, points) VALUES ('" + _channelName + "', '" + nick + "', 0)";

            //Open connection
            MySqlConnection tempCon = OpenConnection();
            if (tempCon != null)
            {
                //create command and assign the query and connection from the constructor
                MySqlCommand cmd = new MySqlCommand(query, tempCon);

                cmd.ExecuteReader();

                //close connection
                CloseConnection(tempCon);
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
                return null;
            }
            catch (Exception e2)
            {
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
                return false;
            }
        }
    }
}
