using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Newtonsoft.Json.Linq;
using Windows.UI;
using Windows.Storage.Pickers;
using Windows.Storage;
using Newtonsoft.Json;
using System.Management;

namespace UnmatchedParser
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private StorageFolder folder;
        public MainPage()
        {
            this.InitializeComponent();
        }

        private void OutputNameBox_Enter(object sender, RoutedEventArgs e)
        {
            if (OutputNameBox.Text == "unmatchedbatch")
            {
                SolidColorBrush brush = new SolidColorBrush(Colors.Black);
                OutputNameBox.Foreground = brush;
                OutputNameBox.Text = "";
            }
        }
        private void OutputNameBox_Leave(object sender, RoutedEventArgs e)
        {
            if (OutputNameBox.Text.Length == 0)
            {
                SolidColorBrush brush = new SolidColorBrush(Colors.LightGray);
                OutputNameBox.Foreground = brush;
                OutputNameBox.Text = "unmatchedbatch";
            }
        }

        private async void FolderPickerButton_Click(object sender, RoutedEventArgs e)
        {
            FolderPicker folderPicker = new FolderPicker();
            folderPicker.FileTypeFilter.Add("*");
            folder = await folderPicker.PickSingleFolderAsync();
            if(folder != null)
            {
                FolderDisplayText.Text = folder.Path;
                ParseButton.IsEnabled = true;
            }
        }

        private async void ParseButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                List<string> names = new List<string>();
                List<string> abilities = new List<string>();
                List<string> cardtitles = new List<string>();
                List<string> effects = new List<string>();

                IReadOnlyList<StorageFile> fileList = await folder.GetFilesAsync();

                foreach (StorageFile item in fileList)
                {
                    if (item.FileType == ".json")
                    {
                        string jsonString = await Windows.Storage.FileIO.ReadTextAsync(item);
                        JToken deckData = JObject.Parse(jsonString);
                        JToken token = deckData.SelectToken("ObjectStates[0]");
                        names.Add("" + token["Nickname"]);

                        JArray heroCards = (JArray)deckData.SelectToken("ObjectStates");
                        foreach (JToken card in heroCards)
                        {
                            if ("" + card["Name"] == "Card")
                            {
                                string description = "" + card["Description"];
                                string[] tokenizedDescription = description.Split('\n', StringSplitOptions.RemoveEmptyEntries);

                                string ability = "";
                                for (int i = 2; i < tokenizedDescription.Length; i++)
                                {
                                    ability += tokenizedDescription[i];
                                    ability += " ";
                                }
                                abilities.Add(ability);
                            }
                        }

                        JArray cards = (JArray)deckData.SelectToken("ObjectStates[0].ContainedObjects");
                        foreach (JToken card in cards)
                        {
                            if ("" + card["Name"] == "Card")
                            {
                                string description = "" + card["Description"];
                                string[] tokenizedDescription = description.Split('\n', StringSplitOptions.RemoveEmptyEntries);

                                bool exists = false;
                                foreach (string title in cardtitles)
                                {

                                    if (title == "" + card["Nickname"])
                                    {
                                        exists = true;
                                    }
                                }
                                if (!exists)
                                {
                                    cardtitles.Add("" + card["Nickname"]);
                                    string effect = "";
                                    for (int i = 3; i < tokenizedDescription.Length - 1; i++)
                                    {
                                        effect += tokenizedDescription[i];
                                        effect += " ";
                                    }
                                    effects.Add(effect);
                                }
                            }
                        }
                    }
                }
                Windows.Storage.StorageFolder storageFolder = folder;
                Windows.Storage.StorageFile output =
                    await storageFolder.CreateFileAsync(OutputNameBox.Text + ".txt",
                        Windows.Storage.CreationCollisionOption.ReplaceExisting);

                string outputText = "Names:\n";
                foreach (string s in names)
                {
                    outputText += s;
                    outputText += "\n";
                }
                outputText += "\nAbilities:\n";
                foreach (string s in abilities)
                {
                    outputText += s;
                    outputText += "\n";
                }
                outputText += "\nCard Titles:\n";
                foreach (string s in cardtitles)
                {
                    outputText += s;
                    outputText += "\n";
                }
                outputText += "\nCard Effects:\n";
                foreach (string s in effects)
                {
                    outputText += s;
                    outputText += "\n";
                }

                await Windows.Storage.FileIO.WriteTextAsync(output, outputText);
                Result.Text = "Successfully created file.";
            }
            catch (Exception ex)
            {
                Result.Text = "Failed to create file: " + ex.Message;
            }
        }
    }
}
