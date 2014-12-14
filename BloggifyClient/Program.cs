using System;
using System.Net;
using System.Collections.Specialized;
using System.Text;
using System.IO;

namespace BloggifyClient
{
    class MainClass
    {

        static void readField(ref string field, string label) {
            Console.Write("> " + label + ": ");
            field = Console.ReadLine();
            while (field.Length == 0) {
                readField(ref field, label);
            }
        }

        static void promptForLoginData(ref string username, ref string password, ref string host)
        {
            readField(ref username, "Username");
            readField(ref password, "Password");
            readField(ref host, "Host");
        }

        static string getSid()
        {
            string username = null;
            string password = null;
            string host = null;

            try {
                StreamReader sr = new StreamReader("conf");
                string[] lines = sr.ReadToEnd().Split('\n');
                username = lines[0];
                password = lines[1];
                host = lines[2];
                sr.Close();
            } catch (Exception ex) {
                promptForLoginData(ref username, ref password, ref host);
            }

            Console.WriteLine("Authenticating...");
            var postData = "username=" + username;
            postData += "&password=" + password;

            var request = (HttpWebRequest)WebRequest.Create(host + "/admin");
            var data = Encoding.ASCII.GetBytes(postData);

            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = data.Length;
            request.AllowAutoRedirect = false;

            using (var stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }

            var response = (HttpWebResponse)request.GetResponse();

            if ((int)response.StatusCode == 301) {
                Console.WriteLine("Logged in successfully.");
                StreamWriter sw = new StreamWriter("conf");
                sw.WriteLine(username);
                sw.WriteLine(password);
                sw.WriteLine(host);
                sw.Close();
                return response.GetResponseHeader("Set-Cookie").Replace("_sid=", "");
            } else {
                Console.WriteLine("Wrong password or username. Reenter login data...");
                File.Delete("conf");
                return getSid();
            }
        }


        static string getAuthToken() {
            try {
                return getSid();
            } catch (Exception ex) {
                Console.WriteLine("Something went wrong: " + ex.Message);
                return getAuthToken();
            }
        }

        public static void Main(string[] args)
        {

            Console.WriteLine("C# Bloggify Client");
            string authToken = getAuthToken();
        
            Console.WriteLine(">> Authentication token was received: " + authToken);
        }
    }
}
