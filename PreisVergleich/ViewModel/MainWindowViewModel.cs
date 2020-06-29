using HtmlAgilityPack;
using PreisVergleich.Helper;
using PreisVergleich.Helpers;
using PreisVergleich.Models;
using PreisVergleich.Views;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using static PreisVergleich.Helpers.Logger;

namespace PreisVergleich.ViewModel
{
    [PropertyChanged.AddINotifyPropertyChangedInterface]
    public class MainWindowViewModel
    {
        Logger log;
        SQLiteHelper sQLiteHelper;

        //private static HttpClient httpClient = new HttpClient();

        public ObservableCollection<ProduktModell> produktItems { get; set; }

        public Dispatcher MainDispatcher { get; set; }

        public ProduktModell selectedItem { get; set; }

        public string statusValue { get; set; }

        //Konstruktor
        public MainWindowViewModel()
        {
            //Logger init
            log = new Logger();
            MainDispatcher = Dispatcher.CurrentDispatcher;
            //SQL Helper initialisieren
            string connectionString = Properties.Settings.Default.DatebaseLocation.Replace("{PROJECT}", AppDomain.CurrentDomain.BaseDirectory); 

            sQLiteHelper = new SQLiteHelper(connectionString);

            if (sQLiteHelper != null)
            {
                produktItems = new ObservableCollection<ProduktModell>();
                LoadGridItems(false);
                log.writeLog(LogType.INFO, "Programmstart");
                statusValue = "Start erfolgreich!";
            }
        }

        public async void LoadGridItems(bool loadSiteData)
        {
            try
            {
                List<ProduktModell> tmpProduktItems = new List<ProduktModell>();

                //Sqls laden
                string sSQL = "SELECT hardwareRatURL, compareSiteURL, hardwareRatPrice, compareSitePrice, state, differencePrice, compareSiteType, produktID, articleName, articleURL FROM PRODUKTE";

                List<ProduktModell> retValProducts = sQLiteHelper.getGridData(sSQL);
                if (retValProducts != null)
                {
                    int countFor = 1;
                    foreach(ProduktModell row in retValProducts)
                    {
                        await MainDispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                        {
                            statusValue = $"Lade Artikel {countFor} von {retValProducts.Count}";
                        }));

                        if (loadSiteData)
                        {
                            //Daten abrufen
                            ProduktModell retVal = getHTMLData(row);
                            row.comparePrice = retVal.comparePrice;
                            row.hardwareRatPrice = retVal.hardwareRatPrice;
                            row.articleName = retVal.articleName;
                        }

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

                        tmpProduktItems.Add(row);
                        countFor++;
                    }

                    await MainDispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                    {
                        statusValue = "Übersicht erfolgreich aktualisiert!";
                        produktItems = new ObservableCollection<ProduktModell>(tmpProduktItems);
                    }));
                }
                else
                {
                    return;
                }
                return;
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
            string retValPicture = "https://hardwarerat.de/media/image/85/fa/30/logo-klein.png";

            try
            {
                HtmlWeb webPage = new HtmlWeb();
                HtmlAgilityPack.HtmlDocument document = new HtmlAgilityPack.HtmlDocument();
                document = webPage.Load(item.hardwareRatURL);

                retValPrice = document.DocumentNode.SelectSingleNode("//span[@class='price--content content--default']/meta").Attributes["content"].Value.Replace(".", ",");
                retValName = document.DocumentNode.SelectSingleNode("//h1[@class='product--title']").InnerText.Replace("\n", "");
                retValPicture = document.DocumentNode.SelectSingleNode("//span[@class='image--media']/img").Attributes["src"].Value;

                item.hardwareRatPrice = Math.Round(double.Parse(retValPrice), 2);
                item.articleName = retValName;
                item.articlePicture = retValPicture;

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
            bool? result = addValue.ShowDialog();
            if(result != null)
            {
                if(result == true)
                {
                    Task.Run(() => LoadGridItems(true));
                }
            }
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
            Task.Run(() => LoadGridItems(true));
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
            LoadGridItems(false);
        }

        public ICommand OpenHWLink
        {
            get { return new DelegateCommand<object>(OpenHWLinkCommand); }
        }

        private void OpenHWLinkCommand(object context)
        {
            if (selectedItem == null)
            {
                return;
            }
            Process.Start(selectedItem.hardwareRatURL);
        }

        public ICommand OpenCompareLink
        {
            get { return new DelegateCommand<object>(OpenCompareLinkCommand); }
        }

        private void OpenCompareLinkCommand(object context)
        {
            if (selectedItem == null)
            {
                return;
            }
            Process.Start(selectedItem.compareURL);
        }


        #endregion
    }
}
