using HtmlAgilityPack;
using PreisVergleich.Helper;
using PreisVergleich.Helpers;
using PreisVergleich.Models;
using PreisVergleich.Views;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Reflection;
using System.Windows.Input;
using static PreisVergleich.Helpers.Logger;

namespace PreisVergleich.ViewModel
{
    public class MainWindowViewModel
    {
        Logger log;
        SQLiteHelper sQLiteHelper;

        //private static HttpClient httpClient = new HttpClient();

        public ObservableCollection<ProduktModell> produktItems { get; set; }

        public ProduktModell selectedItem { get; set; }

        //Konstruktor
        public MainWindowViewModel()
        {
            //Logger init
            log = new Logger();

            //SQL Helper initialisieren
            string connectionString = Properties.Settings.Default.DatebaseLocation.Replace("{PROJECT}", AppDomain.CurrentDomain.BaseDirectory); 

            sQLiteHelper = new SQLiteHelper(connectionString);

            if (sQLiteHelper != null)
            {
                 produktItems = new ObservableCollection<ProduktModell>();
                LoadGridItems();
                log.writeLog(LogType.INFO, "Programmstart");
            }
        }

        public async void LoadGridItems()
        {
            try
            {
                //Sqls laden
                string sSQL = "SELECT hardwareRatURL, compareSiteURL, hardwareRatPrice, compareSitePrice, state, differencePrice, compareSiteType, produktID, articleName FROM PRODUKTE";

                //Sensoren laden
                produktItems.Clear();
                List<ProduktModell> retValProducts = sQLiteHelper.getGridData(sSQL);
                if (retValProducts != null)
                {
                    foreach(ProduktModell row in retValProducts)
                    {
                        //Daten abrufen
                        ProduktModell retVal = getHTMLData(row);
                        row.comparePrice = retVal.comparePrice;
                        row.hardwareRatPrice = retVal.hardwareRatPrice;
                        row.articleName = retVal.articleName;

                        //Status abrufen
                        double difference = Math.Round(row.hardwareRatPrice - row.comparePrice, 2);
                        row.priceDifference = difference;
                        if(difference <= 0)
                        {
                            row.State = "günstiger";
                        }
                        else if(difference > 0 && difference < 3)
                        {
                            row.State = "1-2€ darüber";
                        }
                        else if (difference > 2)
                        {
                            row.State = "3€ oder mehr darüber";
                        }

                        sQLiteHelper.UpdateItem(row);

                        produktItems.Add(row);
                    }
                }
                else
                {
                    return;
                }
                return ;
            }
            catch (Exception ex)
            {
                log.writeLog(LogType.ERROR, MethodBase.GetCurrentMethod().Name + ": Fehler beim Laden der Sensoren", ex);
                return;
            }
        }


        public ProduktModell getHTMLData(ProduktModell item)
        {
            string retValPrice = "0";
            string retValName = "Nicht gefunden!";
            try
            {
                HtmlWeb webPage = new HtmlWeb();
              //  var hardwareRatHTML = httpClient.GetStringAsync(item.hardwareRatURL).Result;
               // var compareSiteHTML = httpClient.GetStringAsync(item.compareURL).Result;

                HtmlAgilityPack.HtmlDocument document = new HtmlAgilityPack.HtmlDocument();
                document = webPage.Load(item.hardwareRatURL);

                retValPrice = document.DocumentNode.SelectSingleNode("//span[@class='price--content content--default']/meta").Attributes["content"].Value.Replace(".", ",");
                retValName = document.DocumentNode.SelectSingleNode("//h1[@class='product--title']").InnerText.Replace("\n", "");

                item.hardwareRatPrice = Math.Round(double.Parse(retValPrice), 2);
                item.articleName = retValName;

                //Geizhals
                document = new HtmlAgilityPack.HtmlDocument();
                document = webPage.Load(item.compareURL);
                retValPrice = document.DocumentNode.SelectSingleNode("//span[@class='variant__header__pricehistory__pricerange']//strong//span[@class='gh_price']").InnerText.Replace("€ ", "").Replace("&euro; ", "");
                item.comparePrice = Math.Round(double.Parse(retValPrice), 2);
            }
            catch (Exception ex)
            {
                log.writeLog(LogType.ERROR, MethodBase.GetCurrentMethod().Name + ": Fehler beim Laden der HTML Seiten ", ex);
                log.writeLog(LogType.ERROR, MethodBase.GetCurrentMethod().Name + $": {item.hardwareRatURL}");
                log.writeLog(LogType.ERROR, MethodBase.GetCurrentMethod().Name + $": {item.compareURL}");
                return item;
            }

            return item;
        }

        #region Commands

        public ICommand AddValue
        {
            get { return new DelegateCommand<object>(AddValueCommand); }
        }

        private void AddValueCommand(object context)
        {
            AddValueView addValue = new AddValueView(OperationMode.CREATE, null);
            addValue.ShowDialog();
        }

        public ICommand UpdateValue
        {
            get { return new DelegateCommand<object>(UpdateValueCommand); }
        }

        private void UpdateValueCommand(object context)
        {
            if (selectedItem == null)
            {
                return;
            }
            AddValueView addValue = new AddValueView(OperationMode.UPDATE, selectedItem);
            addValue.ShowDialog();
        }

        public ICommand UpdateGrid
        {
            get { return new DelegateCommand<object>(UpdateGridCommand); }
        }

        private void UpdateGridCommand(object context)
        {
            LoadGridItems();
        }

        public ICommand DeleteItem
        {
            get { return new DelegateCommand<object>(DeleteItemCommand); }
        }

        private void DeleteItemCommand(object context)
        {
            if(selectedItem == null)
            {
                return;
            }
            sQLiteHelper.DeleteItem(selectedItem);
            LoadGridItems();
        }

        #endregion
    }
}
