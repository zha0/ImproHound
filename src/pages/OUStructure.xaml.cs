﻿using Neo4j.Driver;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Threading.Tasks;
using System.IO;
using ImproHound.classes;
using System.Text.RegularExpressions;
using System.Collections;

namespace ImproHound.pages
{
    public partial class OUStructurePage : Page
    {
        private readonly MainWindow containerWindow;
        private readonly DBConnection connection;
        private readonly ConnectPage connectPage;
        private readonly DBAction dBAction;
        private readonly int defaultTierNumber = 1;

        private Dictionary<string, ADObject> forest;
        private Hashtable idLookupTable;
        private bool ouStructureSaved = true;


        public OUStructurePage(MainWindow containerWindow, DBConnection connection, ConnectPage connectPage, DBAction dBAction)
        {
            this.containerWindow = containerWindow;
            this.connection = connection;
            this.connectPage = connectPage;
            this.dBAction = dBAction;

            InitializeComponent();
            Initialization();
        }

        private async void Initialization()
        {
            EnableGUIWait();
            idLookupTable = new Hashtable();

            if (dBAction.Equals(DBAction.StartFromScratch))
            {
                await PrepareDB();
                await SetDefaultTiers();
            }
            else if (dBAction.Equals(DBAction.StartOver))
            {
                await DeleteTieringInDB();
                await SetDefaultTiers();
            }

            await BuildOUStructure();

            if (dBAction.Equals(DBAction.StartFromScratch) || dBAction.Equals(DBAction.StartOver))
                await SetDefaultTiersForImprohoundCreatedOUs();

            DisableGUIWait();
        }

