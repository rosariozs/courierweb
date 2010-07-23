using System;
using System.Text.RegularExpressions;

namespace MMarinov.WebCrawler.Indexer
{
    /// <summary>
    /// Storage for parsed HTML data returned by ParsedHtmlData();
    /// </summary>
    /// <remarks>
    /// Arbitrary class to encapsulate just the properties we need 
    /// to index Html pages (Title, Meta tags, Keywords, etc).
    /// </remarks>
    public class HtmlDocument : Document
    {
        #region Private fields: _Uri, _ContentType, _RobotIndexOK, _RobotFollowOK

        private string _htmlCode = "";
        private String _ContentType;
        private bool _RobotIndexOK = true;
        private bool _RobotFollowOK = true;
        private string _WordsOnly = "";
        /// <summary>MimeType so we know whether to try and parse the contents, eg. "text/html", "text/plain", etc</summary>
        private string _MimeType = "";
        /// <summary>Html &lt;title&gt; tag</summary>
        private String _Title = "";
        /// <summary>Html &lt;meta http-equiv='description'&gt; tag</summary>
        private string _Description = "";
        private string _keywords = "";
        private MMarinov.WebCrawler.Stemming.Languages _language = MMarinov.WebCrawler.Stemming.Languages.None;

        private System.Collections.Generic.List<string> linksLocal = new System.Collections.Generic.List<string>();
        private System.Collections.Generic.List<string> linksExternal = new System.Collections.Generic.List<string>();

        #endregion

        #region Constructors

        public HtmlDocument(Uri location)
            : base(location)
        {
            this.Uri = location;
        }

        public HtmlDocument(Uri location, string mimeType, System.Text.Encoding encoding)
            : base(location, mimeType)
        {
            this.Uri = location;
            _MimeType = mimeType;

            if (encoding != null)
            {
                _Encoding = encoding;
            }
        }

        #endregion

        #region Public Properties: Uri, RobotIndexOK
        /// <summary>
        /// Whether a robot should index the text 
        /// found on this page, or just ignore it
        /// </summary>
        /// <remarks>
        /// Set when page META tags are parsed - no 'set' property
        /// More info:
        /// http://www.robotstxt.org/
        /// </remarks>
        public override bool RobotIndexOK
        {
            get { return _RobotIndexOK; }
        }
        /// <summary>
        /// Whether a robot should follow any links 
        /// found on this page, or just ignore them
        /// </summary>
        /// <remarks>
        /// Set when page META tags are parsed - no 'set' property
        /// More info:
        /// http://www.robotstxt.org/
        /// </remarks>
        public override bool RobotFollowOK
        {
            get { return _RobotFollowOK; }
        }


        public MMarinov.WebCrawler.Stemming.Languages Language
        {
            get { return _language; }
        }
        #endregion

        #region Public fields: _Encoding, Keywords, All

        /// <summary>
        /// _Encoding eg. "utf-8", "Shift_JIS", "iso-8859-1", "gb2312", etc
        /// </summary>
        private System.Text.Encoding _Encoding = System.Text.Encoding.Default;

        /// <summary>
        /// Html &lt;meta http-equiv='keywords'&gt; tag
        /// </summary>
        public override string Keywords
        {
            get
            {
                return _keywords;
            }
            set
            {
                _keywords = value.Substring(0, 500);
            }
        }

        public override byte FileType
        {
            get
            {
                return (byte)DocumentTypes.HTML;
            }
        }

        public override string Title
        {
            get
            {
                return _Title;
            }
            set
            {
                _Title = value;
            }
        }

        public override string WordsOnly
        {
            get { return _Title + " " + this._keywords + " " + this._Description + " " + Common.GetAuthority(Uri) + " " + this._WordsOnly; }
        }

        public override string Description
        {
            get
            {
                return _Description;
            }
            set
            {
                _Description = value.Substring(0, 500);
            }
        }

        #endregion

