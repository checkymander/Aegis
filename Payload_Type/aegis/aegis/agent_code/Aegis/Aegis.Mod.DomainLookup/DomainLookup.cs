using Aegis.Models.Interfaces;

namespace Aegis.Mod.DomainLookup
{
    public class DomainLookup : IMod
    {
        private Random rng = new Random();
        private HttpClient httpClient = new HttpClient();

        private void Shuffle(List<string> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                string value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
        public async Task<bool> Check()
        {
            List<string> j = new() {
                "https://www.google.com",
                "https://www.facebook.com",
                "https://www.wikipedia.com",
                "https://www.reddit.com",
                "https://www.x.com",
                "https://www.youtube.com",
                "https://www.instagram.com",
                "https://www.yahoo.com",
                "https://www.cnn.com",
                "https://www.msnbc.com",
                "https://www.fox.com",
                "https://www.foxnews.com",
                "https://www.chatgpt.com",
                "https://www.netflix.com",
                "https://www.bing.com",
                "https://www.linkedin.com",
                "https://www.office.com",
                "https://www.amazon.com",
                "https://www.weather.com"
            };

            Shuffle(j);
            return await GetRequests(j);
        }
        private async Task<bool> GetRequests(List<string> urls)
        {
            foreach (string url in urls)
            {
                try
                {
                    HttpResponseMessage response = await httpClient.GetAsync(url);
                    string responseBody = await response.Content.ReadAsStringAsync();
                }
                catch (HttpRequestException e)
                {
                    //Fail out if any of the domain lookups don't work. Is this good or bad?
                    return false;
                }

                Thread.Sleep(10000);
            }
            return true;
        }
    }
}
