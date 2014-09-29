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
        public void syncUsers(List<User> users, string channel) {
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

            // convert values to bools

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
    }
}