        /// <summary>
        /// Pass in a ROBOTS meta tag found while parsing, 
        /// and set HtmlDocument property/ies appropriately
        /// </summary>
        /// <remarks>
        /// More info:
        /// * Robots Exclusion Protocol *
        /// - for META tags http://www.robotstxt.org/wc/meta-user.html
        /// - for ROBOTS.TXT in the siteroot http://www.robotstxt.org/wc/norobots.html
        /// </remarks>
        public void SetRobotDirective(string robotMetaContent)
        {
            robotMetaContent = robotMetaContent.ToLower();
            if (robotMetaContent.IndexOf("none") >= 0)
            {
                // 'none' means you can't Index or Follow!
                _RobotIndexOK = false;
                _RobotFollowOK = false;
            }
            else
            {
                if (robotMetaContent.IndexOf("noindex") >= 0) { _RobotIndexOK = false; }
                if (robotMetaContent.IndexOf("nofollow") >= 0) { _RobotFollowOK = false; }
            }
        }

        #region Parsing

        /// <summary>
        ///
        /// </summary>
        /// <remarks> Regex on this blog will parse ALL attributes from within tags...
        /// IMPORTANT when they're out of order, spaced out or over multiple lines
        /// http://blogs.worldnomads.com.au/matthewb/archive/2003/10/24/158.aspx
        /// http://blogs.worldnomads.com.au/matthewb/archive/2004/04/06/215.aspx
        /// </remarks>
        public override void Parse()
        {
            if (string.IsNullOrEmpty(this._Title))
            {
                this._Title = ISOtoASCII(Regex.Match(_htmlCode, @"(?<=<title[^\>]*>).*?(?=</title>)", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture).Value);
            }

            //ParseLanguage();
            ParseMetaTags();
            ParseLinks();

            this.LocalLinks = linksLocal;
            this.ExternalLinks = linksExternal;
        }// Parse

        private void ParseLinks()
        {
            string link = "";

            // Looks for the src attribute of:
            // <A> anchor tags
            // <AREA> imagemap links
            // <FRAME> frameset links
            // <IFRAME> floating frames
            foreach (Match match in Regex.Matches(_htmlCode, @"(?<anchor><\s*(a|area|frame|iframe)\s*(?:(?:\b\w+\b\s*(?:=\s*(?:""[^""]*""|'[^']*'|[^""'<> ]+)\s*)?)*)?\s*>)", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture))
            {
                // Parse ALL attributes from within tags... IMPORTANT when they're out of order!!
                // in addition to the 'href' attribute, there might also be 'alt', 'class', 'style', 'area', etc...
                // there might also be 'spaces' between the attributes and they may be ", ', or unquoted
                link = "";

                foreach (Match submatch in Regex.Matches(match.Value.ToString(), @"(?<name>\b\w+\b)\s*=\s*(""(?<value>[^""]*)""|'(?<value>[^']*)'|(?<value>[^""'<> \s]+)\s*)+", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture))
                {
                    // we're only interested in the href attribute (although in future maybe index the 'alt'/'title'?)
                    if ("href" == submatch.Groups[1].ToString().ToLower())
                    {
                        link = submatch.Groups[2].ToString();

                        if (link != "#")
                        {
                            break; // break if this isn't just a placeholder href="#", which implies maybe an onclick attribute exists
                        }
                    }
                    if ("onclick" == submatch.Groups[1].ToString().ToLower())
                    {
                        string jscript = submatch.Groups[2].ToString();
                        // some code here to extract a filename/link to follow from the onclick="_____"
                        // say it was onclick="window.location='top.htm'"
                        int firstApos = jscript.IndexOf("'");
                        int secondApos = jscript.IndexOf("'", firstApos + 1);
                        if (secondApos > firstApos)
                        {
                            link = jscript.Substring(firstApos + 1, secondApos - firstApos - 1);
                            break;  // break if we found something, ignoring any later href="" which may exist _after_ the onclick in the <a> element
                        }
                    }
                }

                Document.FoundTotalLinks++;
                AddLinkToCollection(link);
            } // foreach
        }// Parse Links

