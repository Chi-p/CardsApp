using Game2.Engine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CardsApp
{
    /// <summary>
    /// Interaction logic for CardControl.xaml
    /// </summary>
    public partial class CardControl : UserControl
    {
        public CardControl()
        {
            InitializeComponent();
        }

        private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (DataContext is Card card)
            {
                Img.Visibility = Visibility.Collapsed;
                if(card.Suit.Name=="Hearts" || card.Suit.Name == "Diamonds")
                {
                    BtnMain.Foreground = Brushes.Red;
                }
                else
                {
                    BtnMain.Foreground = Brushes.Black;
                }
            }
            else
                Img.Visibility = Visibility.Visible;
        }
    }
}
