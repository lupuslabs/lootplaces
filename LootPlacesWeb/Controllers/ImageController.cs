using System.Text;
using Microsoft.AspNetCore.Mvc;
using SkiaSharp;
using Svg.Skia;

namespace LootPlacesWeb
{
    [ApiController]
    public class ImageController : AppControllerBase
    {
        readonly string[] _domainsSet = new[] {
            "tesla.com", "starwars.com", "garyvaynerchuk.com", "billieeilish.com", "nsa.gov", "youtube.com/user/PewDiePie", "spotify.com", "apple.com", "paypal.com", "facebook.com", "netflix.com",
            "ethereum.org", "axieinfinity.com", "pokemoncenter.com", "minecraft.net", "berniesanders.com", "beeple-crap.com", "opensea.io", "sothebys.com", "christies.com", "microsoft.com",
            "time.com", "coinbase.com", "007.com", "yahoo.com", "imdb.com", "ebay.com", "etsy.com", "craigslist.org", "steemit.com", "huffpost.com", "buzzfeed.com", "cnn.com", "medium.com",
            "youtube.com", "nytimes.com", "espn.com", "hulu.com", "github.com", "stackoverflow.com", "giphy.com", "4chan.org", "nasa.gov", "wsj.com", "roblox.com", "chess.com", "blackrock.com",
            "worldbank.org", "nestle.com", "ethgasstation.info", "3lau.com", "banklesshq.com", "thedailygwei.substack.com", "forbes.com/crypto-blockchain", "a16z.com", "draper.vc", "veefriends.com",
            "microstrategy.com", "robinhood.com", "gemini.com", "coinmarketcap.com", "cointelegraph.com", "decrypt.co", "niftygateway.com", "beta.cent.co", "mintable.app", "trevornoah.com",
            "twitter.com/jack", "twitter.com/hamillhimself", "twitter.com/elonmusk", "messi.com", "50cent.com", "loganpaul.com", "twitch.tv/trymacs", "sequoiacap.com", "earlybird.com",
            "indexventures.com", "parsec.finance", "federalreserve.gov", "sec.gov", "fridaysforfuture.org", "ipcc.ch", "venturebeat.com", "greyscale.co", "britannica.com", "goldmansachs.com",
            "disney.com", "en.wikipedia.org/wiki/Rocket_Raccoon", "xkcd.com", "akb48.co.jp", "tvtropes.org", "reddit.com/r/Bitcoin", "reddit.com/r/ethereum", "cyberpunk.net", "mario.nintendo.com",
            "thelastofus.fandom.com/wiki/Ellie", "epicgames.com/fortnite", "gatherer.wizards.com", "kraken.com", "deviantart.com/tag/chloeprice", "larvalabs.com/cryptopunks", "linkedin.com",
            "messari.io", "imgur.com", "samsung.com", "fandom.com", "geeksforgeeks.org", "discordapp.com", "wellsfargo.com", "fidelity.com", "investopedia.com", "theoceancleanup.com",
            "seashepherd.org", "greenpeace.org", "gitcoin.co", "uniswap.org", "aave.com", "chain.link", "makerdao.com", "bitcoin.org", "polygon.technology", "filecoin.io", "thegraph.com", "sushi.com",
            "wired.com", "4ocean.com", "cookpad.com", "allrecipies.com", "delish.com", "thekitchn.com"
        };

        readonly string[] _resourceSet = new[] { "Rocks", "Minerals", "Ore", "Waste", "Relics", "Rare earths", "Precious metals", "Gems" };
        readonly string[] _securitySet = new[] { "Policed", "Factional", "Lowsec", "Controlled", "Dark", "Secured", "Anarchy", "Safe" };
        readonly string[] _magicSet = new[] { "Silent", "Gaia", "Kami", "Elemental", "Arcane", "Wizardry", "Devine", "Demonic" };
        readonly string[] _factionSet = new[] { "Brass", "Obsidian", "Chrome", "Opal", "Lava", "Rubin", "Coral", "Jade" };
        readonly string[] _infoSet = new[] { "Unconnected", "Intermittent", "Wired", "Ubiquitous", "Embedded", "Matrix", "Quantum", "Transcend" };
        readonly string[] _environmentSet = new[] { "Supportive", "Deep", "Brilliant", "Vivid", "Volatile", "Ascetic", "Corrosive", "Lush" };
        readonly string[] _strengthSet = new[] { "900000", "800000", "700000", "600000", "500000", "400000", "300000", "200000", "100000" };
        readonly Random _r = new Random((int)(DateTime.UtcNow.Ticks % int.MaxValue));

