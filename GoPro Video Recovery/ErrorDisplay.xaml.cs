using System.Collections.ObjectModel;
using System.Windows;

namespace GoPro_Video_Recovery
{
    /// <summary>
    /// Interaction logic for ErrorDisplay.xaml
    /// </summary>
    public partial class ErrorDisplay : Window
    {
        public ErrorDisplay()
        {
            InitializeComponent();
        }
        public ErrorDisplay(ObservableCollection<Error> errors)
        {
            InitializeComponent();
            DataContext = errors;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}