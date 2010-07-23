﻿using System;
using System.Collections.Generic;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Globalization;
using System.Threading;
using System.Resources;

namespace Margent
{
    public partial class _Default : System.Web.UI.Page
    {
        private static string _connectionError = "An error occured while trying to fetch data from the DB.";
        private static string _emptyDataText = "There were not found words, associated to that query. We apologize for the inconvenience.";
        private static string _tooShortQuery = "Please enter a word, longer that two letters.";
        private static Dictionary<DALWebCrawlerActive.Word, DataFetcher.CountFileList> _resultsList = null;
        private ResourceManager rm;

        protected void Page_Load(object sender, EventArgs e)
        {
            SetLanguage();

            lnkLangBulgarian.Click += new ImageClickEventHandler(lnkLangBulgarian_Click);
            lnkLangEnglish.Click += new ImageClickEventHandler(lnkLangEnglish_Click);
            lnkLangGerman.Click += new ImageClickEventHandler(lnkLangGerman_Click);

            gvKeywords.RowDataBound += new GridViewRowEventHandler(gvKeywords_RowDataBound);
            gvKeywords.PageIndexChanging += new GridViewPageEventHandler(gvKeywords_PageIndexChanging);
            gvKeywords.RowCommand += new GridViewCommandEventHandler(gvKeywords_RowCommand);
            gvKeywords.EmptyDataText = _emptyDataText;

            btnDoSearch.Click += new ImageClickEventHandler(btnDoSearch_Click);

            lblSearch.Focus();

            lblError.Visible = false;
            lblError.Text = _connectionError;
        }

        #region Multilanguage support

        private void SetLanguage()
        {
            //Set language
            if (Request.QueryString["Language"] == "DE")
            {
                LoadData("de-DE");
            }
            else if (Request.QueryString["Language"] == "BG")
            {
                LoadData("bg");
            }
            else
            {
                LoadData("en");
            }
        }

        protected void lnkLangEnglish_Click(object sender, ImageClickEventArgs e)
        {
            LoadData("en");
        }
        protected void lnkLangGerman_Click(object sender, ImageClickEventArgs e)
        {
            LoadData("de-DE");
        }
        protected void lnkLangBulgarian_Click(object sender, ImageClickEventArgs e)
        {
            LoadData("bg");
        }
        public void LoadData(string language)
        {
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(language);
            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture(language);

            //lblName.Text = rm.GetString("EventName", ci);
        }

        #endregion

        protected void btnDoSearch_Click(object sender, ImageClickEventArgs e)
        {
            string query = tbSearchQuery.Text.Trim();

            FetchData(query.ToLower());
        }

        protected void gvKeywords_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                System.Collections.Generic.KeyValuePair<DALWebCrawlerActive.Word, DataFetcher.CountFileList> currentWord = (System.Collections.Generic.KeyValuePair<DALWebCrawlerActive.Word, DataFetcher.CountFileList>)e.Row.DataItem;

                LinkButton lnkKeyword = (LinkButton)e.Row.FindControl("lnkKeyword");
                ImageButton btnToggle = (ImageButton)e.Row.FindControl("btnToggle");
                Label lblTotalCount = (Label)e.Row.FindControl("lblTotalCount");

                lnkKeyword.Text = currentWord.Key.WordName;
                lblTotalCount.Text = "[" + currentWord.Value.Count + "]";

                Literal ltrl = (Literal)e.Row.FindControl("lit1");
                ltrl.Text = ltrl.Text.Replace("trCollapseGrid", "trCollapseGrid" + e.Row.RowIndex);
                string str = "trCollapseGrid" + e.Row.RowIndex;
                lnkKeyword.Attributes["OnClick"] = "return OpenTable('" + str + "','" + btnToggle.ClientID + "');";
                btnToggle.Attributes["OnClick"] = "return OpenTable('" + str + "','" + btnToggle.ClientID + "');";

