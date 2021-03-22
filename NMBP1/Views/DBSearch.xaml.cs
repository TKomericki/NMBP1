using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Web;
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
    /// Interaction logic for DBSearch.xaml
    /// </summary>
    public partial class DBSearch : UserControl
    {
        NpgsqlConnection conn;
        String content;
        String connector;
        public DBSearch(NpgsqlConnection conn)
        {
            this.conn = conn;
            content = "";
            connector = "";
            InitializeComponent();          
        }

        private void SearchDB(object sender, RoutedEventArgs e)
        {
            if (content == "") MessageBox.Show("Please select search type");
            else if (searchQuery.Text.Trim() == "") MessageBox.Show("Please input your search");
            else
            {
                String[] search = System.Text.RegularExpressions.Regex.Split(searchQuery.Text.Trim(), "\\s+");
                List<string> text = new List<string>();
                List<string> analysisList = new List<String>();

                for(int i = 0; i < search.Length; i++)
                {
                    if (search[i].StartsWith("\""))
                    {
                        if (search[i].EndsWith("\""))
                        {
                            text.Add(search[i]);
                            analysisList.Add(search[i]);
                        }
                        else
                        {
                            string complexText = "";
                            string simpleText = "";

                            do
                            {
                                complexText += search[i] + " & ";
                                simpleText += search[i] + " ";

                                i++;
                            } while (!search[i].EndsWith("\"") && i != (search.Length - 1));

                            complexText += search[i];
                            simpleText += search[i];

                            if (complexText.EndsWith("\""))
                            {
                                complexText = complexText.Substring(1, complexText.Length - 2);
                                simpleText = simpleText.Substring(1, simpleText.Length - 2);
                            }
                            else
                            {
                                complexText = complexText.Substring(1, complexText.Length - 1);
                                simpleText = simpleText.Substring(1, simpleText.Length - 1);
                            }
                            text.Add(complexText);
                            analysisList.Add(simpleText);
                        }
                    }
                    else
                    {
                        text.Add(search[i]);
                        analysisList.Add(search[i]);
                    }
                }

                text.Sort();
                analysisList.Sort();

                string tsVector = "'";
                string analysis = "''" + analysisList[0] + "''";

                if (text[0].Contains(" ")) tsVector += "(" + text[0] + ")";
                else tsVector += text[0];

                for (int i = 1; i < text.Count; i++) {
                    if(text[i].Contains(" ")) tsVector += " " + connector + " (" + text[i] + ")";
                    else tsVector += " " + connector + " " + text[i];
                    analysis += " " + connector + " ''" + analysisList[i] + "''";
                }
                tsVector += "'";

                result.Text = "SELECT movieid,\nts_headline(title, to_tsquery('english', " + tsVector +
                              ")) title__headline,\nts_headline(description, to_tsquery('english', " + tsVector +
                              ")) desc__headline,\ndescription,\nts_rank(movieTSV, to_tsquery('english', " + tsVector +
                              "), 1) rank\nFROM movie\nWHERE movieTSV @@ to_tsquery('english', '" + text[0] + "')\n";
                if (content == "AND")
                {
                    for (int i = 1; i < text.Count; i++) result.Text += "AND movieTSV @@ to_tsquery('english', '" + text[i] + "')\n";
                }
                else
                {
                    for (int i = 1; i < text.Count; i++) result.Text += "OR movieTSV @@ to_tsquery('english', '" + text[i] + "')\n";
                }
                result.Text += "ORDER BY rank DESC";


            using(NpgsqlCommand com = conn.CreateCommand())
                {
                    com.CommandText = result.Text;
                    com.ExecuteNonQuery();

                    NpgsqlDataAdapter adapter = new NpgsqlDataAdapter(com);
                    DataTable dt = new DataTable("movie");
                    adapter.Fill(dt);
                    dataGrid.ItemsSource = dt.DefaultView;
                    adapter.Update(dt);
                }

            using(NpgsqlCommand com = conn.CreateCommand())
                {
                    com.CommandText = "INSERT INTO analysis(querystring, querydate, querytime) VALUES('" + analysis + "', current_date, current_time)";
                    com.ExecuteNonQuery();
                }         
            }
        }

        private void ORChecked(object sender, RoutedEventArgs e)
        {
            content = "OR";
            connector = "|";
        }

        private void ANDChecked(object sender, RoutedEventArgs e)
        {
            content = "AND";
            connector = "&";
        }

        private void searchQuery_TextChanged(object sender, TextChangedEventArgs e)
        {
            List<string> autocomplete = new List<string>();
            string query = searchQuery.Text.Trim();
            if (query.Length != 0 && query[query.Length - 1] != '\"')
            {
                if (searchQuery.Text[searchQuery.Text.Length - 1] == ' ' && System.Text.RegularExpressions.Regex.Split(query, "\"").Length % 2 == 0 || searchQuery.Text[searchQuery.Text.Length - 1] != ' ')
                {
                    String[] search = System.Text.RegularExpressions.Regex.Split(query, "\"");
                    if (search.Length % 2 != 0) search = System.Text.RegularExpressions.Regex.Split(query, "\\s+");
                    string lastWord = search[search.Length - 1];

                    string titleString = "similarity(title, '" + lastWord + "')";
                    string summaryString = "similarity(summary, '" + lastWord + "')";
                    query = "SELECT CASE WHEN " + titleString + " >= " + summaryString + " THEN title\nWHEN " +
                            titleString + " < " + summaryString + " THEN summary\nEND AS autocomplete\nFROM movie WHERE " +
                            titleString + " > 0.05\nOR " + summaryString + " > 0.01\nORDER BY GREATEST(similarity(title, '" + lastWord + "'), similarity(summary, '" + lastWord + "')) DESC\nLIMIT 5";
                    //result.Text += "\n" + query;

                    using (NpgsqlCommand com = conn.CreateCommand())
                    {
                        com.CommandText = query;
                        com.ExecuteNonQuery();
                        NpgsqlDataReader reader = com.ExecuteReader();
                        result.Text = "";


                        while (reader.Read())
                        {
                            autocomplete.Add(reader[0].ToString());
                            //result.Text += reader[0] + "\n";
                        }
                        reader.Close();
                    }
                }
            }
            if (autocomplete.Count == 0) autocompleter.Visibility = Visibility.Collapsed;
            else
            {
                autocompleter.ItemsSource = autocomplete;
                autocompleter.Visibility = Visibility.Visible;
                autocompleter.Height = autocomplete.Count * 22;
            }
        }

        private void searchQuery_LostFocus(object sender, RoutedEventArgs e)
        {
            autocompleter.Visibility = Visibility.Collapsed;
        }

        private void autocompleter_Selected(object sender, RoutedEventArgs e)
        {
            if (autocompleter.SelectedItem == null) return;
            string newQuery = autocompleter.SelectedItem.ToString();
            string[] queries;
            string last;
            if(System.Text.RegularExpressions.Regex.Split(searchQuery.Text, "\"").Length % 2 == 0)
            {
                queries = System.Text.RegularExpressions.Regex.Split(searchQuery.Text, "\"");
                last = "\"" + queries[queries.Length - 1];
            }
            else
            {
                queries = System.Text.RegularExpressions.Regex.Split(searchQuery.Text, " ");
                last = queries[queries.Length - 1];
            }
            if (newQuery.Contains(" ")) searchQuery.Text = searchQuery.Text.Substring(0, searchQuery.Text.LastIndexOf(last)) + "\"" + newQuery + "\"";
            else searchQuery.Text = searchQuery.Text.Substring(0, searchQuery.Text.LastIndexOf(last)) + newQuery + " ";
        }
    }
}
