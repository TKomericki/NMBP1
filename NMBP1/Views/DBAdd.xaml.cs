using Npgsql;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NMBP1.Views
{
    /// <summary>
    /// Interaction logic for DBAdd.xaml
    /// </summary>
    public partial class DBAdd : UserControl
    {
        NpgsqlConnection conn;
        public DBAdd(NpgsqlConnection conn)
        {
            this.conn = conn;
            InitializeComponent();
        }

        private void AddToDB(object sender, RoutedEventArgs e)
        {
            if (title.Text == "" || category.Text == "") MessageBox.Show("Please fill in Title and Categories fields.");
            else
            {
                try
                {
                    using (NpgsqlCommand com = conn.CreateCommand())
                    {
                        com.CommandText = "INSERT into movie (title, categories, summary, description) values('" + title.Text + "', '" + category.Text + "', '" + summary.Text + "', '" + description.Text + "');";
                        com.ExecuteNonQuery();
                        MessageBox.Show("Movie has been added successfully.");
                    }
                }
                catch(Exception ex)
                {
                    MessageBox.Show("Something has gone wrong.");
                }
            }
        }
    }
}