        private async Task SetDefaultTiersForImprohoundCreatedOUs()
        {
            // Update DB
            List<IRecord> records;
            try
            {
                records = await connection.Query(@"
                        MATCH (ou:OU {improhoundcreated: true})
                        MATCH (n) WHERE n.distinguishedname ENDS WITH ou.distinguishedname
                        UNWIND labels(n) AS allLabels
                        WITH DISTINCT allLabels, ou WHERE allLabels STARTS WITH 'Tier'
                        WITH ou, allLabels ORDER BY allLabels ASC
                        WITH ou, head(collect(allLabels)) AS rightTier
                        CALL apoc.create.setLabels(ou, ['Base', 'OU', rightTier]) YIELD node
                        RETURN ou.objectid, rightTier
                ");
            }
            catch (Exception err)
            {
                // Error
                MessageBox.Show(err.Message.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                containerWindow.NavigateToPage(connectPage);
                return;
            }

            // Update application data
            foreach (IRecord record in records)
            {
                record.Values.TryGetValue("ou.objectid", out object objectid);
                record.Values.TryGetValue("rightTier", out object tier);

                if (idLookupTable.ContainsKey((string)objectid))
                {
                    ADObject adObject = (ADObject)idLookupTable[(string)objectid];
                    adObject.SetTier(((string)tier).Replace("Tier", ""));
                }
            }
        }

        private async Task PrepareDB()
        {
            // Fix domains without distinguishedname and domain property
            // Happens if you upload BloodHound data in the 'wrong' order
            List<IRecord> records;
            try
            {
                records = await connection.Query(@"
                    MATCH (n:Domain)
                    WHERE NOT EXISTS (n.distinguishedname)
                    RETURN n.name
                ");
            }
            catch (Exception err)
            {
                // Error
                MessageBox.Show(err.Message.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                containerWindow.NavigateToPage(connectPage);
                return;
            }

            foreach (IRecord record in records)
            {
                record.Values.TryGetValue("n.name", out object name);
                string distinguishedname = "DC=" + ((string)name).ToLower().Replace(".", ",DC=");

                try
                {
                    await connection.Query(@"
                        MATCH (n:Domain {name:'" + (string)name + @"'})
                        SET n.distinguishedname = '" + distinguishedname + @"', n.domain = n.name, n.highvalue = true
                        RETURN NULL
                    ");
                }
                catch (Exception err)
                {
                    // Error
                    MessageBox.Show(err.Message.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    containerWindow.NavigateToPage(connectPage);
                    return;
                }
            }

            // Set name and distinguishedname for objects without
            List<IRecord> records1;
            try
            {
                records1 = await connection.Query(@"
                        MATCH (o) WHERE NOT EXISTS(o.distinguishedname)
                        MATCH (d:Domain) WHERE o.objectid STARTS WITH d.domain
                        RETURN o.objectid, o.name, d.domain, d.distinguishedname
                    ");
            }
            catch (Exception err)
            {
                // Error
                MessageBox.Show(err.Message.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                containerWindow.NavigateToPage(connectPage);
                return;
            }

            foreach (IRecord record in records1)
            {
                record.Values.TryGetValue("o.objectid", out object objectid);
                record.Values.TryGetValue("o.name", out object name);
                record.Values.TryGetValue("d.domain", out object domain);
                record.Values.TryGetValue("d.distinguishedname", out object domainDistinguishedname);

                if (name == null)
                {
                    if (objectid.ToString().EndsWith("S-1-5-4"))
                    {
                        name = "Interactive (S-1-5-4)@" + (string)domain;
                    }
                    else if (objectid.ToString().EndsWith("S-1-5-17"))
                    {
                        name = "This Organization (S-1-5-17)@" + (string)domain;
                    }
                    else
                    {
                        name = objectid + "@" + (string)domain;
                    }
                }

                string cn = name.ToString().Substring(0, name.ToString().IndexOf("@"));
                string distinguishedname = "CN=" + cn + "," + domainDistinguishedname;

                try
                {
                    await connection.Query(@"
                            MATCH (o {objectid:'" + (string)objectid + @"'})
                            SET o.name = '" + name.ToString() + @"'
                            SET o.distinguishedname = '" + distinguishedname + @"'
                        ");
                }
                catch (Exception err)
                {
                    // Error
                    MessageBox.Show(err.Message.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    containerWindow.NavigateToPage(connectPage);
                    return;
                }
            }

            // Make sure all objects do not have more than the Base label and the type of object label
            // I have seen service accounts in the BloodHound DB having both User and Computer label
            try
            {
                await connection.Query(@"
                        MATCH (n) WHERE SIZE(LABELS(n)) > 2
                        UNWIND LABELS(n) AS lbls
                        WITH n, lbls ORDER BY lbls ASC
                        WITH n, COLLECT(lbls) AS lblsSort
                        CALL apoc.create.setLabels(n, lblsSort[0..2]) YIELD node
                        RETURN NULL
                    ");
            }
            catch (Exception err)
            {
                // Error
                MessageBox.Show(err.Message.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                containerWindow.NavigateToPage(connectPage);
                return;
            }

        }

        private async Task SetDefaultTiers()
        {
            try
            {
                // Set standard Tier 2 groups and group members
                await connection.Query(@"
                    MATCH (group:Group) WHERE EXISTS(group.distinguishedname)
                    AND substring(group.objectid, size(group.objectid) - 4, 4) IN ['" + String.Join("','", DefaultTieringConstants.tier2GroupRids) + @"']
                    CALL apoc.create.addLabels(group, ['Tier2']) YIELD node
                    WITH group
                    MATCH (principal)-[:MemberOf*1..]->(group) WHERE EXISTS(principal.distinguishedname)
                    CALL apoc.create.addLabels(principal, ['Tier2']) YIELD node
                    RETURN NULL
                ");

                // Set standard Tier 0 groups and group members
                await connection.Query(@"
                    MATCH (group:Group) WHERE EXISTS(group.distinguishedname)
                    AND substring(group.objectid, size(group.objectid) - 4, 4) IN ['" + String.Join("','", DefaultTieringConstants.tier0GroupRids) + @"']
                    CALL apoc.create.addLabels(group, ['Tier0']) YIELD node
                    WITH group
                    MATCH (principal)-[:MemberOf*1..]->(group) WHERE EXISTS(principal.distinguishedname)
                    CALL apoc.create.addLabels(principal, ['Tier0']) YIELD node
                    RETURN NULL
                ");

                // DnsAdmins
                await connection.Query(@"
                    MATCH (group:Group) WHERE EXISTS(group.distinguishedname)
                    AND group.distinguishedname STARTS WITH '" + DefaultTieringConstants.tier0DnsAdmins + @"'
                    CALL apoc.create.addLabels(group, ['Tier0']) YIELD node
                    WITH group
                    MATCH (principal)-[:MemberOf*1..]->(group) WHERE EXISTS(principal.distinguishedname)
                    CALL apoc.create.addLabels(principal, ['Tier0']) YIELD node
                    RETURN NULL
                ");

                // WinRMRemoteWMIUsers__
                await connection.Query(@"
                    MATCH (group:Group) WHERE EXISTS(group.distinguishedname)
                    AND group.distinguishedname STARTS WITH '" + DefaultTieringConstants.tier0WinRMRemoteWMIUsers__ + @"'
                    CALL apoc.create.addLabels(group, ['Tier0']) YIELD node
                    WITH group
                    MATCH (principal)-[:MemberOf*1..]->(group) WHERE EXISTS(principal.distinguishedname)
                    CALL apoc.create.addLabels(principal, ['Tier0']) YIELD node
                    RETURN NULL
                ");


                // Set standard Tier 0 users
                await connection.Query(@"
                    MATCH (user) WHERE EXISTS(user.distinguishedname)
                    AND substring(user.objectid, size(user.objectid) - 4, 4) IN ['" + String.Join("','", DefaultTieringConstants.tier0UserRids) + @"']
                    CALL apoc.create.addLabels(user, ['Tier0']) YIELD node
                    RETURN NULL
                ");

                // Set OUs to be in the same tier as the lowest tier of their content
                await connection.Query(@"
                    MATCH (ou:OU) WHERE EXISTS(ou.distinguishedname)
                    MATCH (ou)-[:Contains*1..]->(obj)
                    UNWIND labels(obj) AS allLabels
                    WITH DISTINCT allLabels, ou WHERE allLabels STARTS WITH 'Tier'
                    WITH ou, allLabels ORDER BY allLabels ASC
                    WITH ou, head(collect(allLabels)) AS rightTier
                    CALL apoc.create.setLabels(ou, ['Base', 'OU', rightTier]) YIELD node
                    RETURN NULL
                ");

                // Set domains to Tier 0
                await connection.Query(@"
                    MATCH (domain:Domain) WHERE EXISTS(domain.distinguishedname)
                    CALL apoc.create.addLabels(domain, ['Tier0']) YIELD node
                    RETURN NULL
                ");

                // Set GPOs to be in the same tier as the lowest tier of the OUs (or domain) linked to
                await connection.Query(@"
                    MATCH (gpo:GPO) WHERE EXISTS(gpo.distinguishedname)
                    MATCH (gpo)-[:GpLink]->(ou)
                    UNWIND labels(ou) AS allLabels
                    WITH DISTINCT allLabels, gpo WHERE allLabels STARTS WITH 'Tier'
                    WITH gpo, allLabels ORDER BY allLabels ASC
                    WITH gpo, head(collect(allLabels)) AS rightTier
                    CALL apoc.create.setLabels(gpo, ['Base', 'GPO', rightTier]) YIELD node
                    RETURN NULL
                ");

                // Set all objects without tier label to default tier
                await connection.Query(@"
                    MATCH (o) WHERE EXISTS(o.distinguishedname)
                    AND NOT ('Tier0' IN labels(o) OR 'Tier1' IN labels(o) OR 'Tier2' IN labels(o))
                    UNWIND labels(o) AS allLabels
                    WITH o, COLLECT(allLabels) + 'Tier" + defaultTierNumber + @"' AS newLabels
                    CALL apoc.create.setLabels(o, newLabels) YIELD node
                    RETURN NULL
                ");

                // Delete higher tier labels for objects in multiple tiers
                await connection.Query(@"
                    MATCH (n)
                    UNWIND labels(n) AS label
                    WITH n, label WHERE label STARTS WITH 'Tier'
                    WITH n, label ORDER BY label ASC
                    WITH n, tail(collect(label)) AS wrongTiers
                    CALL apoc.create.removeLabels(n, wrongTiers) YIELD node
                    RETURN NULL
                ");
            }
            catch (Exception err)
            {
                // Error
                MessageBox.Show(err.Message.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                containerWindow.NavigateToPage(connectPage);
                return;
            }
        }

        private async Task BuildOUStructure()
        {
            forest = new Dictionary<string, ADObject>();

            // Create temp tier property on objects
            try
            {
                await connection.Query(@"
                    MATCH (o)
                    UNWIND LABELS(o) AS lbls
                    WITH o, lbls WHERE lbls STARTS WITH 'Tier'
                    SET o.tier = lbls
                ");
            }
            catch (Exception err)
            {
                // Error
                MessageBox.Show(err.Message.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                containerWindow.NavigateToPage(connectPage);
                return;
            }

            // Get all obejcts with distinguishedname (incl. objects with o.tier = null)
            List<IRecord> records;
            try
            {
                object output;
                records = await connection.Query(@"
                        MATCH (o) WHERE EXISTS(o.distinguishedname)
                        UNWIND LABELS(o) AS adtype
                        WITH o.objectid AS objectid, o.name AS name, o.distinguishedname AS distinguishedname, o.tier AS tier, adtype
                        WHERE adtype IN ['Domain', 'OU', 'Group', 'User', 'Computer', 'GPO']
                        RETURN objectid, name, distinguishedname, tier, adtype ORDER BY size(distinguishedname)
                    ");
                if (!records[0].Values.TryGetValue("objectid", out output))
                {
                    // Unknown error
                    MessageBox.Show("Something went wrong. Neo4j server response format is unexpected.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    containerWindow.NavigateToPage(connectPage);
                    return;
                }
            }
            catch (Exception err)
            {
                // Error
                MessageBox.Show(err.Message.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                containerWindow.NavigateToPage(connectPage);
                return;
            }

            foreach (IRecord record in records)
            {
                record.Values.TryGetValue("objectid", out object objectid);
                record.Values.TryGetValue("name", out object name);
                record.Values.TryGetValue("distinguishedname", out object distinguishedname);
                record.Values.TryGetValue("adtype", out object type);
                record.Values.TryGetValue("tier", out object tier);

                // Get tier
                string tierNumber = defaultTierNumber.ToString();
                if (tier != null) tierNumber = tier.ToString().Replace("Tier", "");

                // Get AD type
                bool gotTypeEnum = Enum.TryParse((string)type, out ADObjectType adType);
                if (!gotTypeEnum) adType = ADObjectType.Unknown;

                string distinguishednameStr = distinguishedname.ToString();

                try
                {
                    if (adType.Equals(ADObjectType.Domain))
                    {
                        ADObject adObject = new ADObject((string)objectid, adType, distinguishednameStr, (string)name, distinguishednameStr, tierNumber, this);
                        forest.Add(adObject.Distinguishedname, adObject);
                        idLookupTable.Add((string)objectid, adObject);
                    }
                    else
                    {
                        ADObject parent = GetParent(distinguishednameStr);
                        string rdnName = distinguishednameStr.Replace("," + parent.Distinguishedname, "");
                        string cn = rdnName.Substring(distinguishednameStr.IndexOf("=") + 1);
                        ADObject adObject = new ADObject((string)objectid, adType, cn, (string)name, distinguishednameStr, tierNumber, this);
                        parent.Children.Add(rdnName, adObject);
                        idLookupTable.Add((string)objectid, adObject);
                    }
                }
                catch
                {
                    Console.Error.WriteLine("Something went wrong when adding this AD object (objectid): " + objectid);
                }
            }

            // Delete temp tier property on objects
            try
            {
                await connection.Query(@"
                    MATCH(o)
                    SET o.tier = NULL
                ");
            }
            catch (Exception err)
            {
                // Error
                MessageBox.Show(err.Message.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                containerWindow.NavigateToPage(connectPage);
                return;
            }

            Console.WriteLine("OU Structure build");
            forestTreeView.ItemsSource = forest.Values.ToList();
        }

        internal async void SetTier(string objectid, string newTier)
        {
            if (!ouStructureSaved) return;

            // Set new tier label for object in DB
            List<IRecord> records;
            try
            {
                records = await connection.Query(@"
                    MATCH (o {objectid:'" + objectid + @"'})
                    UNWIND labels(o) AS allLabels
                    WITH DISTINCT allLabels, o WHERE NOT allLabels STARTS WITH 'Tier'
                    WITH o, COLLECT(allLabels) + 'Tier" + newTier + @"' AS newLabels
                    CALL apoc.create.setLabels(o, newLabels) YIELD node
                    RETURN NULL
                ");
            }
            catch (Exception err)
            {
                // Error
                MessageBox.Show(err.Message.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                containerWindow.NavigateToPage(connectPage);
                return;
            }
        }

        private ADObject GetParent(string distinguishedname)
        {
            // Find the domain the object belongs to
            KeyValuePair<string, ADObject> domain = forest.Where(d => distinguishedname.EndsWith(d.Key)).OrderByDescending(d => d.Key.Length).First();

            if (domain.Key != null)
            {
                string[] oupath = Regex.Split(distinguishedname.Replace("," + domain.Key, ""), @"(?<!\\),");
                ADObject parent = domain.Value;
                if (oupath.Length > 1)
                {
                    for (int i = oupath.Length - 1; i > 0; i--)
                    {
                        bool parentFound = false;
                        foreach (KeyValuePair<string, ADObject> container in parent.GetOUChildren())
                        {
                            if (oupath[i].Equals(container.Key))
                            {
                                parent = container.Value;
                                parentFound = true;
                                break;
                            }
                        }

                        // Containers are missing in BloodHound so they have to be created manually
                        if (!parentFound)
                        {
                            string containerDistinguishedname = oupath[i] + "," + parent.Distinguishedname;
                            string objectId = "container-" + containerDistinguishedname;
                            string cn = oupath[i].Replace("CN=", "");
                            string name = (cn + "@" + domain.Value.Name).ToUpper();
                            string tier = defaultTierNumber.ToString();

                            // Create as OU in application data
                            ADObject adContainer = new ADObject(objectId, ADObjectType.OU, cn, name, containerDistinguishedname, tier, this);
                            idLookupTable.Add((string)objectId, adContainer);
                            parent.Children.Add(oupath[i], adContainer);
                            parent = adContainer;

                            // Create as OU in DB
                            CreateADObjectInDB(objectId, ADObjectType.OU, name, containerDistinguishedname, domain.Value.Name, tier);
                        }
                    }
                }

                return parent;
            }

            throw new Exception("Error: Could not find ADObjects OU/Domain parent");
        }

        private async void CreateADObjectInDB(string objectid, ADObjectType adType, string name, string distinguishedname, string domain, string tier)
        {
            List<IRecord> records;
            try
            {
                records = await connection.Query(@"
                    CREATE (o {objectid:'" + objectid + "', domain:'" + domain + "', distinguishedname:'" + distinguishedname + "', name:'" + name +
                    @"', improhoundcreated: true})
                    WITH o
                    CALL apoc.create.setLabels(o, ['Base', '" + adType.ToString() + "', 'Tier" + tier + @"']) YIELD node
                    RETURN NULL
                ");
            }
            catch (Exception err)
            {
                // Error
                MessageBox.Show(err.Message.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }

        private async Task DeleteTieringInDB()
        {
            List<IRecord> records;
            try
            {
                object output;
                records = await connection.Query(@"
                    CALL db.labels()
                    YIELD label WHERE label STARTS WITH 'Tier'
                    WITH COLLECT(label) AS labels
                    MATCH (n)
                    WITH COLLECT(n) AS nodes, labels
                    CALL apoc.create.removeLabels(nodes, labels)
                    YIELD node RETURN NULL
                ");
                if (!records[0].Values.TryGetValue("NULL", out output))
                {
                    // Unknown error
                    MessageBox.Show("Something went wrong. Neo4j server response format is unexpected.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            catch (Exception err)
            {
                // Error
                MessageBox.Show(err.Message.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }

        private async Task SetTieringInDB()
        {
            // Get all AD objects
            List<ADObject> allADObjects = new List<ADObject>();
            foreach (ADObject topADObject in forest.Values)
            {
                allADObjects.Add(topADObject);
                allADObjects.AddRange(topADObject.GetAllChildren());
            }

            // Devide AD objects into tiers and sort by number of objects in tiers
            Dictionary<string, List<ADObject>> tierDict = allADObjects.GroupBy(g => g.Tier).ToDictionary(group => group.Key, group => group.ToList());
            List<KeyValuePair<string, List<ADObject>>> sortedTierDict = (from entry in tierDict orderby entry.Value.Count descending select entry).ToList();

            // Set all AD object in DB to largest tier
            KeyValuePair<string, List<ADObject>> largestTier = sortedTierDict.First();
            sortedTierDict.RemoveAt(0);
            List<IRecord> records;
            try
            {
                object output;
                string query = @"
                        MATCH(obj) WHERE EXISTS(obj.distinguishedname)
                        CALL apoc.create.addLabels(obj, ['Tier' + " + largestTier.Key + @"]) YIELD node
                        RETURN NULL
                    ";
                records = await connection.Query(query);
                if (!records[0].Values.TryGetValue("NULL", out output))
                {
                    // Unknown error
                    MessageBox.Show("Something went wrong. Neo4j server response format is unexpected.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            catch (Exception err)
            {
                // Error
                MessageBox.Show(err.Message.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Replace tier label for AD object not in largest tier to the right tier
            foreach (var tier in sortedTierDict)
            {
                List<string> tierObjectIds = tier.Value.Select(obj => obj.Objectid).ToList();
                string tierObjectIdsString = "['" + String.Join("','", tierObjectIds) + "']";
                try
                {
                    object output;
                    string query = @"
                        MATCH (n:Tier" + largestTier.Key + @")
                        WHERE n.objectid IN " + tierObjectIdsString + @"
                        WITH COLLECT(n) AS nList
                        CALL apoc.refactor.rename.label('Tier' + " + largestTier.Key + ", 'Tier' + " + tier.Key + @", nList) YIELD indexes
                        RETURN NULL
                    ";
                    records = await connection.Query(query);
                    if (!records[0].Values.TryGetValue("NULL", out output))
                    {
                        // Unknown error
                        MessageBox.Show("Something went wrong. Neo4j server response format is unexpected.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
                catch (Exception err)
                {
                    // Error
                    MessageBox.Show(err.Message.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
        }

        private void resetOUStructure()
        {
            GetAllADObjects().ForEach(obj => obj.SetTier(defaultTierNumber.ToString()));
        }

        private List<ADObject> GetAllADObjects()
        {
            List<ADObject> allADObjects = new List<ADObject>();
            foreach (ADObject topADObject in forest.Values)
            {
                allADObjects.Add(topADObject);
                allADObjects.AddRange(topADObject.GetAllChildren());
            }
            return allADObjects;
        }

        private void EnableGUIWait()
        {
            Mouse.OverrideCursor = Cursors.Wait;
            mouseblock.Visibility = Visibility.Visible;
        }

        private void DisableGUIWait()
        {
            Mouse.OverrideCursor = null;
            mouseblock.Visibility = Visibility.Hidden;
        }

        /// BUTTON CLICKS

        private async void resetButton_Click(object sender, RoutedEventArgs e)
        {
            EnableGUIWait();
            MessageBoxResult messageBoxResult = MessageBox.Show("Reset will delete tier labels in DB and set all objects in the OU structure to Tier " + defaultTierNumber.ToString(),
                "Warning", MessageBoxButton.OKCancel, MessageBoxImage.Warning);

            if (messageBoxResult.Equals(MessageBoxResult.OK))
            {
                ouStructureSaved = false;
                resetOUStructure();
                await DeleteTieringInDB();
            }
            DisableGUIWait();
        }

        private async void saveButton_Click(object sender, RoutedEventArgs e)
        {
            EnableGUIWait();
            if (!ouStructureSaved)
            {
                await DeleteTieringInDB();
                await SetTieringInDB();
                ouStructureSaved = true;
            }
            DisableGUIWait();
        }

        private async void setChildrenButton_Click(object sender, RoutedEventArgs e)
        {
            if (forestTreeView.SelectedItem == null) return;

            EnableGUIWait();

            // Set GUI
            ADObject parent = (ADObject)forestTreeView.SelectedItem;
            parent.GetAllChildren().ForEach(child => child.SetTier(parent.Tier));
            forestTreeView.Focus();

            if (ouStructureSaved)
            {
                // Update DB
                try
                {
                    if (parent.Type.Equals(ADObjectType.Domain))
                    {
                        // Delete current tier label
                        await connection.Query(@"
                            CALL db.labels()
                            YIELD label WHERE label STARTS WITH 'Tier'
                            WITH COLLECT(label) AS labels
                            MATCH (n {domain:'" + parent.Name + @"'}) WHERE EXISTS(n.distinguishedname)
                            WITH COLLECT(n) AS nodes, labels
                            CALL apoc.create.removeLabels(nodes, labels) YIELD node
                            RETURN NULL
                        ");

                        // Add new tier label
                        await connection.Query(@"
                            MATCH (n {domain:'" + parent.Name + @"'}) WHERE EXISTS(n.distinguishedname)
                            WITH COLLECT(n) AS nodes
                            CALL apoc.create.addLabels(nodes, ['Tier" + parent.Tier + @"']) YIELD node
                            RETURN NULL
                        ");
                    }
                    else
                    {
                        // Delete current tier label
                        await connection.Query(@"
                            CALL db.labels()
                            YIELD label WHERE label STARTS WITH 'Tier'
                            WITH COLLECT(label) AS labels
                            MATCH (n) WHERE n.distinguishedname ENDS WITH '," + parent.Distinguishedname + @"'
                            WITH COLLECT(n) AS nodes, labels
                            CALL apoc.create.removeLabels(nodes, labels) YIELD node
                            RETURN NULL
                        ");

                        // Add new tier label
                        await connection.Query(@"
                            MATCH (n) WHERE n.distinguishedname ENDS WITH '," + parent.Distinguishedname + @"'
                            WITH COLLECT(n) AS nodes
                            CALL apoc.create.addLabels(nodes, ['Tier" + parent.Tier + @"']) YIELD node
                            RETURN NULL
                        ");
                    }
                }
                catch (Exception err)
                {
                    // Error
                    MessageBox.Show(err.Message.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            DisableGUIWait();
        }

        private async void setMembersButton_Click(object sender, RoutedEventArgs e)
        {
            if (forestTreeView.SelectedItem == null) return;

            EnableGUIWait();

            ADObject group = (ADObject)forestTreeView.SelectedItem;

            if (!ouStructureSaved)
            {
                await DeleteTieringInDB();
                await SetTieringInDB();
                ouStructureSaved = true;
            }

            // Update DB
            List<IRecord> records;
            try
            {
                records = await connection.Query(@"
                    MATCH(o)-[:MemberOf*1..]->(group:Group {objectid:'" + group.Objectid + @"'}) WHERE EXISTS(o.distinguishedname)
                    UNWIND labels(o) AS tierlabel
                    WITH o, tierlabel
                    WHERE tierlabel STARTS WITH 'Tier'
                    CALL apoc.create.removeLabels(o, [tierlabel]) YIELD node
                    WITH o
                    CALL apoc.create.addLabels(o, ['Tier" + group.Tier + @"']) YIELD node
                    RETURN o.objectid
                ");
            }
            catch (Exception err)
            {
                // Error
                MessageBox.Show(err.Message.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Update application data
            foreach (IRecord record in records)
            {
                record.Values.TryGetValue("o.objectid", out object objectid);

                ADObject adObject = (ADObject)idLookupTable[(string)objectid];
                adObject.SetTier(group.Tier);
            }

            forestTreeView.Focus();
            DisableGUIWait();
        }


        private async void setTierGPOsButton_Click(object sender, RoutedEventArgs e)
        {
            EnableGUIWait();

            if (!ouStructureSaved)
            {
                await DeleteTieringInDB();
                await SetTieringInDB();
                ouStructureSaved = true;
            }

            // Set GPOs to the lowest tier of the GPOs they are linked to
            List<IRecord> records;
            try
            {
                records = await connection.Query(@"
                    MATCH(gpo: GPO)
                    MATCH(gpo) -[:GpLink]->(ou)
                    UNWIND labels(ou) AS allLabels
                    WITH DISTINCT allLabels, gpo WHERE allLabels STARTS WITH 'Tier'
                    WITH gpo, allLabels ORDER BY allLabels ASC
                    WITH gpo, head(collect(allLabels)) AS lowestTier
                    CALL apoc.create.setLabels(gpo, ['Base', 'GPO', lowestTier]) YIELD node
                    RETURN gpo.objectid, lowestTier
                ");
            }
            catch (Exception err)
            {
                // Error
                MessageBox.Show(err.Message.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Update application data
            foreach (IRecord record in records)
            {
                record.Values.TryGetValue("gpo.objectid", out object objectid);
                record.Values.TryGetValue("lowestTier", out object lowestTier);

                ADObject gpo = (ADObject)idLookupTable[(string)objectid];
                gpo.SetTier(lowestTier.ToString().Replace("Tier", ""));
            }

            forestTreeView.Focus();
            DisableGUIWait();
        }

        private async void getTieringViolationsButton_Click(object sender, RoutedEventArgs e)
        {
            EnableGUIWait();

            if (!ouStructureSaved)
            {
                await DeleteTieringInDB();
                await SetTieringInDB();
                ouStructureSaved = true;
            }

            // Generate list of tier pairs (source, target)
            List<ADObject> allADObjects = GetAllADObjects();
            List<string> tiers = allADObjects.Select(o => o.Tier).Distinct().OrderByDescending(tier => tier).ToList();
            List<string[]> tierPairs = new List<string[]>();
            for (int i = 0; i < tiers.Count - 1; i++)
            {
                for (int j = i + 1; j < tiers.Count; j++)
                {
                    tierPairs.Add(new string[] { tiers.ElementAt(i), tiers.ElementAt(j) });
                }
            }

            // Prepare csv contents
            string csvHeaderADObjects = @"Tier;
                                Type;
                                Name;
                                Distinguishedname";
            string csvHeaderViolations = @"SourceTier;
                                SourceType;
                                SourceName;
                                SourceDistinguishedname;
                                Relation;
                                IsInherited;
                                TargetTier;
                                TargetType;
                                TargetName;
                                TargetDistinguishedname";
            List<string> csvADObjects = new List<string>() { String.Concat(csvHeaderADObjects.Where(c => !Char.IsWhiteSpace(c))) };
            List<string> csvViolations = new List<string>() { String.Concat(csvHeaderViolations.Where(c => !Char.IsWhiteSpace(c))) };

            // Create csv content: ADobjects
            foreach (ADObject aDObject in allADObjects)
            {
                csvADObjects.Add("Tier" + aDObject.Tier + ";" +
                    aDObject.Type + ";" +
                    aDObject.Name + ";" +
                    aDObject.Distinguishedname);
            }

            // Create csv content: Tiering violations
            foreach (string[] tierPair in tierPairs)
            {
                string sourceTier = "Tier" + tierPair[0];
                string targetTier = "Tier" + tierPair[1];

                List<IRecord> records;
                try
                {
                    records = await connection.Query(@"
                        MATCH (sourceObj:" + sourceTier + ") -[r]->(targetObj:" + targetTier + @")
                        UNWIND LABELS(sourceObj) AS sourceObjlbls
                        UNWIND LABELS(targetObj) AS targetObjlbls
                        WITH sourceObj, sourceObjlbls, targetObjlbls, r, targetObj
                        WHERE sourceObjlbls <> '" + sourceTier + @"' AND sourceObjlbls <> 'Base'
                        AND targetObjlbls <> '" + targetTier + @"' AND targetObjlbls <> 'Base'
                        RETURN sourceObjlbls, sourceObj.name, sourceObj.distinguishedname, TYPE(r), r.isinherited, targetObjlbls, targetObj.name, targetObj.distinguishedname
                    ");
                }
                catch (Exception err)
                {
                    // Error
                    MessageBox.Show(err.Message.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                foreach (IRecord record in records)
                {
                    record.Values.TryGetValue("sourceObj.name", out object sourceName);
                    record.Values.TryGetValue("sourceObj.distinguishedname", out object sourceDistinguishedname);
                    record.Values.TryGetValue("sourceObjlbls", out object sourceType);
                    record.Values.TryGetValue("TYPE(r)", out object relation);
                    record.Values.TryGetValue("r.isinherited", out object isinherited);
                    record.Values.TryGetValue("targetObj.name", out object targetName);
                    record.Values.TryGetValue("targetObj.distinguishedname", out object targetDistinguishedname);
                    record.Values.TryGetValue("targetObjlbls", out object targetType);

                    csvViolations.Add(sourceTier + ";" +
                        sourceType + ";" +
                        sourceName + ";" +
                        sourceDistinguishedname + ";" +
                        relation + ";" +
                        isinherited + ";" +
                        targetTier + ";" +
                        targetType + ";" +
                        targetName + ";" +
                        targetDistinguishedname);
                }
            }

            // Save csvs
            string timeStamp = DateTime.Now.ToString("yyyyMMddHHmmssffff");
            string csvFilenameADObjects = "adobjects-" + timeStamp + ".csv";
            string csvFilenameViolations = "tiering-violations-" + timeStamp + ".csv";
            File.AppendAllLines(csvFilenameADObjects, csvADObjects);
            File.AppendAllLines(csvFilenameViolations, csvViolations);
            MessageBox.Show("ADObject list and tiering violations csvs saved in:\n\n" + Path.GetFullPath(csvFilenameADObjects) + "\n\n" + Path.GetFullPath(csvFilenameViolations),
                "Success", MessageBoxButton.OK, MessageBoxImage.Information);

            DisableGUIWait();
        }

        /// OTHER GUI CHANGES

        private void forestTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            setChildrenButton.IsEnabled = (forestTreeView.SelectedItem != null) &&
                (((ADObject)forestTreeView.SelectedItem).Type.Equals(ADObjectType.Domain) || ((ADObject)forestTreeView.SelectedItem).Type.Equals(ADObjectType.OU));
            setMembersButton.IsEnabled = (forestTreeView.SelectedItem != null) &&
                ((ADObject)forestTreeView.SelectedItem).Type.Equals(ADObjectType.Group);
        }
    }

    public enum DBAction
    {
        StartFromScratch, Continue, StartOver
    }
}
