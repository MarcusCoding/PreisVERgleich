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
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Threading;
using System.Xml;
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
                bool waitTask = false;

                //Sqls laden
                string sSQL = "SELECT hardwareRatURL, compareSiteURL, hardwareRatPrice, compareSitePrice, state, differencePrice, compareSiteType, produktID, articleName, articleURL, hardwareRatID FROM PRODUKTE";

                List<ProduktModell> retValProducts = sQLiteHelper.getGridData(sSQL);
                if (retValProducts != null)
                {
                    int countFor = 1;

                    if (loadSiteData)
                    {
                       
                        DialogResult dialogResult = System.Windows.Forms.MessageBox.Show("Möchten sie mit Zeitversatz arbeiten, um Geizhals Ban zu umgehen?", "Hinweis!", MessageBoxButtons.YesNo);
                        if (dialogResult == DialogResult.Yes)
                        {
                            waitTask = true;
                        }
                    }
                   
                    foreach (ProduktModell row in retValProducts)
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

                            if (waitTask)
                            {
                                System.Threading.Thread.Sleep(4000);
                            }
                        }

                        //Status abrufen
                        double difference = Math.Round(row.hardwareRatPrice - row.comparePrice, 2);
                        row.priceDifference = difference;
                        if (difference <= 0)
                        {
                            row.State = "günstiger";
                        }
                        else if (difference > 0 && difference < 3)
                        {
                            row.State = "1-2€ darüber";
                        }
                        else if (difference > 2)
                        {
                            row.State = "3€ oder mehr darüber";
                        }

                        //Falls kein Preis bei Geizhals vorhanden
                        if (row.comparePrice == 0)
                        {
                            row.State = "günstiger";
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
                    produktItems = new ObservableCollection<ProduktModell>();
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

                //Abfangen wenn es den Artikel nicht mehr gibt / deaktivert
                try
                {
                    string errorString = document.DocumentNode.SelectSingleNode("//div[@class='content--wrapper']/div[@class='detail-error content listing--content']/h1[@class='detail-error--headline']").InnerText;

                    item.articleName = "Artikel nicht mehr verfügbar!";
                    item.articlePicture = "https://hardwarerat.de/media/image/85/fa/30/logo-klein.png";
                    item.hardwareRatPrice = 0;
                }
                catch (Exception)
                {
                }

                retValPrice = document.DocumentNode.SelectSingleNode("//span[@class='price--content content--default']/meta").Attributes["content"].Value.Replace(".", ",");
                retValName = document.DocumentNode.SelectSingleNode("//h1[@class='product--title']").InnerText.Replace("\n", "");
                retValPicture = document.DocumentNode.SelectSingleNode("//span[@class='image--media']/img").Attributes["src"].Value;

                item.hardwareRatPrice = Math.Round(double.Parse(retValPrice), 2);
                item.articleName = retValName;
                item.articlePicture = retValPicture;

                //Geizhals
                try
                {
                    document = new HtmlAgilityPack.HtmlDocument();
                    document = webPage.Load(item.compareURL);
                    retValPrice = document.DocumentNode.SelectSingleNode("//span[@class='variant__header__pricehistory__pricerange']//strong//span[@class='gh_price']").InnerText.Replace("€ ", "").Replace("&euro; ", "");
                    item.comparePrice = Math.Round(double.Parse(retValPrice), 2);
                }
                catch(Exception)
                {
                    item.comparePrice = 0;
                }

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
            if (result != null)
            {
                if (result == true)
                {
                    DialogResult dialogResult = System.Windows.Forms.MessageBox.Show("Möchten sie alle Artikeldaten neuladen?", "Hinweis!", MessageBoxButtons.YesNo);
                    if (dialogResult == DialogResult.Yes)
                    {
                        Task.Run(() => LoadGridItems(true));
                    }
                    else if (dialogResult == DialogResult.No)
                    {
                        Task.Run(() => LoadGridItems(false));
                    }
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

        public ICommand UpdateGridOnly
        {
            get { return new DelegateCommand<object>(UpdateGridOnlyCommand); }
        }

        private void UpdateGridOnlyCommand(object context)
        {
            Task.Run(() => LoadGridItems(false));
        }

        public ICommand DeleteItem
        {
            get { return new DelegateCommand<object>(DeleteItemCommand); }
        }

        private void DeleteItemCommand(object context)
        {
            if (selectedItem == null)
            {
                return;
            }
            sQLiteHelper.DeleteItem(selectedItem);
            LoadGridItems(false);
        }

        public ICommand DeleteDB
        {
            get { return new DelegateCommand<object>(DeleteDBCommand); }
        }

        private void DeleteDBCommand(object context)
        {
            sQLiteHelper.DeleteDB();
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

        public ICommand ImportXML
        {
            get { return new DelegateCommand<object>(ImportXMLCommand); }
        }

        private  void ImportXMLCommand(object context)
        {
            try
            {
                bool useGeizhals = false;

                DialogResult dialogResult = System.Windows.Forms.MessageBox.Show("Möchten sie den Geizhalsbezug mitladen? (Dies nimmt einige Zeit mehr in Anspruch)", "Hinweis!", MessageBoxButtons.YesNo);
                if (dialogResult == DialogResult.Yes)
                {
                    useGeizhals = true;
                }

                string xmlStr;
                using (var wc = new WebClient())
                {
                    xmlStr = wc.DownloadString(Properties.Settings.Default.URL);
                }
                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(xmlStr);

                 Task.Run(() => LoadXMLintoSQLite(xmlDoc, useGeizhals));

              
            }
            catch (Exception ex)
            {
                log.writeLog(LogType.ERROR, MethodBase.GetCurrentMethod().Name + $": Fehler beim Laden vom XML", ex);
            }
        }

        public async void LoadXMLintoSQLite(XmlDocument xmlDoc, bool loadGeizhals)
        {
            try
            {
                List<ProduktModell> listXML = new List<ProduktModell>();

                //XML auswerten und antragen
                XmlNodeList artikelDaten = xmlDoc.SelectNodes(".//item");

                int maxCount = artikelDaten.Count - 1;

                for (int i = 0; i < artikelDaten.Count; i++)
                {
                    ProduktModell model = new ProduktModell()
                    {
                        hardwareRatURL = artikelDaten[i].ChildNodes[5].InnerText,
                        hardwareRatPrice = double.Parse(artikelDaten[i].ChildNodes[9].InnerText),
                        articlePicture = artikelDaten[i].ChildNodes[6].InnerText,
                        articleName = artikelDaten[i].ChildNodes[1].InnerText,
                        hardwareRatID = int.Parse(artikelDaten[i].ChildNodes[0].InnerText),
                    };

                    listXML.Add(model);

                    await MainDispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                    {
                        statusValue = $"Lade XML Artikel {i} von {maxCount}";
                    }));
                }

                int articleAdded = 0;
                int articleMax = 0;
                //In Datenbank packen
                if (listXML.Count > 0)
                {
                    articleMax = listXML.Count;
                    HtmlAgilityPack.HtmlDocument document = null;
                    HtmlWeb webPage = null;

                    if (loadGeizhals)
                    {
                        webPage = new HtmlWeb();
                        document = new HtmlAgilityPack.HtmlDocument();
                    }

                    foreach (ProduktModell row in listXML)
                    {
                        //Geizhalsbezug aufrufen
                        if (loadGeizhals)
                        {
                            try
                            {
                                try
                                {
                                    document = new HtmlAgilityPack.HtmlDocument();

                                    //Name parsen, damit er akzeptiert wird
                                    string searchProduct = row.articleName.Replace(" ", "+").Replace(",", "%2C");

                                    document = webPage.Load($"https://geizhals.de/?fs={searchProduct}&hloc=at&in=");

                                    //GeizhalsURL öffnen
                                    row.compareURL = "https://geizhals.de/" + document.DocumentNode.SelectSingleNode("//a[@class='listview__name-link']").Attributes["href"].Value;

                                    document = webPage.Load(row.compareURL);

                                    row.comparePrice = double.Parse(document.DocumentNode.SelectSingleNode("//span[@class='variant__header__pricehistory__pricerange']//strong//span[@class='gh_price']").InnerText.Replace("€ ", "").Replace("&euro; ", ""));

                                }
                                catch (Exception) 
                                {
                                    row.comparePrice = 0;
                                }

                                double difference = Math.Round(row.hardwareRatPrice - row.comparePrice, 2);
                                row.priceDifference = difference;
                                if (difference <= 0)
                                {
                                    row.State = "günstiger";
                                }
                                else if (difference > 0 && difference < 3)
                                {
                                    row.State = "1-2€ darüber";
                                }
                                else if (difference > 2)
                                {
                                    row.State = "3€ oder mehr darüber";
                                }

                                //Falls kein Preis bei Geizhals vorhanden
                                if(row.comparePrice == 0)
                                {
                                    row.State = "günstiger";
                                }

                                //4 Sekunden warten GitHub Ban zu umgehen
                                System.Threading.Thread.Sleep(4000);
                            }
                            catch (Exception)
                            {

                            }
                        }

                        if(!produktItems.Any(y => y.hardwareRatID == row.hardwareRatID))
                        {
                            row.articleName += " (Neu)";
                            sQLiteHelper.InsertItem(row);
                            articleAdded++;

                            await MainDispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                            {
                                statusValue = $"Übernehme Artikel {articleAdded} von {articleMax}";
                            }));
                        }
                        else
                        {
                            await MainDispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                            {
                                statusValue = $"Überspringe Artikel {row.hardwareRatID} da bereits vorhanden!";
                            }));
                        }

                    }
                }
                await MainDispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                {
                    statusValue = $"{articleAdded} / {maxCount} Artikel übernommen";
                }));
                LoadGridItems(false);
            }
            catch (Exception)
            {

            }


        }

        #endregion
    }
}
