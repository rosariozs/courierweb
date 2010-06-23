using System;
using System.Xml.Serialization;
using System.Collections.Specialized;

namespace MMarinov.WebCrawler.Library
{
    [Serializable]
    public class WordManipulator
    {
        public struct FileCountPair
        {
            public DALWebCrawler.File File;

            public int Count;

            public FileCountPair(DALWebCrawler.File file, int count)
            {
                File = file;
                Count = count;
            }
        }

        public static bool InsertWordsIntoDB(System.Collections.Generic.ICollection<DALWebCrawler.Word> wordsColl)
        {
            using (DALWebCrawler.WebCrawlerDataContext dataContext = new DALWebCrawler.WebCrawlerDataContext(Preferences.ConnectionString))
            {
                dataContext.Words.InsertAllOnSubmit(wordsColl);
                dataContext.SubmitChanges();

                return true;
            }
        }

        public static bool InsertWordsInFilesIntoDB(System.Collections.Generic.List<DALWebCrawler.WordsInFile> wordInFileColl)
        {
            using (DALWebCrawler.WebCrawlerDataContext dataContext = new DALWebCrawler.WebCrawlerDataContext(Preferences.ConnectionString))
            {
                dataContext.WordsInFiles.InsertAllOnSubmit(wordInFileColl);
                //dataContext.Refresh(System.Data.Linq.RefreshMode.KeepCurrentValues, wordInFileColl);
                dataContext.SubmitChanges();

                return true;
            }
        }
    }
}
