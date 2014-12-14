using System;
using System.Net;
using System.Collections.Specialized;
using System.Text;
using System.IO;

namespace BloggifyClient
{
    class MainClass
    {

        static string host = "";
        static string username = "";
        static string password = "";
        static string authToken = "";
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

        static string makeApiRequest(string api, string postData) {
            var request = (HttpWebRequest)WebRequest.Create(host + "/api/" + api);
            var data = Encoding.ASCII.GetBytes(postData);


            request.Method = postData.Length > 0 ? "POST" : "GET";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = data.Length;
            request.AllowAutoRedirect = false;

            request.Headers["cookie"] = "_sid=" + authToken;
            if (postData.Length > 0)
            {
                using (var stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }
            }


            try {
                using (WebResponse response = request.GetResponse())
                {
                    return new StreamReader(response.GetResponseStream()).ReadToEnd();
                }
            } catch (WebException e) {
                using (WebResponse response = e.Response)
                {
                    HttpWebResponse httpResponse = (HttpWebResponse) response;
                    Console.WriteLine("Error code: {0}", httpResponse.StatusCode);
                    using (Stream d = response.GetResponseStream())
                    using (var reader = new StreamReader(d))
                    {
                        string text = reader.ReadToEnd();
                        return text;
                    }
                }
            }
        }

        static string getSid()
        {
            try {
                StreamReader sr = new StreamReader("conf");
                string[] lines = sr.ReadToEnd().Split('\n');
                username = lines[0];
                password = lines[1];
                host = lines[2];
                sr.Close();
            } catch (Exception) {
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

        static int Menu() {
            Console.WriteLine("1) List articles");
            Console.WriteLine("2) List pages");
            Console.WriteLine("3) Delete page");
            Console.WriteLine("4) Delete article");
            Console.WriteLine("5) New article");
            Console.WriteLine("6) New page");
            Console.WriteLine("7) Sync data with git remote server");
            Console.WriteLine("8) Quit");
            Console.Write("> ");
            try {
                int selected = int.Parse(Console.ReadLine());
                if (selected > 8 || selected < 1) {
                    return Menu();
                }

                return selected;
            } catch(Exception) {
                return Menu();
            }
        }

        static void handleMenuItem(int i)
        {
            string res = "";
            string[] lines = {};
            switch (i)
            {
                // List articles
                case 1:
                    res = makeApiRequest("articles", "");
                    lines = res.Split('\n');
                    Console.WriteLine(">>> Articles");
                    foreach (var line in lines)
                    {
                        if (line.Contains("title\":"))
                        {
                            Console.WriteLine(line.Replace("\"title\":", ""));
                        }
                    }
                    break;
                // List pages
                case 2:
                    res = makeApiRequest("pages", "");
                    lines = res.Split('\n');
                    Console.WriteLine(">>> Pages");
                    foreach (var line in lines)
                    {
                        if (line.Contains("title\":"))
                        {
                            Console.WriteLine(line.Replace("\"title\":", ""));
                        }
                    }
                    break;
                // Delete page
                case 3:
                    Console.WriteLine(">>> Delete page.");
                    string pageSlug = "";
                    readField(ref pageSlug, "Page Slug");
                    Console.WriteLine(makeApiRequest("delete/page", "{ \"slug\": \"" + pageSlug + "\"}"));
                    break;
                // Delete article
                case 4:
                    Console.WriteLine(">>> Delete article.");
                    string articleId = "";
                    readField(ref articleId, "Article Id");
                    Console.WriteLine(makeApiRequest("delete/article", "{ \"id\": \"" + articleId + "\"}"));
                    break;
                // New article
                case 5:
                    Console.WriteLine(">>> New article.");
                    string articleTitle = "";
                    string tags = "";
                    string articleContent = "";

                    readField(ref articleTitle, "Title");
                    readField(ref tags, "Tags");
                    readField(ref articleContent, "Content");

                    Console.WriteLine(makeApiRequest("save/article", "{ \"title\": \"" + articleTitle + "\", \"tags\": \"" + tags  + "\", \"content\": \"" + articleContent + "\" }"));
                    break;
                // New page
                case 6:
                    Console.WriteLine(">>> New page.");
                    string pageTitle = "";
                    string pageContent = "";
                    string pageOrder = "";

                    readField(ref pageTitle, "Title");
                    readField(ref pageContent, "Content");
                    readField(ref pageOrder, "Order");


                    Console.WriteLine(makeApiRequest("save/page", "{ \"title\": \"" + pageTitle + "\", \"content\": \"" + pageContent + "\", \"order\": " +  pageOrder+ " }"));
                    break;
                // Sync
                case 7:
                    Console.WriteLine(">>> Syncing " + host + " with git remote server.");
                    Console.WriteLine(makeApiRequest("sync", ""));
                    break;
                // Quit
                case 8:
                    Console.WriteLine("Have a nice day.");
                    Environment.Exit(0);
                    break;
            }
            handleMenuItem(Menu());
        }


        public static void Main(string[] args)
        {
            Console.WriteLine("C# Bloggify Client");
            Console.WriteLine("------------------");
            authToken = getAuthToken();
            Console.WriteLine(">> Authentication token was received: " + authToken);
            handleMenuItem(Menu());
        }
    }
}