        /// <summary>
        /// Gets the content of meta tags and set keywords, description and robot directives
        /// </summary>
        private void ParseMetaTags()
        {
            string metaKey = "";
            string metaValue = "";

            foreach (Match metamatch in Regex.Matches(_htmlCode, @"<meta\s*(?:(?:\b(\w|-)+\b\s*(?:=\s*(?:""[^""]*""|'[^']*'|[^""'<> ]+)\s*)?)*)/?\s*>", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture))
            {
                metaKey = "";
                metaValue = "";
                // Loop through the attribute/value pairs inside the tag
                foreach (Match submetamatch in Regex.Matches(metamatch.Value, @"(?<name>\b(\w|-)+\b)\s*=\s*(""(?<value>[^""]*)""|'(?<value>[^']*)'|(?<value>[^""'<> ]+)\s*)+", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture))
                {
                    switch (submetamatch.Groups[1].ToString().ToLower())
                    {
                        case "http-equiv":
                            metaKey = submetamatch.Groups[2].ToString();
                            break;
                        case "name":
                            if (metaKey == "")
                            { // if it's already set, HTTP-EQUIV takes precedence
                                metaKey = submetamatch.Groups[2].ToString();
                            }
                            break;
                        case "content":
                            metaValue = submetamatch.Groups[2].ToString();
                            break;
                        default: break;
                    }
                }

                switch (metaKey.ToLower())
                {
                    case "description":
                        _Description = ISOtoASCII(metaValue);
                        break;
                    case "keywords":
                    case "keyword":
                        Array.ForEach<string>(base.WordsStringToArray(metaValue), word => _keywords += word + " ");
                        break;
                    case "robots":
                    case "robot":
                        this.SetRobotDirective(metaValue);
                        break;
                    default: break;
                }
            }

        }//Parse MetaTags

        /// <summary>
        /// Parse HTML tag to look for lang or xml:lang tag.
        /// </summary>
        private void ParseLanguage()
        {
            Match htmlMatch = Regex.Match(_htmlCode, @"<html\b(?>\s+(?:alt=""([^""]*)""|lang=""([^""]*)""|xml:lang=""([^""]*)"")|[^\s>]+|\s+)*>", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);

            // Loop through the attribute/value pairs inside the tag
            foreach (Match submetamatch in Regex.Matches(htmlMatch.Value, @"(?<name>\b(\w|-)+\b)\s*=\s*(""(?<value>[^""]*)""|'(?<value>[^']*)'|(?<value>[^""'<> ]+)\s*)+", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture))
            {
                if (submetamatch.Groups[1].Value.ToLower() == "lang")
                {
                    switch (submetamatch.Groups[2].Value.ToLower())
                    {
                        case "en":
                            _language = Stemming.Languages.English;
                            break;
                        case "de":
                            _language = Stemming.Languages.German;
                            break;
                        case "bg":
                            _language = Stemming.Languages.Bulgarian;
                            break;
                        default: break;
                    }
                }
                else if (submetamatch.Groups[1].Value.ToLower() == "xml:lang" && _language == Stemming.Languages.None)
                {
                    switch (submetamatch.Groups[2].Value.ToLower())
                    {
                        case "en":
                            _language = Stemming.Languages.English;
                            break;
                        case "de":
                            _language = Stemming.Languages.German;
                            break;
                        case "bg":
                            _language = Stemming.Languages.Bulgarian;
                            break;
                        default: break;
                    }
                }
            }

        }// ParseLanguage

        /// <summary>
        /// Checks link and adds it to external/local links collection
        /// </summary>
        /// <param name="link"></param>
        private void AddLinkToCollection(string link)
        {
            // strip off internal links, so we don't index same page over again
            if (link.Contains("#"))
            {
                link = link.Substring(0, link.IndexOf("#"));
            }

            //                       !!!!!!!!!!!!!
            if (link.Contains("?"))//!!!!!!!!!!!!!
            {
                link = link.Substring(0, link.IndexOf("?"));
            }

            link = link.Replace('\\', '/');

            if (!Document.IsAWebPage(link))
            {
                return;
            }

            if (link.Length > 1 && !link.StartsWith("/") && (link.StartsWith(Common.HTTP) || link.StartsWith("https://")))
            {
                try
                {
                    Uri address = new Uri(link);
                    string authority = Regex.Replace(address.Authority, Common.MatchWwwDigitDotPattern, "");

                    if (authority == this.Uri.Authority)
                    {
                        if (!linksLocal.Contains(address.PathAndQuery))
                        {
                            linksLocal.Add(address.PathAndQuery); //gets only the relative link
                            Document.FoundValidLinks++;
                        }
                    }
                    else if (!linksExternal.Contains(Common.GetHttpAuthority(address)))
                    {
                        linksExternal.Add(Common.GetHttpAuthority(address));
                        Document.FoundValidLinks++;
                    }
                }
                catch
                {
                    base.DocumentProgressEvent(new Report.ProgressEventArgs(new Exception("Malformed URL: " + link)));
                }
            }
            //else if (link.StartsWith("?"))
            //{
            //    if (!linksLocal.Contains(this.Uri.AbsolutePath + link))
            //    {
            //        // it's possible to have /?query which sends the querystring to the 'default' page in a directory
            //        linksLocal.Add(this.Uri.AbsolutePath + link);
            //    }
            //}
            else if (!linksLocal.Contains(link))
            {
                linksLocal.Add(link);
                Document.FoundValidLinks++;
            }
        }

