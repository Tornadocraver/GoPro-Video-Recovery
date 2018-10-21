using System.ComponentModel;
using System.Windows;

namespace GoPro_Video_Recovery
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (((MainPageViewModel)DataContext).Running)
                e.Cancel = true;
            else
                base.OnClosing(e);
        }
    }
}