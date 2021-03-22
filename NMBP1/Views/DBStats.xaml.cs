using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
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
    /// Interaction logic for DBStats.xaml
    /// </summary>
    public partial class DBStats : UserControl
    {
        NpgsqlConnection conn;
        string content;
        public DBStats(NpgsqlConnection conn)
        {
            this.conn = conn;
            InitializeComponent();
            content = "";
        }
        private void HoursChecked(object sender, RoutedEventArgs e)
        {
            content = "HOURS";
        }

        private void DaysChecked(object sender, RoutedEventArgs e)
        {
            content = "DAYS";
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (!beginDate.SelectedDate.HasValue || !endDate.SelectedDate.HasValue) MessageBox.Show("Please select both dates");
            else if (content == "") MessageBox.Show("Please choose analysis type");
            else if (beginDate.SelectedDate.Value > endDate.SelectedDate.Value) MessageBox.Show("Begin date must be older than end date");
            else
            {
                DateTime begin = beginDate.SelectedDate.Value;
                DateTime end = endDate.SelectedDate.Value;
                string query = "";
                if (content.Equals("DAYS"))
                {
                    query = "CREATE TEMP TABLE allDates(dates varchar(20) primary key);\nINSERT INTO allDates VALUES ('" + begin.ToString("dd.MM.yyyy") + "')";
                    for (int i = 1; i <= (end - begin).Days; i++) query += ", ('" + begin.AddDays(i).ToString("dd.MM.yyyy") + "')";
                    query += ";\nSELECT * FROM crosstab('SELECT querystring::varchar(255), querydate::varchar(20), COUNT(*)::INT as numOfTimes \nFROM analysis WHERE querydate BETWEEN ''"
                                    + begin.ToString("dd.MM.yyyy") + "'' AND ''" + end.ToString("dd.MM.yyyy") + "''\nGROUP BY querystring, querydate\nORDER BY querystring, querydate'";
                    query += ", 'SELECT CAST(dates AS varchar(20)) FROM allDates ORDER BY dates')\nAS pivotTable(querystring varchar(255)";
                    for (int i = 0; i <= (end - begin).Days; i++) query += ", d" + begin.AddDays(i).ToString("dd.MM.yyyy").Replace(".", "") + " varchar(20)";
                    query += ")\nORDER BY querystring;\nDROP TABLE allDates"; 
                    //resultBlock.Text = query;
                }
                else
                {
                    query = "CREATE TEMP TABLE allHours(hours INT);\nINSERT INTO allHours VALUES (0)";
                    for (int i = 1; i < 24; i++) query += ", (" + i + ")";
                    query += ";\nSELECT * FROM crosstab('SELECT querystring::varchar(255), CAST(EXTRACT(HOUR FROM querytime) AS INT) AS hourOfDay, COUNT(*)::INT as numOfTimes \nFROM analysis WHERE querydate BETWEEN ''"
                                    + begin.ToString("dd.MM.yyyy") + "'' AND ''" + end.ToString("dd.MM.yyyy") + "''\nGROUP BY querystring, hourOfDay\nORDER BY querystring, hourOfDay'";
                    query += ", 'SELECT hours FROM allHours ORDER BY hours')\nAS pivotTable(querystring varchar(255)";
                    for (int i = 0; i < 24; i++) query += ", h" + i + "__" + (i+1) + " int";
                    query += ")\nORDER BY querystring;\nDROP TABLE allHours";
                    //resultBlock.Text = query;
                }

                using (NpgsqlCommand com = conn.CreateCommand())
                {
                    com.CommandText = query;
                    com.ExecuteNonQuery();

                    NpgsqlDataAdapter adapter = new NpgsqlDataAdapter(com);
                    DataTable dt = new DataTable("analysis");
                    adapter.Fill(dt);
                    dataGrid.ItemsSource = dt.DefaultView;
                    adapter.Update(dt);
                }
            }


        }
    }
}