        #endregion

        private static System.IO.Stream CopyStream(System.IO.Stream inputStream)
        {
            const int readSize = 8192;
            byte[] buffer = new byte[readSize];
            System.IO.MemoryStream ms = new System.IO.MemoryStream();

            int count = inputStream.Read(buffer, 0, readSize);

            while (count > 0)
            {
                ms.Write(buffer, 0, count);
                count = inputStream.Read(buffer, 0, readSize);
            }

            ms.Seek(0, System.IO.SeekOrigin.Begin);
            return ms;
        }


        public override bool GetResponse(System.Net.HttpWebResponse webResponse)
        {
            System.IO.Stream responseStream = null;

            try
            {
                DateTime startDLtime = DateTime.Now;

                responseStream = CopyStream(webResponse.GetResponseStream());
                //stream = new System.IO.StreamReader(webResponse.GetResponseStream(),this._Encoding);

                if (webResponse.ResponseUri != this.Uri)
                {
                    this.Uri = webResponse.ResponseUri; // we *may* have been redirected... and we want the *final* URL

                    if (!base.AddURLtoGlobalVisited(this.Uri))
                    {
                        return false;
                    }
                }

                _htmlCode = new System.IO.StreamReader(responseStream, _Encoding).ReadToEnd();

                if (_htmlCode != "")
                {
                    base.SetDownloadSpeed(_htmlCode.Length / (DateTime.Now - startDLtime).TotalSeconds / 1000);

                    if (_htmlCode.Contains("�") || _htmlCode.Contains("â") || _htmlCode.Contains("") || _htmlCode.Contains("Ã") || _htmlCode.Contains("¼") || _htmlCode.Contains("¤") || _htmlCode.Contains("??????"))
                    {
                        GetMetaEncoding();

                        responseStream.Seek(0, System.IO.SeekOrigin.Begin);
                        _htmlCode = new System.IO.StreamReader(responseStream, _Encoding).ReadToEnd();

                        if (_htmlCode.Contains("�") || _htmlCode.Contains("â") || _htmlCode.Contains("") || _htmlCode.Contains("Ã") || _htmlCode.Contains("¼") || _htmlCode.Contains("¤") || _htmlCode.Contains("??????"))
                        { 
                        }

                        //responseStream.Seek(0, System.IO.SeekOrigin.Begin);
                        //_htmlCode = new System.IO.StreamReader(responseStream, System.Text.Encoding.GetEncoding(webResponse.CharacterSet)).ReadToEnd();
                        //responseStream.Seek(0, System.IO.SeekOrigin.Begin);
                        //_htmlCode = new System.IO.StreamReader(responseStream, System.Text.Encoding.UTF8).ReadToEnd();
                        //responseStream.Seek(0, System.IO.SeekOrigin.Begin);
                        //_htmlCode = new System.IO.StreamReader(responseStream, System.Text.Encoding.Default).ReadToEnd();
                        //responseStream.Seek(0, System.IO.SeekOrigin.Begin);
                        //_htmlCode = new System.IO.StreamReader(responseStream, System.Text.Encoding.GetEncoding("ISO-8859-1")).ReadToEnd();
                    }
                    _WordsOnly = StripHtml(_htmlCode);
                }
            }
            catch (Exception e)
            {
                this.DocumentProgressEvent(new Report.ProgressEventArgs(new Exception(Uri.AbsoluteUri, e)));
                return false;
            }
            finally
            {
                if (responseStream != null)
                {
                    responseStream.Close();
                    responseStream.Dispose();
                }
            }

            return true; //success
        }//GetResponse