        public ImageController(MyApp app) : base(app) { }

        //[HttpGet]
        //[Route("[controller]")]
        //public ActionResult Get(string domain, string size)
        //{
        //    domain ??= GetProperty(_domainsSet);
        //    var nSize = int.Parse(size ?? "400");

        //    var s = "<!DOCTYPE html>";
        //    s += "<html lang='en'><head><title>LootPlacesImage</title></head><body>";
        //    Don.t = () => {
        //        s += "<img src='data:image/svg+xml;base64," + Base64.Encode(GetSvg(domain)) + "' style='width:" + nSize + "px; height:" + nSize + "px' />";
        //    };
        //    s += "<img src='data:image/png;base64," + Base64.Encode(GetPng(domain)) + "' style='width:" + nSize + "px; height:" + nSize + "px' />";
        //    s += "</body>";
        //    s = s.Replace("'", "\"");
        //    return Content(s, "text/html");
        //}

        [HttpGet]
        [Route("[controller]/[action]")]
        public ActionResult Svg(string domain)
        {
            Log.Info("", new LogData { [nameof(domain)] = domain });
            return Content(GetSvg(domain), "image/svg+xml");
        }

        [HttpGet]
        [Route("[controller]/[action]")]
        public ActionResult Png(string domain)
        {
            Log.Info("", new LogData { [nameof(domain)] = domain });
            return File(GetPng(domain), "image/png");
        }

        string GetSvg(string domain)
        {
            var y = 0;
            var dy = 30;

            y += dy;
            var s = "<svg xmlns='http://www.w3.org/2000/svg' preserveAspectRatio='xMinYMin meet' viewBox='0 0 350 350'><style>.base { fill: white; font-family: serif; font-size: 24px; }</style><rect width='100%' height='100%' fill='black' />";
            s += "<text x='10' y='" + y + "' class='base'>";
            s += domain;

            y += dy;
            s += "</text><text x='10' y='" + y + "' class='base'>";

            y += dy;
            s += GetStrength();
            s += "</text><text x='10' y='" + y + "' class='base'>";

            y += dy;
            s += GetResource();
            s += "</text><text x='10' y='" + y + "' class='base'>";

            y += dy;
            s += GetSecurity();
            s += "</text><text x='10' y='" + y + "' class='base'>";

            y += dy;
            s += GetMagic();
            s += "</text><text x='10' y='" + y + "' class='base'>";

            y += dy;
            s += GetFaction();
            s += "</text><text x='10' y='" + y + "' class='base'>";

            y += dy;
            s += GetInfo();
            s += "</text><text x='10' y='" + y + "' class='base'>";

            y += dy;
            s += GetEnvironment();
            s += "</text></svg>";

            s = s.Replace("'", "\"");
            return s;
        }

        private byte[] GetPng(string domain)
        {
            var svgData = GetSvg(domain);
            using var inStream = new MemoryStream(Encoding.UTF8.GetBytes(svgData ?? ""));
            var svg = new /*SkiaSharp.Extended.Svg.*/SKSvg();
            svg.Load(inStream);
            if (svg.Drawable == null) throw new Exception("svg.Load failed");

            var bitmap = new SKBitmap((int)svg.Drawable.Bounds.Width, (int)svg.Drawable.Bounds.Height);
            var canvas = new SKCanvas(bitmap);
            canvas.DrawPicture(svg.Picture);
            canvas.Flush();
            canvas.Save();

            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 80);
            using var svgStream = data.AsStream();
            using var outStream = new MemoryStream();
            svgStream.CopyTo(outStream);
            return outStream.ToArray();
        }

        private string GetProperty(string[] set)
        {
            return set[_r.Next(set.Length)];
        }

        private string GetEnvironment() { return GetProperty(_environmentSet); }
        private string GetInfo() { return GetProperty(_infoSet); }
        private string GetFaction() { return GetProperty(_factionSet); }
        private string GetMagic() { return GetProperty(_magicSet); }
        private string GetSecurity() { return GetProperty(_securitySet); }
        private string GetResource() { return GetProperty(_resourceSet); }
        private string GetStrength() { return GetProperty(_strengthSet); }
    }
}