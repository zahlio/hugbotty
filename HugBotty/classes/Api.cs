using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Net;

namespace HugBotty.classes
{
    class Api
    {
        public List<User> syncUsers(List<User> users, string channel)
        {
            string usersJson = "";

            if (users.Count > 0)
            {
                User lastObj = users.Last();
                foreach (User u in users)
                {
                    usersJson += JsonConvert.SerializeObject(u);
                    if (!u.Equals(lastObj))
                    {
                        usersJson += ", ";
                    }
                }
            }


            string json = @"{'channel': '" + channel + "', 'users': [" + usersJson + "]}";

            // replace ' with "
            json = json.Replace("'", "\"");
            Debug.WriteLine("API REQUEST: " + json);

            var request = (HttpWebRequest)WebRequest.Create("http://www.hugbotty.com/api/");

            var postData = "action=sync&json=" + json;
            var data = Encoding.ASCII.GetBytes(postData);

            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = data.Length;

            using (var stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }

            var response = (HttpWebResponse)request.GetResponse();

            var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();

            Debug.WriteLine("API RESPONSE: " + responseString);

            foreach (User uTemp in users)
            {
                uTemp.isNew = false;
            }

            Debug.WriteLine("GOT A TOTAL OF: " + users.Count + " USERS");
            return users;
        }

        public List<User> getUsers(string channel)
        {
            var request = (HttpWebRequest)WebRequest.Create("http://www.hugbotty.com/api/");

            var postData = "action=get&channel=" + channel;
            var data = Encoding.ASCII.GetBytes(postData);

            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = data.Length;

            using (var stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }

            var response = (HttpWebResponse)request.GetResponse();

            var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();

            Debug.WriteLine("API RESPONSE: " + responseString);

            List<User> users = JsonConvert.DeserializeObject<List<User>>(responseString);
            Debug.WriteLine("GOT A TOTAL OF: " + users.Count + " USERS");
            return users;
        }

        public void listUsers(List<User> users) {
            foreach (User u in users)
            {
                Debug.WriteLine(u.nick);
            }
        }

        public List<User> updatedFollowers(List<User> users, string channel, int offset)
        {
            // https://api.twitch.tv/kraken/channels/diojr1/follows

            var request = (HttpWebRequest)WebRequest.Create("https://api.twitch.tv/kraken/channels/" + channel + "/follows?direction=DESC&limit=100&offset=" + offset);

            request.Method = "GET";
            request.ContentType = "application/x-www-form-urlencoded";

            var response = (HttpWebResponse)request.GetResponse();

            var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();

            RootObject members = JsonConvert.DeserializeObject<RootObject>(responseString);
            int count = 0;
            foreach(User u in users){
                if(!u.isFollower){
                    foreach (Follow f in members.follows) {
                        if(f.user.name.Equals(u.nick)){
                            u.isFollower = true;
                            count++;
                        }
                    }
                }
            }

            Console.WriteLine("Added a total of " + count + " new followers.");

            if (members._total > offset)
            {
                return updatedFollowers(users, channel, (offset + 100));
            }
            else {
                return users;
            }
        }
    }


    /** JSON CLASSSES START **/
    public class TwitchUser
    {
        public int _id { get; set; }
        public string name { get; set; }
        public string created_at { get; set; }
        public string updated_at { get; set; }
        public Links2 _links { get; set; }
        public string display_name { get; set; }
        public string logo { get; set; }
        public string bio { get; set; }
        public string type { get; set; }
    }

    public class Follow
    {
        public string created_at { get; set; }
        public Links _links { get; set; }
        public TwitchUser user { get; set; }
    }

    public class Links
    {
        public string self { get; set; }
    }

    public class Links2
    {
        public string self { get; set; }
        public string next { get; set; }
    }

    public class RootObject
    {
        public List<Follow> follows { get; set; }
        public int _total { get; set; }
        public Links2 _links { get; set; }
    }

    /** JSON CLASSSES END **/
}
