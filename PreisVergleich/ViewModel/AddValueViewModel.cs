using PreisVergleich.Helper;
using PreisVergleich.Models;
using Prism.Commands;
using System;
using System.Windows;
using System.Windows.Input;
using PreisVergleich.Views;

namespace PreisVergleich.ViewModel
{
    public class AddValueViewModel
    {
        public string urlHWRat { get; set; }

        public string urlCompareSite { get; set; }

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

            View.CloseWindow();
        }

        public ICommand buttonCancel
        {
            get { return new DelegateCommand<object>(buttonCancelCommand); }
        }

        public void buttonCancelCommand(object action)
        {
            View.CloseWindow();
        }

    }
}
