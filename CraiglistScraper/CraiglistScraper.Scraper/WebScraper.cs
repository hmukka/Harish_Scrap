using CraiglistScraper.Scraper.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CraiglistScraper.Scraper
{
    public class WebScraper
    {
        public Dictionary<string, string> ScrapeLocations(string pageString)
        {
            var linkRegex = new Regex("<li><a href=\"https(.+?)</a></li>");
            var matches = linkRegex.Matches(pageString);
            var locations = new Dictionary<string, string>();
            if (matches.Count > 0)
            {
                foreach (var match in matches)
                {
                    var link = string.Empty;
                    var location = string.Empty;
                    var li = match.ToString();
                    var regex = new Regex("<a href=\"(.+?)\">");
                    var mtch = regex.Match(li);
                    if (mtch.Success)
                    {
                        link = "https:" +  mtch.ToString().Replace("<a href=\"", "").Replace("\">", "");
                    }
                    regex = new Regex("\">(.+?)</a></li>");
                    mtch = regex.Match(li);
                    if (mtch.Success)
                    {
                        location = mtch.ToString().Replace("</a></li>", "").Replace("\">", "");
                    }
                    if (location.Equals("calgary")) break;
                    if (!string.IsNullOrEmpty(location) && !locations.ContainsKey(location))
                    {
                        locations.Add(location, link);
                    }
                }
            }

            return locations;
        }

        public List<PostLink> ScrapePostLinks(string rootUrl, string pageString)
        {
            var linkRegex = new Regex("<a href=\"(.+?).html\"");
            var matches = linkRegex.Matches(pageString);
            var posts = new List<PostLink>();
            if (matches.Count > 0)
            {
                foreach (var match in matches)
                {
                    var link = string.Empty;
                    var title = string.Empty;
                    var li = match.ToString();
                    var regex = new Regex("<a href=\"(.+?).html\"");
                    var mtch = regex.Match(li);
                    if (mtch.Success)
                    {
                        if(match.ToString().Contains("craigslist.org"))
                            link = "https:" + mtch.ToString().Replace("<a href=\"", "").Replace("\"", "");
                        else
                            link = rootUrl.Substring(0, rootUrl.Length - 1) + mtch.ToString().Replace("<a href=\"", "").Replace("\"", "");
                    }
                    regex = new Regex("<span id=\"titletextonly\">(.+?)</span></a>");
                    mtch = regex.Match(li);
                    if (mtch.Success)
                    {
                        title = mtch.ToString().Replace("<span id=\"titletextonly\">", "").Replace("</span></a>", "");
                    }
                    posts.Add(new PostLink { Title = RemoveHtml(title), Url = link});
                }
            }
            return posts;
        }

        public string GetReplyLink(string rootUrl, string pageString)
        {
            var linkRegex = new Regex("<a id=\"replylink\" href=\"(.+?)\">");
            var match = linkRegex.Match(pageString);
            if (!match.Success) return string.Empty;
            return "http://" + rootUrl.Split('/')[2] + match.ToString().Replace("<a id=\"replylink\" href=\"", "").Replace("\">", "");
        }

        public string GetPhone(string pageString)
        {
            var linkRegex = new Regex("[^.!?;]*(&#9742;)[^.!?;]*");
            var match = linkRegex.Match(pageString);
            if (!match.Success) return string.Empty;
            var newlinkRegex = new Regex("[^.!?;]*(&#9742;)[^.!?;]*");
            var newlinkRegex1 = new Regex("[^.!?;]*(reply-tel-link)[^.!?;]*");
            var matchnew = newlinkRegex.Match(match.ToString());
            var matchnew1 = newlinkRegex1.Match(match.ToString());
            var test = matchnew.ToString().Replace(matchnew1.ToString(), "");
            return test.Replace("; ", "").Replace("\next", "").Trim();
        }

        public string GetEmail(string pageString)
        {
            var linkRegex = new Regex("class=\"mailapp\">(.+?)</a></li>");
            var match = linkRegex.Match(pageString);
            if (!match.Success) return string.Empty;
            return match.ToString().Replace("class=\"mailapp\">", "").Replace("</a></li>", "").Replace("&amp;", "&").Trim();
        }

        public string GetPostTime(string pageString)
        {
            var linkRegex = new Regex("<time class=\"timeago\"(.+?)</time>");
            var match = linkRegex.Match(pageString);
            if (!match.Success) return string.Empty;
            var posted = match.ToString();
            posted = posted.Substring(posted.LastIndexOf("\">")).Replace("</time>", "").Replace("\"", "").Replace(">", "").Replace("am", " AM").Replace("pm", " PM").Trim();
            return posted;
        }

        public string GetCategory(string pageString)
        {
            var linkRegex = new Regex("<li class=\"crumb category\">[\n](.+?)<p>[\n]*(.+?)</a>");
            var match = linkRegex.Match(pageString);
            if (!match.Success) return string.Empty;
            var posted = match.ToString();
            posted = posted.Substring(posted.LastIndexOf("\">")).Replace("</a>", "").Replace("\"", "").Replace(">", "").Replace("&amp;", "&").Trim();
            return posted;
        }

        public string GetBody(string pageString)
        {
            if (!pageString.Contains("<section id=\"postingbody\">") && !pageString.Contains("</section>"))
                return string.Empty;
            var body = pageString.Substring(pageString.IndexOf("<section id=\"postingbody\">"));
            body = body.Substring(0, body.IndexOf("</section>"));
            body = body.Replace("</section>", "").Replace("id=\"postingbody\"", "").Replace("<section", "").Replace(">", "").Trim();
            return RemoveHtml(body);
        }

        private string RemoveHtml(string u)
        {
            u = u.Replace("<b>", "").Replace("Â", "").Replace("&#8211;", "-")
                .Replace("</b>", "").Replace("</ b>", "").Replace("<p>", "").Replace("</p>", "").Replace("&#160;", "")
                .Replace("<strong>", "").Replace("</strong>", "").Replace("</ strong>", "").Replace("&quot;", "\"")
                .Replace("&quot;", "\"").Replace("&nbsp;", " ").Replace("&amp;", "&").Replace("&copy;", "©")
                .Replace("&gt;", ">").Replace("&lt;", "<").Replace("<span>", " ").Replace("</span>", " ")
                .Replace("<em>", " ").Replace("</em>", " ").Replace("</ em>", " ").Replace("<br>", " ").Replace("</br>", " ")
                .Replace("<br/>", " ").Replace("<br />", " ").Replace("<label>", "").Replace("</label>", "")
                .Replace("<div>", "").Replace("</div>", "").Replace("<h1>", "").Replace("</h1>", "")
                .Replace("<h2>", "").Replace("</h2>", "").Replace("<h3>", "").Replace("</h3>", "")
                .Replace("<h4>", "").Replace("</h4>", "").Replace("<h5>", "").Replace("</h5>", "").Replace("<h6>", "")
                .Replace("</h6>", "").Replace("<font>", "").Replace("</font>", "").Replace("<li>", "").Replace("</li>", "")
                .Replace("<ol>", "").Replace("</ol>", "").Replace("<ul>", "").Replace("</ul>", "").Replace("<u>", "")
                .Replace("</u>", "").Replace("&raquo;", "").Replace("<table>", "").Replace("</table>", "")
                .Replace("<tr>", "").Replace("</tr>", "").Replace("<td>", "").Replace("</td>", "").Replace("<th>", "")
                .Replace("</th>", "").Replace("<tbody>", "").Replace("</tbody>", "").Replace("<thead>", "").Replace("</thead>", "")
                .Replace("<center>", "").Replace("</center>", "").Replace("&ndash;", "-").Replace("&mdash;", "—")
                .Replace("&iexcl;", "¡").Replace("&iquest;", "¿").Replace("&quot;", "\"")
                .Replace("&ldquo;", "“").Replace("&rdquo;", "”").Replace("&#39;", "'").Replace("&lsquo;", "‘")
                .Replace("&rsquo;", "’").Replace("&laquo;", "«").Replace("&raquo;", "»").Replace("&cent", "¢")
                .Replace("&divide;", "÷").Replace("&micro;", "µ").Replace("&middot;", "·").Replace("&para;", "¶")
                .Replace("&plusmn;", "±").Replace("&euro;", "€").Replace("&pound;", "£").Replace("&reg", "®")
                .Replace("</object>;", "").Replace("&sect;", "§").Replace("&trade", "™").Replace("&yen;", "¥")
                .Replace("&deg;", "°").Replace("<object>;", "").Replace("&aacute;", "á").Replace("&Aacute;", "Á")
                .Replace("<i>", "").Replace("</i>", "").Replace("<small>;", "").Replace("</small>", "").Replace("</area>", "")
                .Replace("</audio>", "").Replace("</map>", "").Replace("</video>", "").Replace("</button>", "")
                .Replace("</fieldset>", "").Replace("</form>", "").Replace("</label>", "").Replace("</select>", "")
                .Replace("</option>", "").Replace("</optgroup>", "").Replace("</textarea>", "").Replace("</a>", "").Replace("Ã¢â,", "")
                .Replace("¬â€", "").Replace("¬Å", "").Replace("¬â", "").Replace("&#8217;", "'").Replace("&hellip;", "..")
                .Replace("&#8212;", "—").Replace("&#161;", "¡").Replace("&#191;", "¿").Replace("&#34;", "\"")
                .Replace("&#8220;", "“").Replace("&#8221;", "”").Replace("&#39;", "'").Replace("&#8216", "‘")
                .Replace("&#171;", "«").Replace("&#187;", "»").Replace("&#38;", "&").Replace("&#162;", "¢").Replace("&#169;", "©")
                .Replace("&#247;", "÷").Replace("&#62;", ">").Replace("&#60;", "<").Replace("Ã", "").Replace("¢s", "")
                .Replace("</iframe>", "").Replace("¬Å", "").Replace("Ã‚Â", "")
                .Replace("Ã‚Â²", "").Replace("¦", " ").Replace("</img>", "").Replace("</abbr>", "").Replace("<sup>", "")
                .Replace("</sup>", "").Replace("</g:plusone>", "").Replace("&#x000A9;", "©").Replace("Ã¢â‚", "").Replace("¬Â", "")
                .Replace("&#8230;", " ").Replace("Ã¢â‚¬â„", "").Replace("Â", "").Replace("</noscript>", "").Replace("</body>", "").Replace("<br", "");
            u = Regex.Replace(u, "<img(.*?)>", " ");
            u = Regex.Replace(u, "<img(.*?)>", " ");
            u = Regex.Replace(u, "<input(.*?)>", " ");
            u = Regex.Replace(u, "<a(.*?)</a>", " ");
            u = Regex.Replace(u, "<a(.*?)</ a>", " ");
            u = Regex.Replace(u, "<span(.*?)>", " ");
            u = Regex.Replace(u, "<p(.*?)>", " ");
            u = Regex.Replace(u, "<div(.*?)>", " ");
            u = Regex.Replace(u, "<font(.*?)>", " ");
            u = Regex.Replace(u, "<li(.*?)>", " ");
            u = Regex.Replace(u, "<ul(.*?)>", " ");
            u = Regex.Replace(u, "<ol(.*?)>", " ");
            u = Regex.Replace(u, "<u(.*?)>", " ");
            u = Regex.Replace(u, "<!--(.*?)-->", " ");
            u = Regex.Replace(u, "<script(.*?)</script>", " ");
            u = Regex.Replace(u, "<table(.*?)>", " ");
            u = Regex.Replace(u, "<tbody(.*?)>", " ");
            u = Regex.Replace(u, "<thead(.*?)>", " ");
            u = Regex.Replace(u, "<tr(.*?)>", " ");
            u = Regex.Replace(u, "<td(.*?)>", " ");
            u = Regex.Replace(u, "<th(.*?)>", " ");
            u = Regex.Replace(u, "<h1(.*?)>", " ");
            u = Regex.Replace(u, "<h2(.*?)>", " ");
            u = Regex.Replace(u, "<h3(.*?)>", " ");
            u = Regex.Replace(u, "<h4(.*?)>", " ");
            u = Regex.Replace(u, "<h5(.*?)>", " ");
            u = Regex.Replace(u, "<h6(.*?)>", " ");
            u = Regex.Replace(u, "<a(.*?)>", " ");
            u = Regex.Replace(u, "<center(.*?)>", " ");
            u = Regex.Replace(u, "<address(.*?)>", " ");
            u = Regex.Replace(u, "<em(.*?)>", " ");
            u = Regex.Replace(u, "<cite(.*?)>", " ");
            u = Regex.Replace(u, "<code(.*?)>", " ");
            u = Regex.Replace(u, "<area(.*?)>", " ");
            u = Regex.Replace(u, "<map(.*?)>", " ");
            u = Regex.Replace(u, "<audio(.*?)>", " ");
            u = Regex.Replace(u, "<video(.*?)>", " ");
            u = Regex.Replace(u, "<small(.*?)>", " ");
            u = Regex.Replace(u, "<object(.*?)>", " ");
            u = Regex.Replace(u, "<button(.*?)>", " ");
            u = Regex.Replace(u, "<fieldset(.*?)>", " ");
            u = Regex.Replace(u, "<form(.*?)>", " ");
            u = Regex.Replace(u, "<label(.*?)>", " ");
            u = Regex.Replace(u, "<select(.*?)>", " ");
            u = Regex.Replace(u, "<optgroup(.*?)>", " ");
            u = Regex.Replace(u, "<option(.*?)>", " ");
            u = Regex.Replace(u, "<textarea(.*?)>", " ");
            u = Regex.Replace(u, "<iframe(.*?)>", " ");
            u = Regex.Replace(u, "<abbr(.*?)>", " ");
            u = Regex.Replace(u, "<g:plusone(.*?)>", " ");
            u = Regex.Replace(u, "<noscript(.*?)>", " ");
            u = Regex.Replace(u, "<body(.*?)>", "");
            return u;
        }
    }
}