        private void GetMetaEncoding()
        {
            string metaKey = "";
            string metaValue = "";

            foreach (Match metamatch in Regex.Matches(_htmlCode, @"<meta\s*(?:(?:\b(\w|-)+\b\s*(?:=\s*(?:""[^""]*""|'[^']*'|[^""'<> ]+)\s*)?)*)/?\s*>", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture))
            {
                metaKey = "";
                metaValue = "";
                // Loop through the attribute/value pairs inside the tag
                foreach (Match submetamatch in Regex.Matches(metamatch.Value, @"(?<name>\b(\w|-)+\b)\s*=\s*(""(?<value>[^""]*)""|'(?<value>[^']*)'|(?<value>[^""'<> ]+)\s*)+", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture))
                {
                    switch (submetamatch.Groups[1].ToString().ToLower())
                    {
                        case "http-equiv":
                            metaKey = submetamatch.Groups[2].ToString();
                            break;
                        case "content":
                            metaValue = submetamatch.Groups[2].ToString();
                            break;
                        default: break;
                    }
                }

                if (metaKey.ToLower() == "content-type")
                {
                    System.Text.Encoding encodingMeta = DocumentFactory.ParseEncoding(metaValue);

                    if (encodingMeta != null && !encodingMeta.Equals(_Encoding))
                    {
                        // Convert the string into a byte[].
                        byte[] srcBytes = _Encoding.GetBytes(_htmlCode);

                        // Perform the conversion from one encoding to the other.
                        byte[] destBytes = System.Text.Encoding.Convert(_Encoding, encodingMeta, srcBytes);

                        // Convert the new byte[] into a char[] and then into a string.
                        char[] destChars = new char[_Encoding.GetCharCount(destBytes, 0, destBytes.Length)];
                        encodingMeta.GetChars(destBytes, 0, destBytes.Length, destChars, 0);
                        _htmlCode = new string(destChars);
                        _Encoding = encodingMeta;
                        break;
                    }
                }
            }

        }

        /// <summary>
        /// Stripping HTML
        /// http://www.4guysfromrolla.com/webtech/042501-1.shtml
        /// </summary>
        /// <remarks>
        /// Using regex to find tags without a trailing slash
        /// http://concepts.waetech.com/unclosed_tags/index.cfm
        ///
        /// Replace html comment tags
        /// http://www.faqts.com/knowledge_base/view.phtml/aid/21761/fid/53
        /// </remarks>
        protected string StripHtml(string htmlCode)
        {
            const string matchCommentPattern = @"(\<![ \r\n\t]*(--([^\-]|[\r\n]|-[^\-])*--[ \r\n\t]*)\>)";

            htmlCode = Regex.Replace(htmlCode, "&amp;", "&", RegexOptions.IgnoreCase | RegexOptions.Singleline);

            //Strips the <script> and <noscript> tags, comments <!-- --> , <style>
            htmlCode = Regex.Replace(htmlCode, MatchTag("script") + "|" + MatchTag("noscript") + "|" + matchCommentPattern + "|" + MatchTag("style"), " ", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            // htmlCode = Regex.Replace(htmlCode, @"(<script.*?</script>)|(<noscript.*?</noscript>)|(\<![ \r\n\t]*(--([^\-]|[\r\n]|-[^\-])*--[ \r\n\t]*)\>)|(<style.*?</style>)", " ", RegexOptions.Singleline | RegexOptions.IgnoreCase);      

            //removes tags, new lines and multiple spaces 
            htmlCode = Regex.Replace(htmlCode, "<(.|\n)*?>", " ");
            // new lines and multiple spaces 
            htmlCode = Regex.Replace(htmlCode, "(&(.|\n)+?;)|(\r?\n?)", "");
            htmlCode = Regex.Replace(htmlCode, Common.MatchEmptySpacesPattern, " ");

            htmlCode = ISOtoASCII(htmlCode);

            return htmlCode;
        }//stripHtml


        private string MatchTag(string tagName)
        {
            return @"(\<[ \r\n\t]*" + tagName + @"([ \r\n\t\>]|\>){1,}([ \r\n\t]|.)*</[ \r\n\t]*" + tagName + @"[ \r\n\t]*\>)";
        }
    }
}
