using NMBP1.Views;
using Npgsql;
using System;
using System.Collections.Generic;
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

namespace NMBP1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public const string ConnectionString = "Server=localhost; port=5432; User Id=postgres; Password=TinyPro555; Database=Movies";
        NpgsqlConnection conn;

        public MainWindow()
        {
            conn = new NpgsqlConnection(ConnectionString);
            conn.Open();
            NpgsqlCommand com = conn.CreateCommand();
            com.CommandText = "SET DateStyle ='German, DMY';";
            com.ExecuteNonQuery();
            InitializeComponent();
        }

        private void Add_clicked(object sender, RoutedEventArgs e)
        {
            DataContext = new DBAdd(conn);
            Badd.IsEnabled = false;
            Bsearch.IsEnabled = true;
            Bstats.IsEnabled = true;
        }
        private void Search_clicked(object sender, RoutedEventArgs e)
        {
            DataContext = new DBSearch(conn);
            Badd.IsEnabled = true;
            Bsearch.IsEnabled = false;
            Bstats.IsEnabled = true;
        }
        private void Stats_clicked(object sender, RoutedEventArgs e)
        {
            DataContext = new DBStats(conn);
            Badd.IsEnabled = true;
            Bsearch.IsEnabled = true;
            Bstats.IsEnabled = false;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if(conn.State == System.Data.ConnectionState.Open)
            {
                conn.Close();
                conn.Dispose();
                base.OnClosing(e);
            }
        }
    }
}
