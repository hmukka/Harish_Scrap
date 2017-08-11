using CraiglistScraper.Scraper;
using CraiglistScraper.Scraper.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CraiglistScraper
{
    public partial class Form1 : Form
    {
        private Dictionary<string, string> _locations;
        private bool _stopped = false;
        int number = 1;
        public Form1()
        {
            InitializeComponent();
            Init();
        }

        #region [Functions]

        private void Init()
        {
            //Width = 1627;
            _locations = new Dictionary<string, string>();
            resultDataGridView.RowCount = 5000;            
        }

        private void ClearGrid()
        {
            foreach (DataGridViewRow row in resultDataGridView.Rows)
            {
                foreach (DataGridViewCell col in row.Cells)
                {
                    col.Value = "";
                }
            }
            //postsLabel.Text = urlsLabel.Text = emailsLabel.Text = phonesLabel.Text = @"0";
            statusLabel.Text = @"Ready";
        }

        private string DownloadPageString(string url)
        {
            try
            {
                using (var webClient = new WebClient())
                {
                    webClient.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");
                    var pageString = webClient.DownloadString(url);
                    return pageString;
                }
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        private void SearchLocations()
        {
            var page = DownloadPageString("https://www.craigslist.org/about/sites");
            var scraper = new WebScraper();
            var locs = new List<string>();
            _locations =scraper.ScrapeLocations(page);
            if (_locations.Count > 0)
                locationComboBox.Invoke(new Action(() =>  locationComboBox.Items.Clear()));
                //locationComboBox.Items.Clear();
            foreach (var location in _locations.Keys)
            {
                if (locs.Contains(location.Trim())) continue;
                locationComboBox.Invoke(new Action(() => locationComboBox.Items.Add(location.Trim())));
                //locationComboBox.Items.Add(location.Trim());
                locs.Add(location.Trim());
            }
        
                locationComboBox.Invoke(new Action(() => locationComboBox.SelectedIndex = 0));
        
             statusLabel.Invoke(new Action(() => statusLabel.Text = @"Finished"));
            //startButton.Enabled = stopButton.Enabled = clearButton.Enabled = exportButton.Enabled = true;
        }

        private List<string> GetSearchCategoryLinks(string query, int number)
        {
            string MaxValueGiven = "Y";
            string MinValueGiven = "Y";
            int MinPrice = 0;
            int MaxPrice = 0;

            var categories = new List<string> { "cto" };
            int pages = number * 120 + 1;
           // if (!_locations.ContainsKey(locationComboBox.SelectedItem.ToString())) return new List<string>();
            var links = new List<string>();
            string root = "";
            locationComboBox.Invoke(new Action(() =>root =  locationComboBox.SelectedItem.ToString()));
            var rootUrl = _locations[root];
            var baseurl = rootUrl.Replace("https:https://", "https://");
            if(String.IsNullOrEmpty(MaxValue.Text))
            {
                MaxValueGiven = "N";
            }
            else
            {
                MaxPrice = Convert.ToInt32(MaxValue.Text);
            }

            if (String.IsNullOrEmpty(MinValue.Text))
            {
                MinValueGiven = "N";
            }
            else
            {
                MinPrice = Convert.ToInt32(MinValue.Text);
            }

            foreach (var cat in categories)
            {
                for (var i = Convert.ToInt32(From.Text)*120; i <= Convert.ToInt32(To.Text)*120; i += 120)
                {
                    if (MaxValueGiven == "Y" && MinValueGiven == "Y")
                    {
                        links.Add(baseurl + "search/" + cat + "?s=" + i + "&max_price=" +MaxPrice +"&min_price=" + MinPrice);
                    }
                    else if(MaxValueGiven == "Y")
                    {
                        links.Add(baseurl + "search/" + cat + "?s=" + i + "&max_price=" + MaxPrice );
                    }
                    else if (MinValueGiven == "Y")
                    {
                        links.Add(baseurl + "search/" + cat + "?s=" + i + "&min_price=" + MinPrice);
                    }
                }                
            }
            return links;
        }

        private void SetCountStatus(Label lbl, int count)
        {
            lbl.Text = count.ToString();
        }

        private void Search()
        {
            try
            {
                if (string.IsNullOrEmpty(MinValue.Text))
                {
                    MessageBox.Show(@"Please enter search term before searching");
                    return;
                }

                //ClearGrid();
                var scraper = new WebScraper();
                var query = MinValue.Text;
                var categoryLinks = GetSearchCategoryLinks(query , number);
                string root = "";
                locationComboBox.Invoke(new Action(() => root = locationComboBox.SelectedItem.ToString()));
                var rootUrl = _locations[root];
                var baseurl = rootUrl.Replace("https:https://", "https://");
                var index = 0;
                int postCount = 0, urlCount = 0, phoneCount = 0, emailCount = 0;
                foreach (var link in categoryLinks)            {
                    if (_stopped) break;
                    var catPage = DownloadPageString(link);
                    var posts = scraper.ScrapePostLinks(baseurl, catPage);
                    var test = posts.Select(x => x.Url).Distinct().ToList();
                    foreach (var post in test.Distinct().ToList())
                    {
                        if (_stopped) break;
                       // statusLabel.Text = @"Currently searching " + post.Title + @" for " + query + @"..";
                        var postPage = DownloadPageString(post);
                        var replyLink = scraper.GetReplyLink(post, postPage);
                        var replyPage = DownloadPageString(replyLink);
                        var tit = posts.Where(x => x.Url == post).Select(y => y.Title).FirstOrDefault();
                        var postDate = new Post
                        {
                            Url = post,
                            Title = tit,
                            Time = scraper.GetPostTime(postPage),
                            Category = scraper.GetCategory(postPage),
                            Price = scraper.GetPrice(postPage),
                            City = root.ToString(),
                            Email = scraper.GetEmail(replyPage),
                            Phone = scraper.GetPhone(replyPage),
                            Body = scraper.GetBody(postPage)
                        };

                        //SetCountStatus(postsLabel, string.IsNullOrEmpty(postDate.Body) ? postCount : ++postCount);
                        //SetCountStatus(urlsLabel, string.IsNullOrEmpty(postDate.Url) ? urlCount : ++urlCount);
                        //SetCountStatus(emailsLabel, string.IsNullOrEmpty(postDate.Email) ? emailCount : ++emailCount);
                        //SetCountStatus(phonesLabel, string.IsNullOrEmpty(postDate.Phone) ? phoneCount : ++phoneCount);

                        resultDataGridView.Rows[index].Cells[0].Value = (index + 1).ToString();
                        resultDataGridView.Rows[index].Cells[1].Value = postDate.Category;
                        resultDataGridView.Rows[index].Cells[2].Value = postDate.City;
                        resultDataGridView.Rows[index].Cells[3].Value = postDate.Phone;
                        resultDataGridView.Rows[index].Cells[4].Value = postDate.Email;
                        resultDataGridView.Rows[index].Cells[7].Value = postDate.Price;
                        resultDataGridView.Rows[index].Cells[5].Value = postDate.Body;
                        resultDataGridView.Rows[index].Cells[6].Value = postDate.Url;
                        resultDataGridView.Rows[index].Cells[8].Value = postDate.Time;
                        index++;
                    }
                }
                searchButton.Enabled = searchButton.Enabled = clearButton.Enabled = exportButton.Enabled = true;
                //statusLabel.Text = @"Finished";
                _stopped = false;
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception + "");
            }
        }


        private void NewSearch()
        {
            //try
            //{


            //    //ClearGrid();
            //    var scraper = new WebScraper();
            //    var query = searchTextBox.Text;
            //    var categoryLinks = "https://harrisburg.craigslist.org/search/cto/";
            //    var rootUrl = "https://harrisburg.craigslist.org/";
            //    var index = 0;
            //    int postCount = 0, urlCount = 0, phoneCount = 0, emailCount = 0;


            //    var catPage = DownloadPageString(categoryLinks);
            //    var posts = scraper.ScrapePostLinks(rootUrl, catPage);
            //    foreach (var post in posts)
            //    {
            //        if (_stopped) break;
            //        statusLabel.Text = @"Currently searching " + post.Title + @" for " + query + @"..";
            //        var postPage = DownloadPageString(post.Url);
            //        var replyLink = scraper.GetReplyLink(post.Url, postPage);
            //        var replyPage = DownloadPageString(replyLink);
            //        var postDate = new Post
            //        {
            //            Url = post.Url,
            //            Title = post.Title,
            //            Time = scraper.GetPostTime(postPage),
            //            Category = scraper.GetCategory(postPage),
            //            City = locationComboBox.SelectedItem.ToString(),
            //            Email = scraper.GetEmail(replyPage),
            //            Phone = scraper.GetPhone(replyPage),
            //            Body = scraper.GetBody(postPage)
            //        };

            //        SetCountStatus(postsLabel, string.IsNullOrEmpty(postDate.Body) ? postCount : ++postCount);
            //        SetCountStatus(urlsLabel, string.IsNullOrEmpty(postDate.Url) ? urlCount : ++urlCount);
            //        SetCountStatus(emailsLabel, string.IsNullOrEmpty(postDate.Email) ? emailCount : ++emailCount);
            //        SetCountStatus(phonesLabel, string.IsNullOrEmpty(postDate.Phone) ? phoneCount : ++phoneCount);

            //        resultDataGridView.Rows[index].Cells[0].Value = (index + 1).ToString();
            //        resultDataGridView.Rows[index].Cells[1].Value = postDate.Category;
            //        resultDataGridView.Rows[index].Cells[2].Value = postDate.City;
            //        resultDataGridView.Rows[index].Cells[3].Value = postDate.Phone;
            //        resultDataGridView.Rows[index].Cells[4].Value = postDate.Email;
            //        resultDataGridView.Rows[index].Cells[5].Value = postDate.Title;
            //        resultDataGridView.Rows[index].Cells[6].Value = postDate.Body;
            //        resultDataGridView.Rows[index].Cells[7].Value = postDate.Url;
            //        resultDataGridView.Rows[index].Cells[8].Value = postDate.Time;
            //        index++;
            //    }

            //    searchButton.Enabled = searchButton.Enabled = clearButton.Enabled = exportButton.Enabled = true;
            //    statusLabel.Text = @"Finished";
            //    _stopped = false;
            //}
            //catch (Exception exception)
            //{
            //    MessageBox.Show(exception + "");
            //}
        }

        private void ExportData(string path)
        {
            try
            {
                using (var sw = new StreamWriter(path))
                {
                    sw.WriteLine("#,Category,City,Phone,Email,Title,Post Data,URL,Post Time");
                    var rowIndex = 1;
                    foreach (DataGridViewRow row in resultDataGridView.Rows)
                    {
                        if (row.Cells[0].Value == null || string.IsNullOrEmpty(row.Cells[0].Value.ToString()))
                            continue;
                        var eachLine = string.Empty;
                        foreach (DataGridViewCell cell in row.Cells)
                        {
                            var val = cell.Value == null ? string.Empty : cell.Value.ToString().Replace(",", ";").Replace("\"", "");
                            //if (val.Contains(","))
                            val = "\"" + val + "\"";
                            eachLine += val + ",";
                        }
                        eachLine = eachLine.Substring(0, eachLine.Length - 1);
                        sw.WriteLine(eachLine);
                        rowIndex++;
                    }
                    sw.Flush();
                    sw.Close();
                }
                MessageBox.Show(@"Data Exported successfully");
            }
            catch (Exception exception)
            {
                MessageBox.Show(@"Error while exporting data.\nOriginal Error:\n" + exception.Message);
            }
        }

        private void OpenFileSaveDialogue()
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = @"CSV Files (*.csv)|*.csv|All files (*.*)|*.*",
                FilterIndex = 1,
                RestoreDirectory = true
            };

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                ExportData(saveFileDialog.FileName);
            }
        }

        #endregion

        #region [Events]

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Dispose(true);
        }

        private void startButton_Click(object sender, EventArgs e)
        {
            searchButton.Enabled = searchButton.Enabled = clearButton.Enabled = exportButton.Enabled = true;
            statusLabel.Text = @"Searching..";
            var thread = new Thread(new ThreadStart(Search));
            thread.Start();
        }

        private void searchButton_Click(object sender, EventArgs e)
        {
            statusLabel.Text = @"Please wait. Searching locations..";
            var thread = new Thread(new ThreadStart(SearchLocations));
            thread.Start();
        }

        private void stopButton_Click(object sender, EventArgs e)
        {
            _stopped = true;
            statusLabel.Text = @"Stopping. Please wait..";
        }

        private void clearButton_Click(object sender, EventArgs e)
        {
            ClearGrid();
        }

        private void exportButton_Click(object sender, EventArgs e)
        {
            OpenFileSaveDialogue();
        }

        private void resultDataGridView_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == 7 && resultDataGridView.Rows[e.RowIndex].Cells[e.ColumnIndex].Value != null &&
                !string.IsNullOrEmpty(resultDataGridView.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString()))
            {
                Process.Start(resultDataGridView.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString());
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            resultDataGridView.FirstDisplayedScrollingRowIndex = 0;
        }

        #endregion

        private void button1_Click_1(object sender, EventArgs e)
        {
            NewSearch();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            number = 9;
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            number = 49;
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            number = 99;
        }

        private void checkBox4_CheckedChanged(object sender, EventArgs e)
        {
            number = 1;
        }

        private void button1_Click_2(object sender, EventArgs e)
        {
            _stopped = true;
            statusLabel.Text = @"Stopping. Please wait..";
        }
    }
}
