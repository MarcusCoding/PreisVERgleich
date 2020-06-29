﻿using PreisVergleich.Helper;
using PreisVergleich.Models;
using Prism.Commands;
using System;
using System.Windows;
using System.Windows.Input;
using PreisVergleich.Views;
using HtmlAgilityPack;

namespace PreisVergleich.ViewModel
{
    [PropertyChanged.AddINotifyPropertyChangedInterface]
    public class AddValueViewModel
    {

        #region Variablen für Maske

        public string urlHWRat { get; set; }

        public string urlCompareSite { get; set; }

        public string hwProductPicture { get; set; }

        public string hwProductName { get; set; }

        public string hwProductPrice { get; set; }

        public string ghzProductPrice { get; set; }

        public string productDifference { get; set; }

        public string currentState { get; set; }

        #endregion

        public SQLiteHelper sqlHelper;

        public ICloseWindow View { get; set; }

        public OperationMode operationMode { get; set; }

        public ProduktModell orginalItem { get; set; }

        public AddValueViewModel(OperationMode mode, ProduktModell selectedItem)
        {
            operationMode = mode;
            if(operationMode == OperationMode.UPDATE)
            {
                urlHWRat = selectedItem.hardwareRatURL;
                urlCompareSite = selectedItem.compareURL;
                orginalItem = selectedItem;
            }
            string connectionString = Properties.Settings.Default.DatebaseLocation.Replace("{PROJECT}", AppDomain.CurrentDomain.BaseDirectory);
            sqlHelper = new SQLiteHelper(connectionString);
        }


        public ICommand buttonFinished
        {
            get { return new DelegateCommand<object>(buttonFinishedCommand); }
        }

        public void buttonFinishedCommand(object action)
        {
            int notFilledFields = 0;

            if (string.IsNullOrEmpty(urlHWRat))
            {
                notFilledFields++;
            }
            if (string.IsNullOrEmpty(urlCompareSite))
            {
                notFilledFields++;
            }

            if(notFilledFields > 0)
            {
                MessageBox.Show("Beide Felder müssen gefüllt sein!");
                return;
            }

            if(operationMode == OperationMode.CREATE)
            {
                sqlHelper.InsertItem(urlHWRat, urlCompareSite);
            }
            else if (operationMode == OperationMode.UPDATE)
            {
                orginalItem.hardwareRatURL = urlHWRat;
                orginalItem.compareURL = urlCompareSite;
                sqlHelper.UpdateItem(orginalItem);
            }

            View.CloseWindow(true);
        }

        public ICommand buttonCancel
        {
            get { return new DelegateCommand<object>(buttonCancelCommand); }
        }

        public void buttonCancelCommand(object action)
        {
            View.CloseWindow(false);
        }

        public ICommand buttonLoadData
        {
            get { return new DelegateCommand<object>(buttonLoadDataCommand); }
        }

        public void buttonLoadDataCommand(object action)
        {
            if (!string.IsNullOrEmpty(urlHWRat))
            {
                try
                {
                    HtmlWeb webPage = new HtmlWeb();
                    HtmlAgilityPack.HtmlDocument document = new HtmlAgilityPack.HtmlDocument();
                    document = webPage.Load(urlHWRat);
                    hwProductPrice = document.DocumentNode.SelectSingleNode("//span[@class='price--content content--default']/meta").Attributes["content"].Value.Replace(".", ",");
                    hwProductName = document.DocumentNode.SelectSingleNode("//h1[@class='product--title']").InnerText.Replace("\n", "");
                    hwProductPicture = document.DocumentNode.SelectSingleNode("//span[@class='image--media']/img").Attributes["src"].Value;

                    buttonLoadGeizhalsCommand(null);
                }
                catch(Exception)
                {
                    MessageBox.Show("Fehler beim Laden von HardwareRat");
                }

            }

            if (!string.IsNullOrEmpty(urlCompareSite))
            {
                try
                {
                    HtmlWeb webPage = new HtmlWeb();
                    HtmlAgilityPack.HtmlDocument document = new HtmlAgilityPack.HtmlDocument();

                    document = new HtmlAgilityPack.HtmlDocument();
                    document = webPage.Load(urlCompareSite);
                    ghzProductPrice = document.DocumentNode.SelectSingleNode("//span[@class='variant__header__pricehistory__pricerange']//strong//span[@class='gh_price']").InnerText.Replace("€ ", "").Replace("&euro; ", "");
                }
                catch(Exception)
                {
                    MessageBox.Show("Fehler beim Laden von Geizhals");
                }             
            }

            //Differenz errechnen
            if (!string.IsNullOrEmpty(ghzProductPrice))
            {
                if (!string.IsNullOrEmpty(hwProductPrice))
                {
                    double difference = Math.Round(double.Parse(hwProductPrice) - double.Parse(ghzProductPrice), 2);
                    productDifference = difference.ToString();
                    if (difference <= 0)
                    {
                        currentState = "günstiger";
                    }
                    else if (difference > 0 && difference < 3)
                    {
                        currentState = "1-2€ darüber";
                    }
                    else if (difference > 2)
                    {
                        currentState = "3€ oder mehr darüber";
                    }
                }
            }
        }

        public ICommand buttonLoadGeizhals
        {
            get { return new DelegateCommand<object>(buttonLoadGeizhalsCommand); }
        }

        public void buttonLoadGeizhalsCommand(object action)
        {
            //Produkt per Suche laden
            if (!string.IsNullOrEmpty(hwProductName))
            {
                try
                {
                    HtmlWeb webPage = new HtmlWeb();
                    HtmlAgilityPack.HtmlDocument document = new HtmlAgilityPack.HtmlDocument();

                    //Name parsen, damit er akzeptiert wird
                    string searchProduct = hwProductName.Replace(" ", "+").Replace(",", "%2C");

                    document = webPage.Load($"https://geizhals.de/?fs={searchProduct}&hloc=at&in=");

                    //GeizhalsURL Feld befüllen
                    urlCompareSite = "https://geizhals.de/" + document.DocumentNode.SelectSingleNode("//a[@class='listview__name-link']").Attributes["href"].Value;
                }
                catch(Exception)
                {
                    MessageBox.Show("Fehler beim Suchen des Artikel bei Geizhals.");
                }

            }
        }


    }
}
