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
using MySql.Data.MySqlClient;


namespace Radio
{
    public partial class MainWindow : Window
    {
        readonly List<string> selectedCountries = new();
        readonly List<string> selectedTags = new();

        // Initialize lists for tags and countries
        readonly List<Tag> tags = new();
        readonly List<Country> countries = new();

        // Initialize list for radio stations
        readonly List<RadioStation> radioStations = new();

        string selectedCountry;
        readonly string connectionString = "server=127.0.0.1;user id=radioapp;password=;database=radio";
        public MainWindow()
        {
            InitializeComponent();

            // Connect to MySQL database
            MySqlConnection connection = new(connectionString);
            connection.Open();

            // Get countries from database and add them to the list
            MySqlCommand countryCommand = new("SELECT `kraj_prefix` FROM `stacje`;", connection);
            MySqlDataReader countryReader = countryCommand.ExecuteReader();

            while (countryReader.Read())
            {
                ComboBoxItem item = new()
                {
                    Content = countryReader.GetString("kraj_prefix")
                };

                // Add country to the list if it's not already there
                if (!countries.Exists(x => x.CountryName == countryReader.GetString("kraj_prefix")))
                {
                    Country countryName = new()
                    {
                        CountryName = countryReader.GetString("kraj_prefix")
                    };
                    countries.Add(countryName);
                    comboBox.Items.Add(item);
                }
            }
            countryReader.Close();

            // Get radio stations from database and add them to the list
            MySqlCommand radioStationCommand = new("SELECT `nazwa`, `image_name`, `adres`, `kraj_prefix`, `kategorie` FROM stacje WHERE `aktywny` = 1", connection);
            MySqlDataReader radioStationReader = radioStationCommand.ExecuteReader();
            while (radioStationReader.Read())
            {
                RadioStation radioStation = new()
                {
                    RadioName = radioStationReader.GetString("nazwa"),
                    RadioImage = System.AppDomain.CurrentDomain.BaseDirectory + "..\\..\\..\\img\\" + radioStationReader.GetString("image_name"),
                    RadioStream = radioStationReader.GetString("adres"),
                    RadioCountry = radioStationReader.GetString("kraj_prefix"),
                    RadioTags = radioStationReader.GetString("kategorie")
                };
                radioStations.Add(radioStation);
            }
            radioStationReader.Close();

            connection.Close();

            // Bind tags to the ItemsControl
            Tags.ItemsSource = null;

            // Bind radio stations to the ItemsControl
            RadioButtons.ItemsSource = null;
        }
        private void PlayRadioStream(object sender, RoutedEventArgs e)
        {
            // Get the selected radio station from the button's DataContext
            if (((Button)sender).DataContext is RadioStation radioStation)
            {
                // Set the media player's source to the selected radio station's stream
                media.Source = new Uri(radioStation.RadioStream);
            }
        }

        private void TagCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            // Get the selected tag from the checkbox's content
            string selectedTag = ((CheckBox)sender).Content.ToString();

            // Add the selected tag to the list of selected tags
            if (!selectedTags.Contains(selectedTag))
            {
                selectedTags.Add(selectedTag);
            }

            // Update the radio stations list
            UpdateRadioStations();
        }

        private void TagCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            // Get the selected tag from the checkbox's content
            string selectedTag = ((CheckBox)sender).Content.ToString();

            // Remove the selected tag from the list of selected tags
            if (selectedTags.Contains(selectedTag))
            {
                selectedTags.Remove(selectedTag);
            }

            // Update the radio stations list
            UpdateRadioStations();
        }

        private void CountryComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Clear the selected tags list
            selectedTags.Clear();

            // Uncheck all the checkboxes in the Tags ItemsControl
            foreach (var tag in Tags.Items)
            {
                if (Tags.ItemContainerGenerator.ContainerFromItem(tag) is CheckBox checkBox)
                {
                    checkBox.IsChecked = false;
                }
            }

            // Get the selected country from the combobox's selected item
            selectedCountry = ((ComboBoxItem)comboBox.SelectedItem).Content.ToString();

            // Add the selected country to the list of selected countries
            if (!selectedCountries.Contains(selectedCountry))
            {
                selectedCountries.Add(selectedCountry);
            }
            UpdateTags(selectedCountry);
            // Update the radio stations list
            UpdateRadioStations();
        }

        private void UpdateTags(string country)
        {
            // Clear the tags list
            Tags.ItemsSource = null;
            tags.Clear();

            // Connect to MySQL database
            MySqlConnection connection = new(connectionString);
            connection.Open();

            // Get tags from database that are available in the selected country
            MySqlCommand tagCommand = new("SELECT `kategorie` FROM `stacje` WHERE `kraj_prefix` = @country AND `aktywny` = 1 ORDER BY `kategorie` ASC;", connection);
            tagCommand.Parameters.AddWithValue("@country", country);
            MySqlDataReader tagReader = tagCommand.ExecuteReader();
            while (tagReader.Read())
            {
                // tag record looks like this "tag1>tag2>tag3>", "tag1>" so we need to split it by ">" and add each tag to the list if it's not already there, delete duplicates and sort alphabetically
                string[] tagArray = tagReader.GetString(0).Split('>');
                foreach (string tag in tagArray)
                {
                    if (tag != "")
                    {
                        if (!tags.Exists(x => x.TagName == tag))
                        {
                            Tag tagName = new()
                            {
                                TagName = tag
                            };
                            tags.Add(tagName);
                        }
                    }
                }
            }
            tagReader.Close();
            connection.Close();

            // sort tags alphabetically
            tags.Sort((x, y) => x.TagName.CompareTo(y.TagName));

            // Bind tags to the ItemsControl
            Tags.ItemsSource = tags;
        }


        private void UpdateRadioStations()
        {
            bool isAnyCheckboxChecked = selectedTags.Any();

            if (isAnyCheckboxChecked)
            {
                // Filter radio stations by and tags. I need wrap radio tags in ">" to make sure that the tag is not a part of another tag, for example "rock" is a part of "rock'n'roll", handle multiple tags. Radio need minimum only one tag from selected tags to be displayed. When country is selected, show only radio stations from this country. If for example when country is selected and no tags are selected, show all radio stations from selected country. selectedCountry is empty when no country is selected. I need to handle this case. sort alphabeticly
                RadioButtons.ItemsSource = radioStations.Where(x => x.RadioCountry == selectedCountry && x.RadioTags.Split('>').Any(y => selectedTags.Contains(y))).OrderBy(x => x.RadioName);
            }
            else
            {
                // Update the ItemsSource of the RadioButtons control to show the filtered radio stations
                RadioButtons.ItemsSource = null;
            }
        }
    }


    // Class for tags
    public class Tag
    {
        public string TagName { get; set; }
    }

    // Class for countries
    public class Country
    {
        public string CountryName { get; set; }
    }

    // Class for radio stations
    public class RadioStation
    {
        public string RadioName { get; set; }
        public string RadioImage { get; set; }
        public string RadioStream { get; set; }
        public string RadioCountry { get; set; }
        public string RadioTags { get; set; }
    }
}