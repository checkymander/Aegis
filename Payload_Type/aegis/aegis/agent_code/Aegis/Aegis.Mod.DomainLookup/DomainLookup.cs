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
                "google.com",
                "facebook.com",
                "wikipedia.com",
                "reddit.com",
                "x.com",
                "youtube.com",
                "instagram.com",
                "yahoo.com",
                "cnn.com",
                "msnbc.com",
                "fox.com",
                "foxnews.com",
                "chatgpt.com",
                "microsoftonline.com",
                "netflix.com",
                "bing.com",
                "linkedin.com",
                "office.com",
                "amazon.com",
                "weather.com"
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