                GridView gvLinks = (GridView)e.Row.FindControl("gvLinks");
                gvLinks.RowDataBound += new GridViewRowEventHandler(gvLinks_RowDataBound);
                gvLinks.PageIndexChanging += new GridViewPageEventHandler(gvLinks_PageIndexChanging);
                gvLinks.RowCommand += new GridViewCommandEventHandler(gvLinks_RowCommand);
                gvLinks.DataSource = currentWord.Value.FilesList;
                gvLinks.EmptyDataText = _emptyDataText;
                gvLinks.DataBind();
            }
        }

        protected void gvLinks_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                DALWebCrawlerActive.File file = (DALWebCrawlerActive.File)e.Row.DataItem;

                HyperLink lnkWebLink = (HyperLink)e.Row.FindControl("lnkLinkTitle");
                Label lblLinkDescription = (Label)e.Row.FindControl("lblLinkDescription");
                HyperLink lnkLink = (HyperLink)e.Row.FindControl("lnkLink");
                
                lnkWebLink.NavigateUrl = file.URL;
                if (file.Title != "")
                {
                    lnkWebLink.Text = file.Title;
                }
                else
                {
                    lnkWebLink.Text = file.URL;
                }

                if (file.Description != "/")
                {
                    lblLinkDescription.Text = file.Description;
                }

                lnkLink.NavigateUrl = file.URL;
                lnkLink.Text = file.URL;
            }
            else if (e.Row.RowType == DataControlRowType.Pager)
            {
                TextBox txtSlideLinks = (TextBox)e.Row.FindControl("txtSlideLinks");
                txtSlideLinks.Text = (gvLinks.PageIndex + 1).ToString();

                AjaxControlToolkit.SliderExtender ajaxSliderLinks = (AjaxControlToolkit.SliderExtender)e.Row.FindControl("ajaxSliderLinks");
                ajaxSliderLinks.Steps = gvLinks.PageCount;
                ajaxSliderLinks.Maximum = gvLinks.PageCount;

                //ImageButton btnFirstLinks = (ImageButton)e.Row.FindControl("btnFirstLinks");
                //ImageButton btnPreviousLinks = (ImageButton)e.Row.FindControl("btnPreviousLinks");
                //ImageButton btnNextLinks = (ImageButton)e.Row.FindControl("btnNextLinks");
                //ImageButton btnLastLinks = (ImageButton)e.Row.FindControl("btnLastLinks");
                //btnFirstLinks.Attributes["OnClick"] = "return false;";
                //btnPreviousLinks.Attributes["OnClick"] = "return false;";
                //btnNextLinks.Attributes["OnClick"] = "return false;";
                //btnLastLinks.Attributes["OnClick"] = "return false;";
            }
        }

        private void FetchData(string query)
        {
            if (query == "")
            {
                gvKeywords.Visible = false;
                return;
            }

            if (query.Length < 3)
            {
                lblError.Text = _tooShortQuery;
                lblError.Visible = true;
                gvKeywords.Visible = false;
                return;
            }

            _resultsList = DataFetcher.FetchResults(query);

            if (_resultsList != null)
            {
                if (_resultsList.Count > 0)
                {
                    lblSummary.Text = "fetch[" + DataFetcher.FetchTimeInSec.ToString("###0.00") + " sec]; sort[" +
                        DataFetcher.SortTimeInSec.ToString("###0.00") + " sec]; shown links[" + DataFetcher.ShownLinks + "]";
                }
                else
                {
                    lblSummary.Text = "";
                }

                gvKeywords.PageIndex = 0;
                gvKeywords.Visible = true;
                lblError.Visible = false;
                SetDataSource();
            }
            else
            {
                lblError.Visible = true;
                gvKeywords.Visible = false;
            }
        }

        private void SetDataSource()
        {
            gvKeywords.DataSource = _resultsList;
            gvKeywords.DataBind();
        }

        #region gvKeywords paging

        protected void txtSlide_Changed(object sender, EventArgs e)
        {
            TextBox txtSliderExt = (TextBox)gvKeywords.BottomPagerRow.Cells[0].FindControl("txtSlide");

            gvKeywords.PageIndex = Int32.Parse(txtSliderExt.Text) - 1;
            SetDataSource();
        }

        void gvKeywords_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvKeywords.PageIndex = e.NewPageIndex;
        }

        void gvKeywords_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            TextBox txtSliderExt = (TextBox)gvKeywords.BottomPagerRow.Cells[0].FindControl("txtSlide");
            int pageIndex = Int32.Parse(txtSliderExt.Text);

            switch (e.CommandName)
            {
                case "Next":
                    if (gvKeywords.PageCount > pageIndex)
                    {
                        txtSliderExt.Text = (pageIndex + 1).ToString();
                        gvKeywords.PageIndex = pageIndex;
                        SetDataSource();
                    }
                    break;
                case "Previous":
                    if (pageIndex > 1)
                    {
                        txtSliderExt.Text = (--pageIndex).ToString();
                        gvKeywords.PageIndex = pageIndex - 1;
                        SetDataSource();
                    }
                    break;
                case "Last":
                    if (gvKeywords.PageCount > pageIndex)
                    {
                        txtSliderExt.Text = gvKeywords.PageCount.ToString();
                        gvKeywords.PageIndex = gvKeywords.PageCount - 1;
                        SetDataSource();
                    }
                    break;
                case "First":
                default:
                    if (pageIndex > 1)
                    {
                        txtSliderExt.Text = "1";
                        gvKeywords.PageIndex = 0;
                        SetDataSource();
                    }
                    break;
            }
        }

        #endregion

        protected void gvLinks_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            ((GridView)sender).PageIndex = e.NewPageIndex;
        }

        protected void gvLinks_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            GridView gv = ((GridView)sender);
            TextBox txtSliderExt = (TextBox)gv.FindControl("txtSlideLinks");
            int pageIndex = Int32.Parse(txtSliderExt.Text);

            switch (e.CommandName)
            {
                case "Next":
                    if (gvKeywords.PageCount - 1 > pageIndex)
                    {
                        txtSliderExt.Text = (pageIndex + 1).ToString();
                        gvKeywords.PageIndex = pageIndex;
                        SetDataSource();
                    }
                    break;
                case "Previous":
                    if (pageIndex > 1)
                    {
                        txtSliderExt.Text = (--pageIndex).ToString();
                        gvKeywords.PageIndex = pageIndex - 1;
                        SetDataSource();
                    }
                    break;
                case "Last":
                    txtSliderExt.Text = gvKeywords.PageCount.ToString();
                    gvKeywords.PageIndex = gvKeywords.PageCount - 1;
                    SetDataSource();
                    break;
                case "First":
                default:
                    txtSliderExt.Text = "1";
                    gvKeywords.PageIndex = 0;
                    SetDataSource();
                    break;
            }
        }

        protected void txtSlideLinks_Changed(object sender, EventArgs e)
        {
            TextBox txtSliderExt = (TextBox)gvLinks.BottomPagerRow.Cells[0].FindControl("txtSlide");

            gvLinks.PageIndex = Int32.Parse(txtSliderExt.Text) - 1;
            SetDataSource();
        }
    }
}