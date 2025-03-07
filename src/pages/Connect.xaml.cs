﻿using Neo4j.Driver;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ImproHound.pages
{
    public partial class ConnectPage : Page
    {
        public ConnectPage()
        {
            MainWindow.SetConnectPage(this);
            InitializeComponent();
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                EnableGUIWait();

                // Make sure we can connect to the DB and the graph is not empty
                DBConnection.Connect(url.Text, username.Text, password.Password);

                List<IRecord> response = await DBConnection.Query("CALL apoc.meta.stats() YIELD labels RETURN labels");
                if (!response[0].Values.TryGetValue("labels", out object output))
                {
                    // Unknown error
                    MessageBox.Show("Something went wrong.\nNo authentication error but could not fetch number of nodes in graph.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    DisableGUIWait();
                    return;
                }

                // Ensure graph is not empty
                Dictionary<string, object> dirout = (Dictionary<string, object>)output;
                object numOfBase;
                dirout.TryGetValue("Base", out numOfBase);
                if (numOfBase == null)
                {
                    // 0 nodes in graph error
                    MessageBox.Show("You have 0 nodes with label 'Base' in your graph.\nMake sure you have upload BloodHound data to graph before connecting.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    DisableGUIWait();
                    return;
                }

                // Get number of nodes with tier label in db
                response = await DBConnection.Query(@"CALL db.labels()
                    YIELD label WHERE label STARTS WITH 'Tier'
                    MATCH(n) WHERE label IN labels(n)
                    RETURN COUNT(n)");
                if (!response[0].Values.TryGetValue("COUNT(n)", out output))
                {
                    // Unknown error
                    MessageBox.Show("Something went wrong.\nNo authentication error but could not fetch number of nodes in graph.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    DisableGUIWait();
                    return;
                }
                response[0].Values.TryGetValue("COUNT(n)", out object numOfTierLabeled);

                // Get number of nodes with distinguished name in db
                response = await DBConnection.Query("MATCH(n) WHERE EXISTS(n.distinguishedname) RETURN COUNT(n)");
                if (!response[0].Values.TryGetValue("COUNT(n)", out output))
                {
                    // Unknown error
                    MessageBox.Show("Something went wrong.\nNo authentication error but could not fetch number of nodes in graph.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    DisableGUIWait();
                    return;
                }
                response[0].Values.TryGetValue("COUNT(n)", out object numOfDistinguishedname);

                bool alreadyTieredCorrectly = numOfDistinguishedname.ToString().Equals(numOfTierLabeled.ToString());

                DisableGUIWait();

                if (alreadyTieredCorrectly)
                {
                    // Jump to alreay tiered page
                    MainWindow.NavigateToPage(new AlreadyTieredPage());
                }
                else
                {
                    // Jump to OU structure page
                    MainWindow.NavigateToPage(new OUStructurePage(DBAction.StartFromScratch));
                }
            }
            catch (Exception err)
            {
                // Error
                if (err.Message.ToString().StartsWith("There is no procedure with the name"))
                {
                    MessageBox.Show("Procedure 'apoc.meta.stats()' does not exist. Make sure APOC plugin is installed in database.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    MessageBox.Show(err.Message.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                DisableGUIWait();
            }
        }

        private void EnableGUIWait()
        {
            Mouse.OverrideCursor = Cursors.Wait;
            url.IsEnabled = false;
            username.IsEnabled = false;
            password.IsEnabled = false;
            connectButton.IsEnabled = false;
        }

        private void DisableGUIWait()
        {
            Mouse.OverrideCursor = null;
            url.IsEnabled = true;
            username.IsEnabled = true;
            password.IsEnabled = true;
            connectButton.IsEnabled = true;
        }
    }
}
