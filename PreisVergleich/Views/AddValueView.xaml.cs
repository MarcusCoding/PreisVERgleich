using PreisVergleich.Models;
using PreisVergleich.ViewModel;
using System.Windows;


namespace PreisVergleich.Views
{

    /// <summary>
    /// Interaktionslogik für AddValueView.xaml
    /// </summary>
    public partial class AddValueView : Window, ICloseWindow
    {

        public AddValueView(OperationMode mode, ProduktModell selectedItem)
        {
            InitializeComponent();
            this.DataContext = new AddValueViewModel(mode, selectedItem);
            (DataContext as AddValueViewModel).View = this as ICloseWindow;
        }

        public void CloseWindow()
        {
            this.Close();
        }

    }

    public interface ICloseWindow
    {
        void CloseWindow();
    }
}
