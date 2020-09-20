using HtmlAgilityPack;
using PreisVergleich.Helper;
using PreisVergleich.Helpers;
using PreisVergleich.Models;
using PreisVergleich.Views;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
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

        public ICollectionView productGrid { get; set; }

        public string statusValue { get; set; }
        public string rowsLoaded { get; set; }

        #region Sorting bools

        public bool? SortedByDate { get; set; }
        public bool? SortedByGZ { get; set; }
        public bool? SortedByPrice { get; set; }
        public bool? SortedByState { get; set; }

        #endregion

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
                LoadGridItems(false, false);
                log.writeLog(LogType.INFO, "Programmstart");
                statusValue = "Start erfolgreich!";
            }
        }

        #region Grouping

        public ICommand StateGrouping
        {
            get { return new DelegateCommand<object>(StateGroupingCommand); }
        }

        public void StateGroupingCommand(object action)
        {
            CollectionViewSource productDialog = new CollectionViewSource() { Source = produktItems };
            productDialog.GroupDescriptions.Clear();
            productDialog.GroupDescriptions.Add(new PropertyGroupDescription("State"));
            productGrid = productDialog.View;
        }

        public ICommand HasGZUrlGrouping
        {
            get { return new DelegateCommand<object>(HasGZUrlGroupingCommand); }
        }

        public void HasGZUrlGroupingCommand(object action)
        {
            CollectionViewSource productDialog = new CollectionViewSource() { Source = produktItems };
            productDialog.GroupDescriptions.Clear();
            productDialog.GroupDescriptions.Add(new PropertyGroupDescription("hasGeizhalsURL"));
            productGrid = productDialog.View;
        }

        public ICommand DeleteGrouping
        {
            get { return new DelegateCommand<object>(DeleteGroupingCommand); }
        }

        public void DeleteGroupingCommand(object action)
        {
            CollectionViewSource productDialog = new CollectionViewSource() { Source = produktItems };
            productDialog.GroupDescriptions.Clear();
            productGrid = productDialog.View;
        }

        #endregion

        #region Sorting 

        public ICommand SortByState
        {
            get { return new DelegateCommand(SortByStateCommand); }
        }

        public void SortByStateCommand()
        {
            if (SortedByState == true)
            {
                SortedByState = false;
                produktItems = new ObservableCollection<ProduktModell>(produktItems.OrderByDescending(y => y.State));
                CollectionViewSource productCView = new CollectionViewSource() { Source = produktItems };
                productGrid = productCView.View;
            }
            else
            {
                SortedByState = true;
                produktItems = new ObservableCollection<ProduktModell>(produktItems.OrderBy(y => y.State));
                CollectionViewSource productCView = new CollectionViewSource() { Source = produktItems };
                productGrid = productCView.View;
            }
        }

        public ICommand SortByDifference
        {
            get { return new DelegateCommand(SortByDifferenceCommand); }
        }

        public void SortByDifferenceCommand()
        {
            if (SortedByPrice == true)
            {
                SortedByPrice = false;
                produktItems = new ObservableCollection<ProduktModell>(produktItems.OrderByDescending(y => y.priceDifference));
                CollectionViewSource productCView = new CollectionViewSource() { Source = produktItems };
                productGrid = productCView.View;
            }
            else
            {
                SortedByPrice = true;
                produktItems = new ObservableCollection<ProduktModell>(produktItems.OrderBy(y => y.priceDifference));
                CollectionViewSource productCView = new CollectionViewSource() { Source = produktItems };
                productGrid = productCView.View;
            }
        }

        public ICommand SortByDate
        {
            get { return new DelegateCommand(SortByDateCommand); }
        }

        public void SortByDateCommand()
        {
            if (SortedByDate == true)
            {
                SortedByDate = false;
                produktItems = new ObservableCollection<ProduktModell>(produktItems.OrderByDescending(y => y.AddedAt));
                CollectionViewSource productCView = new CollectionViewSource() { Source = produktItems };
                productGrid = productCView.View;
            }
            else
            {
                SortedByDate = true;
                produktItems = new ObservableCollection<ProduktModell>(produktItems.OrderBy(y => y.AddedAt));
                CollectionViewSource productCView = new CollectionViewSource() { Source = produktItems };
                productGrid = productCView.View;
            }
        }

        public ICommand DeleteSorting
        {
            get { return new DelegateCommand(DeleteSortingCommand); }
        }

        public void DeleteSortingCommand()
        {
            SortedByGZ = null;
            SortedByPrice = null;
            SortedByState = null;
            SortedByDate = null;

            produktItems = new ObservableCollection<ProduktModell>(produktItems.OrderBy(y => y.hardwareRatID));
            CollectionViewSource productCView = new CollectionViewSource() { Source = produktItems };
            productGrid = productCView.View;
        }

        public ICommand SortByGZURL
        {
            get { return new DelegateCommand(SortByGZURLCommand); }
        }

        public void SortByGZURLCommand()
        {
            if (SortedByGZ == true)
            {
                produktItems = new ObservableCollection<ProduktModell>(produktItems.OrderByDescending(y => y.hasGeizhalsURL));
                CollectionViewSource productCView = new CollectionViewSource() { Source = produktItems };
                productGrid = productCView.View;
                SortedByGZ = false;
            }
            else
            {
                produktItems = new ObservableCollection<ProduktModell>(produktItems.OrderBy(y => y.hasGeizhalsURL));
                CollectionViewSource productCView = new CollectionViewSource() { Source = produktItems };
                productGrid = productCView.View;
                SortedByGZ = true;
            }
        }

        #endregion

        public async void LoadGridItems(bool loadSiteData, bool onlyUpdateEmpty)
        {
            try
            {
                List<ProduktModell> tmpProduktItems = new List<ProduktModell>();
                bool waitTask = false;

                //Sqls laden
                string sSQL = "SELECT hardwareRatURL, compareSiteURL, hardwareRatPrice, compareSitePrice, state, differencePrice, compareSiteType, produktID, articleName, articleURL, hardwareRatID, addedAt, hasGeizhalsURL, IsNew, GTIN FROM PRODUKTE";

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
                            statusValue = $"Lade Artikel {countFor} von {retValProducts.Count} ({Math.Round(((double)countFor / retValProducts.Count * 100), 0)}%)";
                        }));

                        if (loadSiteData)
                        {
                            //Daten abrufen
                            ProduktModell retVal = getHTMLData(row, onlyUpdateEmpty);
                            row.comparePrice = retVal.comparePrice;
                            row.hardwareRatPrice = retVal.hardwareRatPrice;
                            row.articleName = retVal.articleName;

                            if (waitTask && !row.hasGeizhalsURL && onlyUpdateEmpty)
                            {
                                System.Threading.Thread.Sleep(2500);
                            }
                            else if (waitTask && !onlyUpdateEmpty)
                            {
                                System.Threading.Thread.Sleep(2500);
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

                    rowsLoaded = $"{retValProducts.Count} Produkte geladen [{retValProducts.Count(y => y.State == "günstiger")} günstiger; {retValProducts.Count(y => y.State == "1-2€ darüber")} 1-2€ darüber; {retValProducts.Count(y => y.State == "3€ oder mehr darüber")} 3€ oder mehr]!";

                    await MainDispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                    {
                        statusValue = "Übersicht erfolgreich aktualisiert!";
                        produktItems = new ObservableCollection<ProduktModell>(tmpProduktItems);
                        CollectionViewSource productCView = new CollectionViewSource() { Source = tmpProduktItems };
                        productGrid = productCView.View;
                    }));
                }
                else
                {
                    produktItems = new ObservableCollection<ProduktModell>();
                    await MainDispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                    {
                        statusValue = "Keine Daten vorhanden!";
                        productGrid = null;
                    }));
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

        public ProduktModell getHTMLData(ProduktModell item, bool onlyUpdateEmpty)
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

                if (onlyUpdateEmpty && item.hasGeizhalsURL == false)
                {
                    //Geizhals
                    try
                    {
                        if (!item.hasGeizhalsURL)
                        {
                            //URL laden
                            ProduktModell retVal = SearchGeizhalsData(item.gTIN, item.articleName);
                            item.compareURL = retVal.compareURL;
                            item.comparePrice = retVal.comparePrice;
                            item.hasGeizhalsURL = retVal.hasGeizhalsURL;
                        }

                        document = new HtmlAgilityPack.HtmlDocument();
                        document = webPage.Load(item.compareURL);
                        retValPrice = document.DocumentNode.SelectSingleNode("//span[@class='variant__header__pricehistory__pricerange']//strong//span[@class='gh_price']").InnerText.Replace("€ ", "").Replace("&euro; ", "");
                        item.comparePrice = Math.Round(double.Parse(retValPrice), 2);
                        item.hasGeizhalsURL = true;
                    }
                    catch (Exception)
                    {
                        item.comparePrice = 0;
                        item.hasGeizhalsURL = false;
                    }
                }

                if (!onlyUpdateEmpty)
                {
                    try
                    {
                        document = new HtmlAgilityPack.HtmlDocument();
                        document = webPage.Load(item.compareURL);
                        retValPrice = document.DocumentNode.SelectSingleNode("//span[@class='variant__header__pricehistory__pricerange']//strong//span[@class='gh_price']").InnerText.Replace("€ ", "").Replace("&euro; ", "");
                        item.comparePrice = Math.Round(double.Parse(retValPrice), 2);
                        item.hasGeizhalsURL = true;
                    }
                    catch (Exception)
                    {
                        item.comparePrice = 0;
                        item.hasGeizhalsURL = false;
                    }
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
                        Task.Run(() => LoadGridItems(true, false));
                    }
                    else if (dialogResult == DialogResult.No)
                    {
                        Task.Run(() => LoadGridItems(false, false));
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
            Task.Run(() => LoadGridItems(true, false));
        }

        public ICommand UpdateGridEmpty
        {
            get { return new DelegateCommand<object>(UpdateGridEmptyCommand); }
        }

        private void UpdateGridEmptyCommand(object context)
        {
            Task.Run(() => LoadGridItems(true, true));
        }

        public ICommand UpdateGridOnly
        {
            get { return new DelegateCommand<object>(UpdateGridOnlyCommand); }
        }

        private void UpdateGridOnlyCommand(object context)
        {
            Task.Run(() => LoadGridItems(false, false));
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
            LoadGridItems(false, false);
        }

        public ICommand DeleteDB
        {
            get { return new DelegateCommand<object>(DeleteDBCommand); }
        }

        private void DeleteDBCommand(object context)
        {
            if (System.Windows.MessageBox.Show("Möchten sie wirklich die Datenbank löschen?", "Sicherheitsfrage", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
            {
                return;
            }

            sQLiteHelper.DeleteDB();
            LoadGridItems(false, false);
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

        private void ImportXMLCommand(object context)
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

                Stopwatch xmlTime = new Stopwatch();

                xmlTime.Start();

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
                        gTIN = artikelDaten[i].ChildNodes[11].InnerText,
                        AddedAt = DateTime.Now,
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
                                ProduktModell retVal = SearchGeizhalsData(row.gTIN, row.articleName);
                                row.comparePrice = retVal.comparePrice;
                                row.compareURL = retVal.compareURL;
                                row.hasGeizhalsURL = retVal.hasGeizhalsURL;

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

                                //1.2 Sekunden warten IP Ban zu umgehen
                                System.Threading.Thread.Sleep(1200);
                            }
                            catch (Exception)
                            {
                            }
                        }

                        if (!produktItems.Any(y => y.hardwareRatID == row.hardwareRatID))
                        {
                            row.IsNew = true;
                            row.AddedAt = DateTime.Now;
                            sQLiteHelper.InsertItem(row);
                            articleAdded++;

                            await MainDispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                            {
                                statusValue = $"Übernehme Artikel {articleAdded} von {articleMax} ({Math.Round(((double)articleAdded / articleMax * 100), 0)}%)";
                            }));
                        }
                        else
                        {
                            //Artikel nur updaten
                            row.IsNew = false;
                            sQLiteHelper.UpdateItemXML(row);

                            await MainDispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                            {
                                statusValue = $"Update Artikel HardwareRat ID {row.hardwareRatID}, da er bereits vorhanden ist!";
                            }));
                        }

                    }
                }
                xmlTime.Stop();
                log.writeLog(LogType.ERROR, $"XML Abruf hat {xmlTime.Elapsed.ToString("hh")}h {xmlTime.Elapsed.ToString("mm")}m {xmlTime.Elapsed.ToString("ss")}s {xmlTime.Elapsed.ToString("ff")}ms gedauert.");

                await MainDispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                {
                    statusValue = $"{articleAdded} / {maxCount} Artikel übernommen ({Math.Round(((double)articleAdded / articleMax * 100), 0)}%)";
                }));
                LoadGridItems(false, false);
            }
            catch (Exception)
            {

            }
        }


        public ProduktModell SearchGeizhalsData(string gTIN, string productName)
        {
            ProduktModell retVal = new ProduktModell();

            try
            {
                HtmlWeb webPage = new HtmlWeb();
                HtmlAgilityPack.HtmlDocument document = new HtmlAgilityPack.HtmlDocument();

                //Name parsen, damit er akzeptiert wird
                string searchProduct = string.IsNullOrEmpty(gTIN) ? "" : gTIN.Replace(" ", "+").Replace(",", "%2C").Replace("/EU", "");

                document = webPage.Load($"https://geizhals.eu/?fs={searchProduct}&hloc=at&in=");

                if (webPage.ResponseUri.ToString().Contains("?fs=") || webPage.ResponseUri.ToString().Contains("geizhals"))
                {
                    try
                    {
                        //GeizhalsURL öffnen
                        try
                        {
                            string foundURL = document.DocumentNode.SelectSingleNode("//a[@class='listview__name-link']").Attributes["href"].Value;
                            retVal.comparePrice = double.Parse(document.DocumentNode.SelectSingleNode("//a[@class='listview__price-link ']//span[@class='price']").InnerText.Replace("€ ", "").Replace("&euro; ", ""));
                            retVal.compareURL = foundURL.Contains("geizhals.eu") ? "https:" + foundURL : "https://geizhals.eu/" + foundURL;

                            //document = webPage.Load(retVal.compareURL);
                            //retVal.comparePrice = double.Parse(document.DocumentNode.SelectSingleNode("//span[@class='variant__header__pricehistory__pricerange']//strong//span[@class='gh_price']").InnerText.Replace("€ ", "").Replace("&euro; ", ""));
                            retVal.hasGeizhalsURL = true;
                        }
                        catch (Exception ex)
                        {
                            log.writeLog(LogType.ERROR, $"{MethodBase.GetCurrentMethod().Name}: Fehler beim Laden mit GTIN {gTIN}", ex);

                            //Suche per Name probieren
                            searchProduct = productName.Replace(" ", "+").Replace(",", "%2C").Replace("/EU", "");

                            document = webPage.Load($"https://geizhals.eu/?fs={searchProduct}&hloc=at&in=&sort=p");
                            
                            string foundURL = document.DocumentNode.SelectSingleNode("//a[@class='listview__name-link']").Attributes["href"].Value;
                            retVal.comparePrice = double.Parse(document.DocumentNode.SelectSingleNode("//a[@class='listview__price-link ']//span[@class='price']").InnerText.Replace("€ ", "").Replace("&euro; ", ""));

                            retVal.compareURL = foundURL.Contains("geizhals.eu") ? "https:" + foundURL : "https://geizhals.eu/" + foundURL;

                            //document = webPage.Load(retVal.compareURL);
                            //retVal.comparePrice = double.Parse(document.DocumentNode.SelectSingleNode("//span[@class='variant__header__pricehistory__pricerange']//strong//span[@class='gh_price']").InnerText.Replace("€ ", "").Replace("&euro; ", ""));
                            retVal.hasGeizhalsURL = true;
                        }

                    }
                    catch (Exception ex)
                    {
                        log.writeLog(LogType.ERROR, $"{MethodBase.GetCurrentMethod().Name}: Kein Artikel gefunden zu {gTIN} und {productName}", ex);
                        retVal.compareURL = "Artikel bei Geizhals nicht gefunden";
                        retVal.hasGeizhalsURL = false;
                        retVal.comparePrice = 0;
                    }
                }
                else
                {
                    retVal.compareURL = webPage.ResponseUri.ToString();
                    retVal.comparePrice = double.Parse(document.DocumentNode.SelectSingleNode("//span[@class='variant__header__pricehistory__pricerange']//strong//span[@class='gh_price']").InnerText.Replace("€ ", "").Replace("&euro; ", ""));
                    retVal.hasGeizhalsURL = true;
                }

            }
            catch (Exception)
            {
                retVal.comparePrice = 0;
            }

            return retVal;
        }

        #endregion
    }
}
