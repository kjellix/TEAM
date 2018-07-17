﻿using System;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;

namespace TEAM
{

    public partial class FormModelMetadata : FormBase
    {
        FormAlert _alert;
        private BindingSource bindingSourceTableMetadata = new BindingSource();

        public FormModelMetadata()
        {
            InitializeComponent();
        }

        public class ModelMetadataJson
        {
            public string versionAttributeHash { get; set; }
            public string versionId { get; set; }
            public string tableName { get; set; }
            public string columnName { get; set; }
            public string dataType { get; set; }
            public string characterMaximumLength { get; set; }
            public string numericPrecision { get; set; }
            public string ordinalPosition { get; set; }
            public string primaryKeyIndicator { get; set; }
            public string multiActiveIndicator { get; set; }
        }

        public FormModelMetadata(FormMain parent): base(parent)
        {
            InitializeComponent();

            radiobuttonNoVersionChange.Checked = true;

            InitialiseVersionTrackbar();

            var configurationSettings = new ConfigurationSettings();

            // Get the latest version
            var connOmd = new SqlConnection { ConnectionString = configurationSettings.ConnectionStringOmd };
            var selectedVersion = GetMaxVersionId(connOmd);

            // Populate datagrid
            PopulateTableGridWithVersion(selectedVersion);
        }

        [STAThread]
        private void PopulateTableGridWithVersion(int versionId)
        {
            var configurationSettings = new ConfigurationSettings();

            var repositoryTarget = configurationSettings.metadataRepositoryType;

            if (repositoryTarget == "SQLServer") //Queries the tables in SQL Server
            {
                // open latest version
                var connOmd = new SqlConnection {ConnectionString = configurationSettings.ConnectionStringOmd};

                int selectedVersion = versionId;

                try
                {
                    connOmd.Open();
                }
                catch (Exception exception)
                {
                    richTextBoxInformation.Text += exception.Message;
                }

                var sqlStatementForLatestVersion = new StringBuilder();

                sqlStatementForLatestVersion.AppendLine("SELECT ");
                sqlStatementForLatestVersion.AppendLine(" [VERSION_ATTRIBUTE_HASH],");
                sqlStatementForLatestVersion.AppendLine(" CAST([VERSION_ID] AS VARCHAR(100)) AS VERSION_ID,");
                sqlStatementForLatestVersion.AppendLine(" [TABLE_NAME],");
                sqlStatementForLatestVersion.AppendLine(" [COLUMN_NAME],");
                sqlStatementForLatestVersion.AppendLine(" [DATA_TYPE],");
                sqlStatementForLatestVersion.AppendLine(" CAST([CHARACTER_MAXIMUM_LENGTH] AS VARCHAR(100)) AS CHARACTER_MAXIMUM_LENGTH,");
                sqlStatementForLatestVersion.AppendLine(" CAST([NUMERIC_PRECISION] AS VARCHAR(100)) AS NUMERIC_PRECISION,");
                sqlStatementForLatestVersion.AppendLine(" CAST([ORDINAL_POSITION] AS VARCHAR(100)) AS ORDINAL_POSITION,");
                sqlStatementForLatestVersion.AppendLine(" [PRIMARY_KEY_INDICATOR],");
                sqlStatementForLatestVersion.AppendLine(" [MULTI_ACTIVE_INDICATOR]");
                sqlStatementForLatestVersion.AppendLine("FROM [MD_VERSION_ATTRIBUTE]");
                sqlStatementForLatestVersion.AppendLine("WHERE [VERSION_ID] = " + selectedVersion);

                var versionList = GetDataTable(ref connOmd, sqlStatementForLatestVersion.ToString());
                bindingSourceTableMetadata.DataSource = versionList;

                // Set the column header names.
                dataGridViewTableMetadata.DataSource = bindingSourceTableMetadata;
                dataGridViewTableMetadata.ColumnHeadersVisible = true;
                dataGridViewTableMetadata.Columns[0].Visible = false;
                dataGridViewTableMetadata.Columns[1].Visible = false;

                dataGridViewTableMetadata.Columns[0].HeaderText = "Hash Key"; //Key column
                dataGridViewTableMetadata.Columns[1].HeaderText = "Version ID"; //Key column
                dataGridViewTableMetadata.Columns[2].HeaderText = "Table Name"; //Key column
                dataGridViewTableMetadata.Columns[3].HeaderText = "Column Name"; //Key column
                dataGridViewTableMetadata.Columns[4].HeaderText = "Data Type";
                dataGridViewTableMetadata.Columns[5].HeaderText = "Length";
                dataGridViewTableMetadata.Columns[6].HeaderText = "Precision";
                dataGridViewTableMetadata.Columns[7].HeaderText = "Position";
                dataGridViewTableMetadata.Columns[8].HeaderText = "Primary Key";
                dataGridViewTableMetadata.Columns[9].HeaderText = "Multi-Active";

            }
            else if (repositoryTarget == "JSON") //Update the JSON
            {
                //Check if the file exists, otherwise create a dummy / empty file   
                if (!File.Exists(configurationSettings.ConfigurationPath + GlobalParameters.jsonModelMetadataFileName))
                {
                    richTextBoxInformation.AppendText("No JSON file was found, so a new empty one was created");

                    JArray outputFileArray = new JArray();

                    JObject dummyJsonTableMappingFile = new JObject(
                        new JProperty("versionAttributeHash", "NewHash"),
                        new JProperty("tableName", "Sample Table"),
                        new JProperty("columnName", "Sample Column"),
                        new JProperty("dataType", "nvarchar"),
                        new JProperty("characterMaximumLength", "100"),
                        new JProperty("numericPrecision", "0"),
                        new JProperty("ordinalPosition", "1"),
                        new JProperty("primaryKeyIndicator", "N"),
                        new JProperty("multiActiveIndicator", "N")
                        );

                    outputFileArray.Add(dummyJsonTableMappingFile);

                    string json = JsonConvert.SerializeObject(outputFileArray, Formatting.Indented);

                    File.WriteAllText(configurationSettings.ConfigurationPath + GlobalParameters.jsonModelMetadataFileName, json);

                }

                // Load the file, convert it to a DataTable and bind it to the source
                List<ModelMetadataJson> jsonArray =
                    JsonConvert.DeserializeObject<List<ModelMetadataJson>>(
                        File.ReadAllText(configurationSettings.ConfigurationPath +
                                         GlobalParameters.jsonModelMetadataFileName));
                DataTable dt = ConvertToDataTable(jsonArray);
                dt.AcceptChanges();
                    //Make sure the changes are seen as committed, so that changes can be detected later on
                dt.Columns[0].ColumnName = "VERSION_ATTRIBUTE_HASH";
                dt.Columns[1].ColumnName = "VERSION_ID";
                dt.Columns[2].ColumnName = "TABLE_NAME";
                dt.Columns[3].ColumnName = "COLUMN_NAME";
                dt.Columns[4].ColumnName = "DATA_TYPE";
                dt.Columns[5].ColumnName = "CHARACTER_MAXIMUM_LENGTH";
                dt.Columns[6].ColumnName = "NUMERIC_PRECISION";
                dt.Columns[7].ColumnName = "ORDINAL_POSITION";
                dt.Columns[8].ColumnName = "PRIMARY_KEY_INDICATOR";
                dt.Columns[9].ColumnName = "MULTI_ACTIVE_INDICATOR";

                bindingSourceTableMetadata.DataSource = dt;

                if (jsonArray != null)
                {
                    // Set the column header names.
                    dataGridViewTableMetadata.DataSource = bindingSourceTableMetadata;
                    dataGridViewTableMetadata.ColumnHeadersVisible = true;
                    dataGridViewTableMetadata.Columns[0].Visible = false;
                    dataGridViewTableMetadata.Columns[1].Visible = false;

                    dataGridViewTableMetadata.Columns[0].HeaderText = "Hash Key"; //Key column
                    dataGridViewTableMetadata.Columns[1].HeaderText = "Version ID"; //Key column
                    dataGridViewTableMetadata.Columns[2].HeaderText = "Table Name"; //Key column
                    dataGridViewTableMetadata.Columns[3].HeaderText = "Column Name"; //Key column
                    dataGridViewTableMetadata.Columns[4].HeaderText = "Data Type";
                    dataGridViewTableMetadata.Columns[5].HeaderText = "Length";
                    dataGridViewTableMetadata.Columns[6].HeaderText = "Precision";
                    dataGridViewTableMetadata.Columns[7].HeaderText = "Position";
                    dataGridViewTableMetadata.Columns[8].HeaderText = "Primary Key";
                    dataGridViewTableMetadata.Columns[9].HeaderText = "Multi-Active";
                }

                richTextBoxInformation.AppendText("The file " + configurationSettings.ConfigurationPath +
                                                  GlobalParameters.jsonTableMappingFileName + " was loaded.");
            }
            GridAutoLayout();
        }

        //  private void PopulateAttributeGridWithVersion(int versionId)
        //{
        //    var configurationSettings = new ConfigurationSettings();

        //    // open latest version
        //    var connOmd = new SqlConnection { ConnectionString = configurationSettings.ConnectionStringOmd };

        //    var selectedVersion = versionId;

        //    try
        //    {
        //        connOmd.Open();
        //    }
        //    catch (Exception exception)
        //    {
        //        richTextBoxInformation.Text += exception.Message;
        //    }

        //    var sqlStatementForLatestVersion = new StringBuilder();

        //    sqlStatementForLatestVersion.AppendLine("SELECT ");
        //    sqlStatementForLatestVersion.AppendLine(" [TABLE_NAME],");
        //    sqlStatementForLatestVersion.AppendLine(" [COLUMN_NAME],");
        //    sqlStatementForLatestVersion.AppendLine(" [DATA_TYPE],");
        //    sqlStatementForLatestVersion.AppendLine(" [CHARACTER_MAXIMUM_LENGTH],");
        //    sqlStatementForLatestVersion.AppendLine(" [NUMERIC_PRECISION],");
        //    sqlStatementForLatestVersion.AppendLine(" [MULTI_ACTIVE_INDICATOR],");
        //    sqlStatementForLatestVersion.AppendLine(" [VERSION_ID]");
        //    sqlStatementForLatestVersion.AppendLine("FROM [MD_VERSION_ATTRIBUTE]");
        //    sqlStatementForLatestVersion.AppendLine("WHERE [MULTI_ACTIVE_INDICATOR]='Y' AND [VERSION_ID] = " + selectedVersion);

        //    var versionList = GetDataTable(ref connOmd, sqlStatementForLatestVersion.ToString());
        //    bindingSourceMultiActiveMetadata.DataSource = versionList;

        //    // Set the column header names.

        //    dataGridViewAttributeMetadata.DataSource = bindingSourceMultiActiveMetadata;
        //    dataGridViewAttributeMetadata.ColumnHeadersVisible = true;
        //    dataGridViewAttributeMetadata.Columns[6].Visible = false;

        //    dataGridViewAttributeMetadata.Columns[0].HeaderText = "Table Name";
        //    dataGridViewAttributeMetadata.Columns[1].HeaderText = "Column Name";
        //    dataGridViewAttributeMetadata.Columns[2].HeaderText = "Data Type";
        //    dataGridViewAttributeMetadata.Columns[3].HeaderText = "Length";
        //    dataGridViewAttributeMetadata.Columns[4].HeaderText = "Precision";
        //    dataGridViewAttributeMetadata.Columns[5].HeaderText = "Multi-Active";
        //    dataGridViewAttributeMetadata.Columns[6].HeaderText = "Version ID";

        //    GridAutoLayout();
        //}


        private void DataGridViewTableMetadataKeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.Modifiers == Keys.Control)
                {
                    switch (e.KeyCode)
                    {
                        case Keys.V:
                            PasteClipboardTableMetadata();
                            // MessageBox.Show("!");
                            break;
                    }
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Pasting into the data grid has failed", "Copy/Paste", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void PasteClipboardTableMetadata()
        {
            try
            {
                string s = Clipboard.GetText();
                string[] lines = s.Split('\n');

                int iRow = dataGridViewTableMetadata.CurrentCell.RowIndex;
                int iCol = dataGridViewTableMetadata.CurrentCell.ColumnIndex;
                DataGridViewCell oCell;
                if (iRow + lines.Length > dataGridViewTableMetadata.Rows.Count - 1)
                {
                    bool bFlag = false;
                    foreach (string sEmpty in lines)
                    {
                        if (sEmpty == "")
                        {
                            bFlag = true;
                        }
                    }

                    int iNewRows = iRow + lines.Length - dataGridViewTableMetadata.Rows.Count;
                    if (iNewRows > 0)
                    {
                        if (bFlag)
                            dataGridViewTableMetadata.Rows.Add(iNewRows);
                        else
                            dataGridViewTableMetadata.Rows.Add(iNewRows + 1);
                    }
                    else
                        dataGridViewTableMetadata.Rows.Add(iNewRows + 1);
                }
                foreach (string line in lines)
                {
                    if (iRow < dataGridViewTableMetadata.RowCount && line.Length > 0)
                    {
                        string[] sCells = line.Split('\t');
                        for (int i = 0; i < sCells.GetLength(0); ++i)
                        {
                            if (iCol + i < dataGridViewTableMetadata.ColumnCount)
                            {
                                oCell = dataGridViewTableMetadata[iCol + i, iRow];
                                oCell.Value = Convert.ChangeType(sCells[i].Replace("\r", ""), oCell.ValueType);
                            }
                            else
                            {
                                break;
                            }
                        }
                        iRow++;
                    }
                    else
                    {
                        break;
                    }
                }
                //Clipboard.Clear();
            }
            catch (FormatException)
            {
                MessageBox.Show("There is an issue with the data formate for this cell!");
            }
        }
        private void GridAutoLayout()
        {
            //Table metadata
            //Set the autosize based on all cells for each column
            for (var i = 0; i < dataGridViewTableMetadata.Columns.Count - 1; i++)
            {
                dataGridViewTableMetadata.Columns[i].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            }
            if (dataGridViewTableMetadata.Columns.Count > 0)
            {
                dataGridViewTableMetadata.Columns[dataGridViewTableMetadata.Columns.Count - 1].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            }
            // Disable the auto size again (to enable manual resizing)
            for (var i = 0; i < dataGridViewTableMetadata.Columns.Count - 1; i++)
            {
                int columnWidth = dataGridViewTableMetadata.Columns[i].Width;
                dataGridViewTableMetadata.Columns[i].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                dataGridViewTableMetadata.Columns[i].Width = columnWidth;
            }

            //Attribute metadata
            //Set the autosize based on all cells for each column
            //for (var i = 0; i < dataGridViewAttributeMetadata.Columns.Count - 1; i++)
            //{
            //    dataGridViewAttributeMetadata.Columns[i].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            //}
            //if (dataGridViewAttributeMetadata.Columns.Count > 0)
            //{
            //    dataGridViewAttributeMetadata.Columns[dataGridViewAttributeMetadata.Columns.Count - 1].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            //}
            //// Disable the auto size again (to enable manual resizing)
            //for (var i = 0; i < dataGridViewAttributeMetadata.Columns.Count - 1; i++)
            //{
            //    int columnWidth = dataGridViewAttributeMetadata.Columns[i].Width;
            //    dataGridViewAttributeMetadata.Columns[i].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            //    dataGridViewAttributeMetadata.Columns[i].Width = columnWidth;
            //}

        }
    
        private void InitialiseVersionTrackbar()
        {
            var configurationSettings = new ConfigurationSettings();

            //Initialise the versioning
            var connOmd = new SqlConnection { ConnectionString = configurationSettings.ConnectionStringOmd };
            var selectedVersion = GetMaxVersionId(connOmd);

            trackBarVersioning.Maximum = selectedVersion;
            trackBarVersioning.TickFrequency = GetVersionCount();
            trackBarVersioning.Value = selectedVersion;

            var versionMajorMinor = GetVersion(selectedVersion, connOmd);
            var majorVersion = versionMajorMinor.Key;
            var minorVersion = versionMajorMinor.Value;

            labelVersion.Text = majorVersion + "." + minorVersion;

            richTextBoxInformation.Text += "The metadata for version " + majorVersion + "." + minorVersion + " has been loaded.";
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void buttonReverseEngineer_Click(object sender, EventArgs e)
        {
            var configurationSettings = new ConfigurationSettings();

            richTextBoxInformation.Clear();
            richTextBoxInformation.Text += "Commencing reverse-engineering the model metadata from the database.\r\n";
  
            // Truncate existing metadata - if selected
            if (checkBoxClearMetadata.Checked)
            {
                TruncateMetadata();
            }

            //Populate table / attribute version table
            var intDatabase = configurationSettings.IntegrationDatabaseName;
            var stgDatabase = configurationSettings.StagingDatabaseName;

            var connStg= new SqlConnection { ConnectionString = configurationSettings.ConnectionStringStg };
            var connInt = new SqlConnection {ConnectionString = configurationSettings.ConnectionStringInt };
            var stgPrefix = configurationSettings.StgTablePrefixValue;

            // Process changes
            if (radioButtonStagingLayer.Checked)
            {
                ReverseEngineerModelMetadata(connStg, stgPrefix, stgDatabase);
            }
            else
            {
                ReverseEngineerModelMetadata(connInt, @"", intDatabase);
            }
        }

        private void ManageTableMappingVersion()
        {
            var configurationSettings = new ConfigurationSettings();

            //This method makes sure the generation metadata (MD_TABLE_MAPPING) keeps up to date
            var connOmd = new SqlConnection { ConnectionString = configurationSettings.ConnectionStringOmd };

            try
            {
                connOmd.Open();
            }
            catch (Exception exception)
            {
                richTextBoxInformation.Text += "An error has occurred synchronising the model and metadata versions: " + exception.Message + ".\r\n";
            }

            //Retrieve the version key after version handling
            var versionId = GetMaxVersionId(connOmd);
            var previousVersionId = trackBarVersioning.Value;

            //Create the attribute selection statement for the array
            var sqlStatementForTableMappingVersion = new StringBuilder();

            sqlStatementForTableMappingVersion.AppendLine("SELECT [STAGING_AREA_TABLE],[BUSINESS_KEY_ATTRIBUTE],[INTEGRATION_AREA_TABLE],[FILTER_CRITERIA]");
            sqlStatementForTableMappingVersion.AppendLine("FROM MD_TABLE_MAPPING");
            sqlStatementForTableMappingVersion.AppendLine("WHERE VERSION_ID = " + previousVersionId + "");

            var versionTableMapping = GetDataTable(ref connOmd, sqlStatementForTableMappingVersion.ToString());

            //Create insert statement
            var insertQueryTables = new StringBuilder();

            foreach (DataRow version in versionTableMapping.Rows)
            {
                insertQueryTables.AppendLine("INSERT INTO MD_TABLE_MAPPING");
                insertQueryTables.AppendLine("([VERSION_ID],[STAGING_AREA_TABLE],[BUSINESS_KEY_ATTRIBUTE],[INTEGRATION_AREA_TABLE],[FILTER_CRITERIA])");
                insertQueryTables.AppendLine("VALUES");
                var businessKeyDefinition = version["BUSINESS_KEY_ATTRIBUTE"].ToString().Replace("'", "''");
                insertQueryTables.AppendLine("(" + versionId + "," + 
                                            "'" + (string)version["STAGING_AREA_TABLE"] + "'," +
                                            "'" + businessKeyDefinition + "'," +
                                            "'" + (string)version["INTEGRATION_AREA_TABLE"] + "'," + 
                                            "'" + (string)version["FILTER_CRITERIA"] + "'" +
                                            ")");
            }

            //Execute the insert statement
            if (versionTableMapping.Rows.Count > 0)
            {
                using (var connection = new SqlConnection(configurationSettings.ConnectionStringOmd))
                {
                    var command = new SqlCommand(insertQueryTables.ToString(), connection);

                    try
                    {
                        connection.Open();
                        command.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        richTextBoxInformation.Text += "An issue has occurred: " + ex;
                    }
                }
            }
        }

        private void ManageAttributeMappingVersion()
        {
            var configurationSettings = new ConfigurationSettings();

            //This method makes sure the generation metadata (MD_ATTRIBUTE_MAPPING) keeps up to date
            var connOmd = new SqlConnection { ConnectionString = configurationSettings.ConnectionStringOmd };
            try
            {
                connOmd.Open();
            }
            catch (Exception exception)
            {
                richTextBoxInformation.Text += "An error has occurred synchronising the model and metadata versions: " + exception.Message + ".\r\n";
            }

            //Retrieve the version key after version handling
            var versionId = GetMaxVersionId(connOmd);
            var previousVersionId = trackBarVersioning.Value;

            //Create the attribute selection statement for the array
            var sqlStatementForAttributeMappingVersion = new StringBuilder();

            sqlStatementForAttributeMappingVersion.AppendLine("SELECT [SOURCE_TABLE],[SOURCE_COLUMN],[TARGET_TABLE],[TARGET_COLUMN],[TRANSFORMATION_RULE]");
            sqlStatementForAttributeMappingVersion.AppendLine("FROM MD_ATTRIBUTE_MAPPING");
            sqlStatementForAttributeMappingVersion.AppendLine("WHERE VERSION_ID = " + previousVersionId + "");

            var versionTableMapping = GetDataTable(ref connOmd, sqlStatementForAttributeMappingVersion.ToString());

            //Create insert statement
            var insertQueryTables = new StringBuilder();

            foreach (DataRow version in versionTableMapping.Rows)
            {
                insertQueryTables.AppendLine("INSERT INTO MD_ATTRIBUTE_MAPPING");
                insertQueryTables.AppendLine("([VERSION_ID],[SOURCE_TABLE],[SOURCE_COLUMN],[TARGET_TABLE],[TARGET_COLUMN],[TRANSFORMATION_RULE])");
                insertQueryTables.AppendLine("VALUES");
                insertQueryTables.AppendLine("(" + versionId + "," +
                                            "'" + (string)version["SOURCE_TABLE"] + "'," +
                                            "'" + (string)version["SOURCE_COLUMN"] + "'," +
                                            "'" + (string)version["TARGET_TABLE"] + "'," +
                                            "'" + (string)version["TARGET_COLUMN"] + "'," +
                                            "'" + (string)version["TRANSFORMATION_RULE"] + "'" +
                                            ")");
            }

            //Execute the insert statement
            if (versionTableMapping.Rows.Count > 0)
            {
                using (var connection = new SqlConnection(configurationSettings.ConnectionStringOmd))
                {
                    var command = new SqlCommand(insertQueryTables.ToString(), connection);

                    try
                    {
                        connection.Open();
                        command.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        richTextBoxInformation.Text += "An issue has occurred: " + ex+"\r\n";
                    }
                }
            }
        }        

        private void ReverseEngineerModelMetadata(SqlConnection conn, string prefix, string databaseName)
        {
            var configurationSettings = new ConfigurationSettings();

            // This method is called when the reverse-engineer button is clicked.
            try
            {
                conn.Open();
            }
            catch (Exception exception)
            {
                richTextBoxInformation.Text += "An error has occurred uploading the model for the new version. The error message is: " + exception.Message + ".\r\n";
            }

            //Retrieve the version key after version handling
            var connOmd = new SqlConnection { ConnectionString = configurationSettings.ConnectionStringOmd };
            var versionId = GetMaxVersionId(connOmd);

            // Get everything as local variables to reduce multithreading issues
            var effectiveDateTimeAttribute = configurationSettings.EnableAlternativeSatelliteLoadDateTimeAttribute=="True" ? configurationSettings.AlternativeSatelliteLoadDateTimeAttribute : configurationSettings.LoadDateTimeAttribute;

            var dwhKeyIdentifier = configurationSettings.DwhKeyIdentifier; //Indicates _HSH, _SK etc.

            var keyIdentifierLocation = configurationSettings.KeyNamingLocation;
           // ReverseEngineerMainDataGrid(conn, prefix, databaseName, versionId, effectiveDateTimeAttribute, dwhKeyIdentifier, keyIdentifierLocation);

            //Create the attribute selection statement for the array
            var sqlStatementForAttributeVersion = new StringBuilder();

            sqlStatementForAttributeVersion.AppendLine("SELECT ");
            sqlStatementForAttributeVersion.AppendLine("  main.TABLE_NAME, ");
            sqlStatementForAttributeVersion.AppendLine("  main.COLUMN_NAME, ");
            sqlStatementForAttributeVersion.AppendLine("  main.DATA_TYPE, ");
            sqlStatementForAttributeVersion.AppendLine("  CAST(COALESCE(main.CHARACTER_MAXIMUM_LENGTH,0) AS VARCHAR(100)) AS CHARACTER_MAXIMUM_LENGTH,");
            sqlStatementForAttributeVersion.AppendLine("  CAST(COALESCE(main.NUMERIC_PRECISION,0) AS VARCHAR(100)) AS NUMERIC_PRECISION, ");
            sqlStatementForAttributeVersion.AppendLine("  CAST(main.ORDINAL_POSITION AS VARCHAR(100)) AS ORDINAL_POSITION, ");

            sqlStatementForAttributeVersion.AppendLine("  CASE ");
            sqlStatementForAttributeVersion.AppendLine("    WHEN keysub.COLUMN_NAME IS NULL ");
            sqlStatementForAttributeVersion.AppendLine("    THEN 'N' ");
            sqlStatementForAttributeVersion.AppendLine("    ELSE 'Y' ");
            sqlStatementForAttributeVersion.AppendLine("  END AS PRIMARY_KEY_INDICATOR, ");

            sqlStatementForAttributeVersion.AppendLine("  CASE ");
            sqlStatementForAttributeVersion.AppendLine("    WHEN ma.COLUMN_NAME IS NULL ");
            sqlStatementForAttributeVersion.AppendLine("    THEN 'N' ");
            sqlStatementForAttributeVersion.AppendLine("    ELSE 'Y' ");
            sqlStatementForAttributeVersion.AppendLine("  END AS MULTI_ACTIVE_INDICATOR, ");

            sqlStatementForAttributeVersion.AppendLine("  " + versionId + " AS VERSION_ID");

            sqlStatementForAttributeVersion.AppendLine("FROM [" + databaseName + "].INFORMATION_SCHEMA.COLUMNS main");
            sqlStatementForAttributeVersion.AppendLine("-- Primary Key");
            sqlStatementForAttributeVersion.AppendLine("LEFT OUTER JOIN (");
            sqlStatementForAttributeVersion.AppendLine("	SELECT ");
            sqlStatementForAttributeVersion.AppendLine("		sc.name AS TABLE_NAME,");
            sqlStatementForAttributeVersion.AppendLine("		C.name AS COLUMN_NAME");
            sqlStatementForAttributeVersion.AppendLine("	FROM [" + databaseName + "].sys.index_columns A");
            sqlStatementForAttributeVersion.AppendLine("	JOIN [" + databaseName + "].sys.indexes B");
            sqlStatementForAttributeVersion.AppendLine("	ON A.object_id=B.object_id AND A.index_id=B.index_id");
            sqlStatementForAttributeVersion.AppendLine("	JOIN [" + databaseName + "].sys.columns C");
            sqlStatementForAttributeVersion.AppendLine("	ON A.column_id=C.column_id AND A.object_id=C.object_id");
            sqlStatementForAttributeVersion.AppendLine("	JOIN [" + databaseName + "].sys.tables sc on sc.object_id = A.object_id");
            sqlStatementForAttributeVersion.AppendLine("	WHERE is_primary_key=1) keysub");
            sqlStatementForAttributeVersion.AppendLine("	ON main.TABLE_NAME = keysub.TABLE_NAME");
            sqlStatementForAttributeVersion.AppendLine("	AND main.COLUMN_NAME = keysub.COLUMN_NAME");
            //sqlStatementForAttributeVersion.AppendLine("-- Driving Key");
            //sqlStatementForAttributeVersion.AppendLine("LEFT OUTER JOIN (");
            //sqlStatementForAttributeVersion.AppendLine("		SELECT");
            //sqlStatementForAttributeVersion.AppendLine("		 st.name LINK_TABLE_NAME,");
            //sqlStatementForAttributeVersion.AppendLine("		 sc.name HASH_KEY_NAME,");
            //sqlStatementForAttributeVersion.AppendLine("		 sep.value [Value]");
            //sqlStatementForAttributeVersion.AppendLine("		 FROM [" + databaseName + "].sys.tables st");
            //sqlStatementForAttributeVersion.AppendLine("		 INNER JOIN [" + databaseName + "].sys.columns sc on st.object_id = sc.object_id");
            //sqlStatementForAttributeVersion.AppendLine("		 LEFT JOIN [" + databaseName + "].sys.extended_properties sep on st.object_id = sep.major_id");
            //sqlStatementForAttributeVersion.AppendLine("		 AND sc.column_id = sep.minor_id");
            //sqlStatementForAttributeVersion.AppendLine("		 AND sep.name = 'Driving_Key_Indicator'");
            //sqlStatementForAttributeVersion.AppendLine("	) extprop");
            //sqlStatementForAttributeVersion.AppendLine("	ON main.TABLE_NAME=extprop.LINK_TABLE_NAME");
            //sqlStatementForAttributeVersion.AppendLine("	AND main.COLUMN_NAME=extprop.HASH_KEY_NAME");

            //Multi-active
            sqlStatementForAttributeVersion.AppendLine("-- Multi-Active");
            sqlStatementForAttributeVersion.AppendLine("LEFT OUTER JOIN (");
            sqlStatementForAttributeVersion.AppendLine("	SELECT ");
            sqlStatementForAttributeVersion.AppendLine("		sc.name AS TABLE_NAME,");
            sqlStatementForAttributeVersion.AppendLine("		C.name AS COLUMN_NAME");
            sqlStatementForAttributeVersion.AppendLine("	FROM [" + databaseName + "].sys.index_columns A");
            sqlStatementForAttributeVersion.AppendLine("	JOIN [" + databaseName + "].sys.indexes B");
            sqlStatementForAttributeVersion.AppendLine("	ON A.object_id=B.object_id AND A.index_id=B.index_id");
            sqlStatementForAttributeVersion.AppendLine("	JOIN [" + databaseName + "].sys.columns C");
            sqlStatementForAttributeVersion.AppendLine("	ON A.column_id=C.column_id AND A.object_id=C.object_id");
            sqlStatementForAttributeVersion.AppendLine("	JOIN [" + databaseName + "].sys.tables sc on sc.object_id = A.object_id");
            sqlStatementForAttributeVersion.AppendLine("	WHERE is_primary_key=1");
            sqlStatementForAttributeVersion.AppendLine("	AND C.name NOT IN('" + effectiveDateTimeAttribute + "')");
            if (keyIdentifierLocation == "Prefix")
            {
                sqlStatementForAttributeVersion.AppendLine("	AND C.name NOT LIKE '" + dwhKeyIdentifier + "_%'");
            }
            else
            {
                sqlStatementForAttributeVersion.AppendLine("	AND C.name NOT LIKE '%_" + dwhKeyIdentifier + "'");
            }

            sqlStatementForAttributeVersion.AppendLine("	) ma");
            sqlStatementForAttributeVersion.AppendLine("	ON main.TABLE_NAME = ma.TABLE_NAME");
            sqlStatementForAttributeVersion.AppendLine("	AND main.COLUMN_NAME = ma.COLUMN_NAME");

            sqlStatementForAttributeVersion.AppendLine("WHERE main.TABLE_NAME LIKE '" + prefix + "_%'");
            sqlStatementForAttributeVersion.AppendLine("ORDER BY main.ORDINAL_POSITION");

            var reverseEngineerResults = GetDataTable(ref conn, sqlStatementForAttributeVersion.ToString());

            bindingSourceTableMetadata.DataSource = reverseEngineerResults;

            foreach (DataRow row in reverseEngineerResults.Rows) //Flag as new row so it's detected by the save button
            {
                row.SetAdded();
            }

    }


        private void TruncateMetadata()
        {
            var configurationSettings = new ConfigurationSettings();

            //Truncate tables
            const string commandText = //"TRUNCATE TABLE [MD_TABLE_MAPPING]; " +
                                       //"TRUNCATE TABLE [MD_ATTRIBUTE_MAPPING]; " +
                                       "TRUNCATE TABLE [MD_VERSION_ATTRIBUTE];";
                                       //"TRUNCATE TABLE [MD_VERSION];";

            using (var connection = new SqlConnection(configurationSettings.ConnectionStringOmd))
            {
                var command = new SqlCommand(commandText, connection);

                try
                {
                    connection.Open();
                    command.ExecuteNonQuery();
                    richTextBoxInformation.Text += "The metadata tables have been truncated.\r\n";
                }
                catch (Exception ex)
                {
                    richTextBoxInformation.Text += "An issue has occurred: " + ex;
                }
            }
        }

        private void SaveVersion(int majorVersion, int minorVersion)
        {
            var configurationSettings = new ConfigurationSettings();

            //Insert or create version
            var insertStatement = new StringBuilder();

            insertStatement.AppendLine("INSERT INTO [dbo].[MD_VERSION] ");
            insertStatement.AppendLine("([VERSION_NAME],[VERSION_NOTES],[MAJOR_RELEASE_NUMBER],[MINOR_RELEASE_NUMBER])");
            insertStatement.AppendLine("VALUES ");
            insertStatement.AppendLine("('N/A', 'N/A', " + majorVersion + "," + minorVersion + ")");

            using (var connectionVersion = new SqlConnection(configurationSettings.ConnectionStringOmd))
            {
                var commandVersion = new SqlCommand(insertStatement.ToString(), connectionVersion);

                try
                {
                    connectionVersion.Open();
                    commandVersion.ExecuteNonQuery();
                    richTextBoxInformation.Text += "A version (" + majorVersion + "." + minorVersion +
                                                    ") was created.\r\n";
                }
                catch (Exception ex)
                {
                    richTextBoxInformation.Text += "An issue has occurred: " + ex;
                }
            }
        }

        private void trackBarVersioning_ValueChanged(object sender, EventArgs e)
        {
            var configurationSettings = new ConfigurationSettings();

            var connOmd = new SqlConnection { ConnectionString = configurationSettings.ConnectionStringOmd };
            PopulateTableGridWithVersion(trackBarVersioning.Value);
            var versionMajorMinor = GetVersion(trackBarVersioning.Value, connOmd);
            var majorVersion = versionMajorMinor.Key;
            var minorVersion = versionMajorMinor.Value;

            labelVersion.Text = majorVersion + "." + minorVersion;

            richTextBoxInformation.Text = "The metadata for version " + majorVersion + "." + minorVersion + " has been prepared.\r\n";
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxClearMetadata.Checked)
            {
               MessageBox.Show("Selection this option will mean that all metadata will be truncated.", "Clear metadata", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        # region Background worker
        private void buttonStart_Click(object sender, EventArgs e)
        {
            var configurationSettings = new ConfigurationSettings();

            var connOmd = new SqlConnection { ConnectionString = configurationSettings.ConnectionStringOmd };

            richTextBoxInformation.Clear();
            var versionMajorMinor = GetVersion(trackBarVersioning.Value, connOmd);
            var majorVersion = versionMajorMinor.Key;
            var minorVersion = versionMajorMinor.Value;
            richTextBoxInformation.Text += "Commencing preparation / activation for version " + majorVersion + "." +
                               minorVersion + ".\r\n";


            if (checkBoxIgnoreVersion.Checked == false)
            {
                var versionExistenceCheck = new StringBuilder();

                versionExistenceCheck.AppendLine("SELECT * FROM MD_VERSION_ATTRIBUTE WHERE VERSION_ID = " + trackBarVersioning.Value);

                var versionExistenceCheckDataTable = GetDataTable(ref connOmd, versionExistenceCheck.ToString());

                if (versionExistenceCheckDataTable != null && versionExistenceCheckDataTable.Rows.Count > 0)
                {
                    if (backgroundWorker1.IsBusy) return;
                    // create a new instance of the alert form
                    _alert = new FormAlert();
                    // event handler for the Cancel button in AlertForm
                    _alert.Canceled += buttonCancel_Click;
                    _alert.Show();
                    // Start the asynchronous operation.
                    backgroundWorker1.RunWorkerAsync();
                }
                else
                {
                    richTextBoxInformation.Text +=
                        "There is no model metadata available for this version, so the metadata can only be actived with the 'Ignore Version' enabled for this specific version.\r\n ";
                }
            }
            else
            {
                if (backgroundWorker1.IsBusy) return;
                // create a new instance of the alert form
                _alert = new FormAlert();
                // event handler for the Cancel button in AlertForm
                _alert.Canceled += buttonCancel_Click;
                _alert.Show();
                // Start the asynchronous operation.
                backgroundWorker1.RunWorkerAsync();
            }



        }

        // This event handler cancels the backgroundworker, fired from Cancel button in AlertForm.
        private void buttonCancel_Click(object sender, EventArgs e)
        {
            if (backgroundWorker1.WorkerSupportsCancellation)
            {
                // Cancel the asynchronous operation.
                backgroundWorker1.CancelAsync();
                // Close the AlertForm
                _alert.Close();
            }
        }

        // Multithreading for updating the user (Link Satellite form)
        delegate int GetVersionFromTrackBarCallBack();
        private int GetVersionFromTrackBar()
        {
            if (trackBarVersioning.InvokeRequired)
            {
                var d = new GetVersionFromTrackBarCallBack(GetVersionFromTrackBar);
                return Int32.Parse(Invoke(d).ToString());
            }
            else
            {
                return trackBarVersioning.Value;
            }
        }

        // This event handler deals with the results of the background operation.
        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                labelResult.Text = "Cancelled!";
            }
            else if (e.Error != null)
            {
                labelResult.Text = "Error: " + e.Error.Message;
            }
            else
            {
                labelResult.Text = "Done!";
                richTextBoxInformation.Text += "The metadata was processed succesfully!\r\n";
            }
            // Close the AlertForm
            //alert.Close();
        }

        // This event handler updates the progress.
        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            // Show the progress in main form (GUI)
            labelResult.Text = (e.ProgressPercentage + "%");

            // Pass the progress to AlertForm label and progressbar
            _alert.Message = "In progress, please wait... " + e.ProgressPercentage + "%";
            _alert.ProgressValue = e.ProgressPercentage;

            // Manage the logging
        }

        # endregion

        // This event handler is where the time-consuming work is done.
        private void backgroundWorker_DoWorkMetadataActivation(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            var configurationSettings = new ConfigurationSettings();

            var errorLog = new StringBuilder();
            var errorCounter = new int();

            var connOmd = new SqlConnection { ConnectionString = configurationSettings.ConnectionStringOmd };
            var metaDataConnection = configurationSettings.ConnectionStringOmd;

            // Get everything as local variables to reduce multithreading issues
            var stagingDatabase = '[' + configurationSettings.StagingDatabaseName + ']';
            var integrationDatabase = '[' + configurationSettings.IntegrationDatabaseName + ']';

            var linkedServer = configurationSettings.LinkedServer;
            if (linkedServer != "")
            {
                linkedServer = '[' + linkedServer + "].";
            }

            var effectiveDateTimeAttribute = configurationSettings.EnableAlternativeSatelliteLoadDateTimeAttribute=="True" ? configurationSettings.AlternativeSatelliteLoadDateTimeAttribute : configurationSettings.LoadDateTimeAttribute;
            var currentRecordAttribute = configurationSettings.CurrentRowAttribute;
            var eventDateTimeAtttribute = configurationSettings.EventDateTimeAttribute;
            var recordSource = configurationSettings.RecordSourceAttribute;
            var alternativeRecordSource = configurationSettings.AlternativeRecordSourceAttribute;
            var sourceRowId = configurationSettings.RowIdAttribute;
            var recordChecksum = configurationSettings.RecordChecksumAttribute;
            var changeDataCaptureIndicator = configurationSettings.ChangeDataCaptureAttribute;
            var hubAlternativeLdts = configurationSettings.AlternativeLoadDateTimeAttribute;
            var etlProcessId = configurationSettings.EtlProcessAttribute;
            var loadDateTimeStamp = configurationSettings.LoadDateTimeAttribute;

            var stagingPrefix = configurationSettings.StgTablePrefixValue;
            var hubTablePrefix = configurationSettings.HubTablePrefixValue;
            var lnkTablePrefix = configurationSettings.LinkTablePrefixValue;
            var satTablePrefix = configurationSettings.SatTablePrefixValue;
            var lsatTablePrefix = configurationSettings.LsatPrefixValue;

            if (configurationSettings.TableNamingLocation=="Prefix")
            {
                stagingPrefix = stagingPrefix + '%';
                hubTablePrefix = hubTablePrefix + '%';
                lnkTablePrefix = lnkTablePrefix + '%';
                satTablePrefix = satTablePrefix + '%';
                lsatTablePrefix = lsatTablePrefix + '%';
            }
            else
            {
                stagingPrefix = '%' + stagingPrefix;
                hubTablePrefix = '%' + hubTablePrefix;
                lnkTablePrefix = '%' + lnkTablePrefix;
                satTablePrefix = '%' + satTablePrefix;
                lsatTablePrefix = '%' + lsatTablePrefix;
            }

            var dwhKeyIdentifier = configurationSettings.DwhKeyIdentifier;

            if (configurationSettings.KeyNamingLocation=="Prefix")
            {
                dwhKeyIdentifier = dwhKeyIdentifier + '%';
            }
            else
            {
                dwhKeyIdentifier = '%' + dwhKeyIdentifier;
            }

            // Handling multithreading
            if (worker != null && worker.CancellationPending)
            {
                e.Cancel = true;
            }
            else
            {
                // Determine the version
                var versionId = GetVersionFromTrackBar();

                var versionMajorMinor = GetVersion(versionId, connOmd);
                var majorVersion = versionMajorMinor.Key;
                var minorVersion = versionMajorMinor.Value;

                _alert.SetTextLogging("Commencing metadata preparation / activation for version " + majorVersion + "." + minorVersion + ".\r\n\r\n");

                // Alerting the user what kind of metadata is prepared
                _alert.SetTextLogging(checkBoxIgnoreVersion.Checked
                    ? "The 'ignore model version' option is selected. This means when possible the live database (tables and attributes) will be used in conjunction with the Data Vault metadata. In other words, the model versioning is ignored.\r\n\r\n"
                    : "Metadata is prepared using the selected version for both the Data Vault metadata as well as the model metadata.\r\n\r\n");

                # region Delete Metadata - 5%
                // 1. Deleting metadata
                _alert.SetTextLogging("Commencing removal of existing metadata.\r\n");
                var deleteStatement = new StringBuilder();

                deleteStatement.AppendLine("DELETE FROM [MD_STG_SAT_ATT_XREF];");
                deleteStatement.AppendLine("DELETE FROM dbo.MD_STG_LINK_ATT_XREF;");
                deleteStatement.AppendLine("DELETE FROM dbo.MD_STG_SAT_ATT_XREF;");
                deleteStatement.AppendLine("DELETE FROM dbo.MD_STG_LINK_XREF;");
                deleteStatement.AppendLine("DELETE FROM dbo.MD_STG_SAT_XREF;");
                deleteStatement.AppendLine("DELETE FROM dbo.MD_DRIVING_KEY_XREF");
                deleteStatement.AppendLine("DELETE FROM dbo.MD_HUB_LINK_XREF;");
                deleteStatement.AppendLine("DELETE FROM dbo.MD_SAT;");
                deleteStatement.AppendLine("DELETE FROM dbo.MD_BUSINESS_KEY_COMPONENT_PART;");
                deleteStatement.AppendLine("DELETE FROM dbo.MD_BUSINESS_KEY_COMPONENT;");
                deleteStatement.AppendLine("DELETE FROM dbo.MD_STG_HUB_XREF;");
                deleteStatement.AppendLine("DELETE FROM dbo.MD_ATT;");
                deleteStatement.AppendLine("DELETE FROM dbo.MD_STG;");
                deleteStatement.AppendLine("DELETE FROM dbo.MD_HUB;");
                deleteStatement.AppendLine("DELETE FROM dbo.MD_LINK;");

                using (var connectionVersion = new SqlConnection(metaDataConnection))
                {
                    var commandVersion = new SqlCommand(deleteStatement.ToString(), connectionVersion);

                    try
                    {
                        connectionVersion.Open();
                        commandVersion.ExecuteNonQuery();

                        if (worker != null) worker.ReportProgress(5);
                        _alert.SetTextLogging("Removal of existing metadata completed.\r\n");
                    }
                    catch (Exception ex)
                    {
                        errorCounter++;
                        _alert.SetTextLogging("An issue has occured during removal of old metadata. Please check the Error Log for more details.\r\n");
                        errorLog.AppendLine("\r\nAn issue has occured during removal of old metadata: \r\n\r\n" + ex);
                    }
                }
                # endregion



                # region Prepare Staging Area - 10%
                // 2. Prepare STG
                _alert.SetTextLogging("\r\n");
                _alert.SetTextLogging("Commencing preparing the Staging Area metadata.\r\n");

                try
                {
                    var prepareStgStatement = new StringBuilder();
                    var stgCounter = 1;

                    prepareStgStatement.AppendLine("SELECT DISTINCT STAGING_AREA_TABLE");
                    prepareStgStatement.AppendLine("FROM MD_TABLE_MAPPING");
                    prepareStgStatement.AppendLine("WHERE STAGING_AREA_TABLE LIKE '" + stagingPrefix + "'");
                    prepareStgStatement.AppendLine("AND [VERSION_ID] = " + versionId);
                    prepareStgStatement.AppendLine("AND [GENERATE_INDICATOR] = 'Y'");
                    prepareStgStatement.AppendLine("ORDER BY STAGING_AREA_TABLE");

                    var listStaging = GetDataTable(ref connOmd, prepareStgStatement.ToString());

                    foreach (DataRow tableName in listStaging.Rows)
                    {
                        using (var connection = new SqlConnection(metaDataConnection))
                        {
                            _alert.SetTextLogging("--> " + tableName["STAGING_AREA_TABLE"] + "\r\n");

                            var insertStgStatemeent = new StringBuilder();

                            insertStgStatemeent.AppendLine("INSERT INTO [MD_STG]");
                            insertStgStatemeent.AppendLine("([STAGING_AREA_TABLE_NAME],[STAGING_AREA_TABLE_ID])");
                            insertStgStatemeent.AppendLine("VALUES ('" + tableName["STAGING_AREA_TABLE"] + "'," + stgCounter + ")");

                            var command = new SqlCommand(insertStgStatemeent.ToString(), connection);

                            try
                            {
                                connection.Open();
                                command.ExecuteNonQuery();
                                stgCounter++;
                            }
                            catch (Exception ex)
                            {
                                errorCounter++;
                                _alert.SetTextLogging("An issue has occured during preparation of the Staging Area. Please check the Error Log for more details.\r\n");
                                errorLog.AppendLine("\r\nAn issue has occured during preparation of the Staging Area: \r\n\r\n" + ex);
                            }
                        }
                    }

                    if (worker != null) worker.ReportProgress(10);
                    _alert.SetTextLogging("Preparation of the Staging Area completed.\r\n");
                }
                catch (Exception ex)
                {
                    errorCounter++;
                    _alert.SetTextLogging("An issue has occured during preparation of the Staging Area. Please check the Error Log for more details.\r\n");
                    errorLog.AppendLine("\r\nAn issue has occured during preparation of the Staging Area: \r\n\r\n" + ex);
                }

                #endregion

                # region Prepare Hubs - 15%
                //3. Prepare Hubs
                _alert.SetTextLogging("\r\n");
                _alert.SetTextLogging("Commencing preparing the Hub metadata.\r\n");

                try
                {
                    var prepareHubStatement = new StringBuilder();
                    var hubCounter = 1;

                    prepareHubStatement.AppendLine("SELECT 'Not applicable' AS HUB_TABLE_NAME");
                    prepareHubStatement.AppendLine("UNION");
                    prepareHubStatement.AppendLine("SELECT DISTINCT INTEGRATION_AREA_TABLE AS HUB_TABLE_NAME");
                    prepareHubStatement.AppendLine("FROM MD_TABLE_MAPPING");
                    prepareHubStatement.AppendLine("WHERE INTEGRATION_AREA_TABLE LIKE '" + hubTablePrefix + "'");
                    prepareHubStatement.AppendLine("AND [VERSION_ID] = " + versionId);
                    prepareHubStatement.AppendLine("AND [GENERATE_INDICATOR] = 'Y'");

                    var listHub = GetDataTable(ref connOmd, prepareHubStatement.ToString());

                    foreach (DataRow tableName in listHub.Rows)
                    {
                        using (var connection = new SqlConnection(metaDataConnection))
                        {
                            _alert.SetTextLogging("--> " + tableName["HUB_TABLE_NAME"] + "\r\n");

                            var insertHubStatemeent = new StringBuilder();

                            insertHubStatemeent.AppendLine("INSERT INTO [MD_HUB]");
                            insertHubStatemeent.AppendLine("([HUB_TABLE_NAME],[HUB_TABLE_ID])");
                            insertHubStatemeent.AppendLine("VALUES ('" + tableName["HUB_TABLE_NAME"] + "'," + hubCounter + ")");

                            var command = new SqlCommand(insertHubStatemeent.ToString(), connection);

                            try
                            {
                                connection.Open();
                                command.ExecuteNonQuery();
                                hubCounter++;
                            }
                            catch (Exception ex)
                            {
                                errorCounter++;
                                _alert.SetTextLogging("An issue has occured during preparation of the Hubs. Please check the Error Log for more details.\r\n");
                                errorLog.AppendLine("\r\nAn issue has occured during preparation of the Hubs: \r\n\r\n" + ex);
                            }
                        }
                    }

                    if (worker != null) worker.ReportProgress(15);
                    _alert.SetTextLogging("Preparation of the Hub metadata completed.\r\n");
                }
                catch (Exception ex)
                {
                    errorCounter++;
                    _alert.SetTextLogging("An issue has occured during preparation of the Hubs. Please check the Error Log for more details.\r\n");
                    errorLog.AppendLine("\r\nAn issue has occured during preparation of the Hubs: \r\n\r\n" + ex);
                }
                #endregion



                #region Prepare Links - 20%
                //4. Prepare links
                _alert.SetTextLogging("\r\n");
                _alert.SetTextLogging("Commencing preparing the Link metadata.\r\n");

                try
                {
                    var prepareLinkStatement = new StringBuilder();
                    var linkCounter = 1;

                    prepareLinkStatement.AppendLine("SELECT 'Not applicable' AS LINK_TABLE_NAME");
                    prepareLinkStatement.AppendLine("UNION");
                    prepareLinkStatement.AppendLine("SELECT DISTINCT INTEGRATION_AREA_TABLE AS LINK_TABLE_NAME");
                    prepareLinkStatement.AppendLine("FROM MD_TABLE_MAPPING");
                    prepareLinkStatement.AppendLine("WHERE INTEGRATION_AREA_TABLE LIKE '" + lnkTablePrefix + "'");
                    prepareLinkStatement.AppendLine("AND [VERSION_ID] = " + versionId);
                    prepareLinkStatement.AppendLine("AND [GENERATE_INDICATOR] = 'Y'");

                    var listLink = GetDataTable(ref connOmd, prepareLinkStatement.ToString());

                    foreach (DataRow tableName in listLink.Rows)
                    {
                        using (var connection = new SqlConnection(metaDataConnection))
                        {
                            _alert.SetTextLogging("--> " + tableName["LINK_TABLE_NAME"] + "\r\n");

                            var insertLinkStatement = new StringBuilder();

                            insertLinkStatement.AppendLine("INSERT INTO [MD_LINK]");
                            insertLinkStatement.AppendLine("([LINK_TABLE_NAME],[LINK_TABLE_ID])");
                            insertLinkStatement.AppendLine("VALUES ('" + tableName["LINK_TABLE_NAME"] + "'," + linkCounter + ")");

                            var command = new SqlCommand(insertLinkStatement.ToString(), connection);

                            try
                            {
                                connection.Open();
                                command.ExecuteNonQuery();
                                linkCounter++;
                            }
                            catch (Exception ex)
                            {
                                errorCounter++;
                                _alert.SetTextLogging("An issue has occured during preparation of the Links. Please check the Error Log for more details.\r\n");
                                errorLog.AppendLine("\r\nAn issue has occured during preparation of the Links: \r\n\r\n" + ex);
                            }
                        }
                    }

                    if (worker != null) worker.ReportProgress(20);
                    _alert.SetTextLogging("Preparation of the Link metadata completed.\r\n");
                }
                catch (Exception ex)
                {
                    errorCounter++;
                    _alert.SetTextLogging("An issue has occured during preparation of the Links. Please check the Error Log for more details.\r\n");
                    errorLog.AppendLine("\r\nAn issue has occured during preparation of the Links: \r\n\r\n" + ex);
                }
                #endregion



                #region Prepare Satellites - 24%
                //5.1 Prepare Satellites
                _alert.SetTextLogging("\r\n");
                _alert.SetTextLogging("Commencing preparing the Satellite metadata.\r\n");
                var satCounter = 1;

                try
                {
                    var prepareSatStatement = new StringBuilder();

                    prepareSatStatement.AppendLine("SELECT DISTINCT");
                    prepareSatStatement.AppendLine("       spec.INTEGRATION_AREA_TABLE AS SATELLITE_TABLE_NAME,");
                    prepareSatStatement.AppendLine("       hubkeysub.HUB_TABLE_ID,");
                    prepareSatStatement.AppendLine("       'Normal' AS SATELLITE_TYPE,");
                    prepareSatStatement.AppendLine("       (SELECT LINK_TABLE_ID FROM MD_LINK WHERE LINK_TABLE_NAME='Not applicable') AS LINK_TABLE_ID -- No link for normal Satellites ");
                    prepareSatStatement.AppendLine("FROM MD_TABLE_MAPPING spec ");
                    prepareSatStatement.AppendLine("LEFT OUTER JOIN ");
                    prepareSatStatement.AppendLine("( ");
                    prepareSatStatement.AppendLine("       SELECT DISTINCT INTEGRATION_AREA_TABLE, hub.HUB_TABLE_ID, STAGING_AREA_TABLE, BUSINESS_KEY_ATTRIBUTE ");
                    prepareSatStatement.AppendLine("       FROM MD_TABLE_MAPPING spec2 ");
                    prepareSatStatement.AppendLine("       LEFT OUTER JOIN -- Join in the Hub ID from the MD table ");
                    prepareSatStatement.AppendLine("             MD_HUB hub ON hub.HUB_TABLE_NAME=spec2.INTEGRATION_AREA_TABLE ");
                    prepareSatStatement.AppendLine("    WHERE INTEGRATION_AREA_TABLE LIKE '" + hubTablePrefix + "'");
                    prepareSatStatement.AppendLine("    AND VERSION_ID = " + versionId);
                    prepareSatStatement.AppendLine("    AND [GENERATE_INDICATOR] = 'Y'");
                    prepareSatStatement.AppendLine(") hubkeysub ");
                    prepareSatStatement.AppendLine("       ON spec.STAGING_AREA_TABLE=hubkeysub.STAGING_AREA_TABLE ");
                    prepareSatStatement.AppendLine("       AND replace(spec.BUSINESS_KEY_ATTRIBUTE,' ','')=replace(hubkeysub.BUSINESS_KEY_ATTRIBUTE,' ','') ");
                    prepareSatStatement.AppendLine("WHERE spec.INTEGRATION_AREA_TABLE LIKE '" + satTablePrefix + "'");
                    prepareSatStatement.AppendLine("AND VERSION_ID = " + versionId);
                    prepareSatStatement.AppendLine("AND [GENERATE_INDICATOR] = 'Y'");

                    var listSat = GetDataTable(ref connOmd, prepareSatStatement.ToString());

                    foreach (DataRow tableName in listSat.Rows)
                    {
                        using (var connection = new SqlConnection(metaDataConnection))
                        {
                            _alert.SetTextLogging("--> " + tableName["SATELLITE_TABLE_NAME"] + "\r\n");

                            var insertSatStatement = new StringBuilder();

                            insertSatStatement.AppendLine("INSERT INTO [MD_SAT]");
                            insertSatStatement.AppendLine("([SATELLITE_TABLE_NAME],[SATELLITE_TABLE_ID], [SATELLITE_TYPE], [HUB_TABLE_ID], [LINK_TABLE_ID])");
                            insertSatStatement.AppendLine("VALUES ('" + tableName["SATELLITE_TABLE_NAME"] + "'," + satCounter + ",'" + tableName["SATELLITE_TYPE"] + "'," + tableName["HUB_TABLE_ID"] + "," + tableName["LINK_TABLE_ID"] + ")");

                            var command = new SqlCommand(insertSatStatement.ToString(), connection);

                            try
                            {
                                connection.Open();
                                command.ExecuteNonQuery();
                                satCounter++;
                            }
                            catch (Exception ex)
                            {
                                errorCounter++;
                                _alert.SetTextLogging("An issue has occured during preparation of the Satellites. Please check the Error Log for more details.\r\n");
                                errorLog.AppendLine("\r\nAn issue has occured during preparation of the Satellites: \r\n\r\n" + ex);
                            }
                        }
                    }

                    if (worker != null) worker.ReportProgress(24);
                    _alert.SetTextLogging("Preparation of the Satellite metadata completed.\r\n");
                }
                catch (Exception ex)
                {
                    errorCounter++;
                    _alert.SetTextLogging("An issue has occured during preparation of the Satellites. Please check the Error Log for more details.\r\n");
                    errorLog.AppendLine("\r\nAn issue has occured during preparation of the Satellites: \r\n\r\n" + ex);
                }
                #endregion


                #region Prepare Link Satellites - 28%
                //5.2 Prepare Satellites
                _alert.SetTextLogging("\r\n");
                _alert.SetTextLogging("Commencing preparing the Link Satellite metadata.\r\n");
                //satCounter = 1;

                try
                {
                    var prepareSatStatement = new StringBuilder();

                    prepareSatStatement.AppendLine("SELECT DISTINCT");
                    prepareSatStatement.AppendLine("       spec.INTEGRATION_AREA_TABLE AS SATELLITE_TABLE_NAME, ");
                    prepareSatStatement.AppendLine("       (SELECT HUB_TABLE_ID FROM MD_HUB WHERE HUB_TABLE_NAME='Not applicable') AS HUB_TABLE_ID, -- No Hub for Link Satellites");
                    prepareSatStatement.AppendLine("       'Link Satellite' AS SATELLITE_TYPE,");
                    prepareSatStatement.AppendLine("       lnkkeysub.LINK_TABLE_ID");
                    prepareSatStatement.AppendLine("FROM MD_TABLE_MAPPING spec");
                    prepareSatStatement.AppendLine("LEFT OUTER JOIN  -- Get the Link ID that belongs to this LSAT");
                    prepareSatStatement.AppendLine("(");
                    prepareSatStatement.AppendLine("       SELECT DISTINCT ");
                    prepareSatStatement.AppendLine("             INTEGRATION_AREA_TABLE AS LINK_TABLE_NAME,");
                    prepareSatStatement.AppendLine("             STAGING_AREA_TABLE,");
                    prepareSatStatement.AppendLine("             BUSINESS_KEY_ATTRIBUTE,");
                    prepareSatStatement.AppendLine("       lnk.LINK_TABLE_ID");
                    prepareSatStatement.AppendLine("       FROM MD_TABLE_MAPPING spec2");
                    prepareSatStatement.AppendLine("       LEFT OUTER JOIN -- Join in the Link ID from the MD table");
                    prepareSatStatement.AppendLine("             MD_LINK lnk ON lnk.LINK_TABLE_NAME=spec2.INTEGRATION_AREA_TABLE");
                    prepareSatStatement.AppendLine("       WHERE INTEGRATION_AREA_TABLE LIKE '" + lnkTablePrefix + "' ");
                    prepareSatStatement.AppendLine("       AND VERSION_ID = " + versionId);
                    prepareSatStatement.AppendLine("       AND [GENERATE_INDICATOR] = 'Y'");
                    prepareSatStatement.AppendLine(") lnkkeysub");
                    prepareSatStatement.AppendLine("    ON spec.STAGING_AREA_TABLE=lnkkeysub.STAGING_AREA_TABLE -- Only the combination of Link table and Business key can belong to the LSAT");
                    prepareSatStatement.AppendLine("   AND spec.BUSINESS_KEY_ATTRIBUTE=lnkkeysub.BUSINESS_KEY_ATTRIBUTE");
                    prepareSatStatement.AppendLine("");
                    prepareSatStatement.AppendLine("-- Only select Link Satellites as the base / driving table (spec alias)");
                    prepareSatStatement.AppendLine("WHERE spec.INTEGRATION_AREA_TABLE LIKE '" + lsatTablePrefix + "'");
                    prepareSatStatement.AppendLine("AND [GENERATE_INDICATOR] = 'Y'");
                    prepareSatStatement.AppendLine("AND VERSION_ID = " + versionId);

                    var listSat = GetDataTable(ref connOmd, prepareSatStatement.ToString());

                    foreach (DataRow tableName in listSat.Rows)
                    {
                        using (var connection = new SqlConnection(metaDataConnection))
                        {
                            _alert.SetTextLogging("--> " + tableName["SATELLITE_TABLE_NAME"] + "\r\n");

                            var insertSatStatement = new StringBuilder();

                            insertSatStatement.AppendLine("INSERT INTO [MD_SAT]");
                            insertSatStatement.AppendLine("([SATELLITE_TABLE_NAME],[SATELLITE_TABLE_ID], [SATELLITE_TYPE], [HUB_TABLE_ID], [LINK_TABLE_ID])");
                            insertSatStatement.AppendLine("VALUES ('" + tableName["SATELLITE_TABLE_NAME"] + "'," + satCounter + ",'" + tableName["SATELLITE_TYPE"] + "'," + tableName["HUB_TABLE_ID"] + "," + tableName["LINK_TABLE_ID"] + ")");

                            var command = new SqlCommand(insertSatStatement.ToString(), connection);

                            try
                            {
                                connection.Open();
                                command.ExecuteNonQuery();
                                satCounter++;
                            }
                            catch (Exception ex)
                            {
                                errorCounter++;
                                _alert.SetTextLogging("An issue has occured during preparation of the Link Satellites. Please check the Error Log for more details.\r\n");
                                errorLog.AppendLine("\r\nAn issue has occured during preparation of the Link Satellites: \r\n\r\n" + ex);
                            }
                        }
                    }

                    worker.ReportProgress(28);
                    _alert.SetTextLogging("Preparation of the Link Satellite metadata completed.\r\n");
                }
                catch (Exception ex)
                {
                    errorCounter++;
                    _alert.SetTextLogging("An issue has occured during preparation of the Link Satellites. Please check the Error Log for more details.\r\n");
                    errorLog.AppendLine("\r\nAn issue has occured during preparation of the Link Satellites: \r\n\r\n" + ex);
                }
                #endregion



                #region Prepare STG / SAT Xref - 28%
                //5.3 Prepare STG / Sat XREF
                _alert.SetTextLogging("\r\n");
                _alert.SetTextLogging("Commencing preparing the relationship between (Link) Satellites and the Staging Area tables.\r\n");
                satCounter = 1;

                try
                {
                    var prepareSatXrefStatement = new StringBuilder();

                    prepareSatXrefStatement.AppendLine("SELECT");
                    prepareSatXrefStatement.AppendLine("       sat.SATELLITE_TABLE_ID,");
                    prepareSatXrefStatement.AppendLine("	   sat.SATELLITE_TABLE_NAME,");
                    prepareSatXrefStatement.AppendLine("       stg.STAGING_AREA_TABLE_ID, ");
                    prepareSatXrefStatement.AppendLine("	   stg.STAGING_AREA_TABLE_NAME,");
                    prepareSatXrefStatement.AppendLine("	   spec.BUSINESS_KEY_ATTRIBUTE,");
                    prepareSatXrefStatement.AppendLine("       spec.FILTER_CRITERIA");
                    prepareSatXrefStatement.AppendLine("FROM MD_TABLE_MAPPING spec");
                    prepareSatXrefStatement.AppendLine("LEFT OUTER JOIN -- Join in the Staging_Area_ID from the MD_STG table");
                    prepareSatXrefStatement.AppendLine("       MD_STG stg ON stg.STAGING_AREA_TABLE_NAME=spec.STAGING_AREA_TABLE");
                    prepareSatXrefStatement.AppendLine("LEFT OUTER JOIN -- Join in the Satellite_ID from the MD_SAT table");
                    prepareSatXrefStatement.AppendLine("       MD_SAT sat ON sat.SATELLITE_TABLE_NAME=spec.INTEGRATION_AREA_TABLE");
                    prepareSatXrefStatement.AppendLine("WHERE spec.INTEGRATION_AREA_TABLE LIKE '" + satTablePrefix + "' ");
                    prepareSatXrefStatement.AppendLine("AND VERSION_ID = " + versionId);
                    prepareSatXrefStatement.AppendLine("AND [GENERATE_INDICATOR] = 'Y'");
                    prepareSatXrefStatement.AppendLine("UNION");
                    prepareSatXrefStatement.AppendLine("SELECT");
                    prepareSatXrefStatement.AppendLine("       sat.SATELLITE_TABLE_ID,");
                    prepareSatXrefStatement.AppendLine("	   sat.SATELLITE_TABLE_NAME,");
                    prepareSatXrefStatement.AppendLine("       stg.STAGING_AREA_TABLE_ID, ");
                    prepareSatXrefStatement.AppendLine("	   stg.STAGING_AREA_TABLE_NAME,");
                    prepareSatXrefStatement.AppendLine("	   spec.BUSINESS_KEY_ATTRIBUTE,");
                    prepareSatXrefStatement.AppendLine("       spec.FILTER_CRITERIA");
                    prepareSatXrefStatement.AppendLine("FROM MD_TABLE_MAPPING spec");
                    prepareSatXrefStatement.AppendLine("LEFT OUTER JOIN -- Join in the Staging_Area_ID from the MD_STG table");
                    prepareSatXrefStatement.AppendLine("       MD_STG stg ON stg.STAGING_AREA_TABLE_NAME=spec.STAGING_AREA_TABLE");
                    prepareSatXrefStatement.AppendLine("LEFT OUTER JOIN -- Join in the Satellite_ID from the MD_SAT table");
                    prepareSatXrefStatement.AppendLine("       MD_SAT sat ON sat.SATELLITE_TABLE_NAME=spec.INTEGRATION_AREA_TABLE");
                    prepareSatXrefStatement.AppendLine("WHERE spec.INTEGRATION_AREA_TABLE LIKE '" + lsatTablePrefix + "' ");
                    prepareSatXrefStatement.AppendLine("AND [GENERATE_INDICATOR] = 'Y'");
                    prepareSatXrefStatement.AppendLine("AND VERSION_ID = " + versionId);


                    var listSat = GetDataTable(ref connOmd, prepareSatXrefStatement.ToString());

                    foreach (DataRow tableName in listSat.Rows)
                    {
                        using (var connection = new SqlConnection(metaDataConnection))
                        {
                            _alert.SetTextLogging("-->  Processing the " + tableName["STAGING_AREA_TABLE_NAME"] + " / " + tableName["SATELLITE_TABLE_NAME"] + " relationship\r\n");

                            var insertSatStatement = new StringBuilder();
                            var filterCriterion = tableName["FILTER_CRITERIA"].ToString();
                            filterCriterion = filterCriterion.Replace("'", "''");

                            var businessKeyDefinition = tableName["BUSINESS_KEY_ATTRIBUTE"].ToString();
                            businessKeyDefinition = businessKeyDefinition.Replace("'", "''");

                            insertSatStatement.AppendLine("INSERT INTO [MD_STG_SAT_XREF]");
                            insertSatStatement.AppendLine("([SATELLITE_TABLE_ID], [STAGING_AREA_TABLE_ID], [BUSINESS_KEY_DEFINITION], [FILTER_CRITERIA])");
                            insertSatStatement.AppendLine("VALUES ('" + tableName["SATELLITE_TABLE_ID"] + "','" + tableName["STAGING_AREA_TABLE_ID"] + "','" + businessKeyDefinition + "','" + filterCriterion + "')");

                            var command = new SqlCommand(insertSatStatement.ToString(), connection);

                            try
                            {
                                connection.Open();
                                command.ExecuteNonQuery();
                                satCounter++;
                            }
                            catch (Exception ex)
                            {
                                errorCounter++;
                                _alert.SetTextLogging("An issue has occured during preparation of the relationship between the Staging Area and the Satellite. Please check the Error Log for more details.\r\n");
                                errorLog.AppendLine("\r\nAn issue has occured during preparation of the Staging / Satellite XREF: \r\n\r\n" + ex);
                            }
                        }
                    }

                    worker.ReportProgress(28);
                    _alert.SetTextLogging("Preparation of the Staging / Satellite XREF metadata completed.\r\n");
                }
                catch (Exception ex)
                {
                    errorCounter++;
                    _alert.SetTextLogging("An issue has occured during preparation of the relationship between the Staging Area and the Satellite. Please check the Error Log for more details.\r\n");
                    errorLog.AppendLine("\r\nAn issue has occured during preparation of the Staging / Satellite XREF: \r\n\r\n" + ex);
                }

                #endregion



                #region Staging / Hub relationship - 30%
                //6. Prepare STG / HUB xref
                _alert.SetTextLogging("\r\n");
                _alert.SetTextLogging("Commencing preparing the relationship between Staging Area and Hubs.\r\n");

                try
                {
                    var prepareStgHubXrefStatement = new StringBuilder();

                    prepareStgHubXrefStatement.AppendLine("SELECT");
                    prepareStgHubXrefStatement.AppendLine("    HUB_TABLE_ID,");
                    prepareStgHubXrefStatement.AppendLine("	   HUB_TABLE_NAME,");
                    prepareStgHubXrefStatement.AppendLine("    STAGING_AREA_TABLE_ID,");
                    prepareStgHubXrefStatement.AppendLine("	   STAGING_AREA_TABLE_NAME,");
                    prepareStgHubXrefStatement.AppendLine("	   BUSINESS_KEY_ATTRIBUTE,");
                    prepareStgHubXrefStatement.AppendLine("    FILTER_CRITERIA");
                    prepareStgHubXrefStatement.AppendLine("FROM");
                    prepareStgHubXrefStatement.AppendLine("       (      ");
                    prepareStgHubXrefStatement.AppendLine("              SELECT DISTINCT ");
                    prepareStgHubXrefStatement.AppendLine("                     STAGING_AREA_TABLE,");
                    prepareStgHubXrefStatement.AppendLine("                     INTEGRATION_AREA_TABLE,");
                    prepareStgHubXrefStatement.AppendLine("					    BUSINESS_KEY_ATTRIBUTE,");
                    prepareStgHubXrefStatement.AppendLine("                     FILTER_CRITERIA");
                    prepareStgHubXrefStatement.AppendLine("              FROM   MD_TABLE_MAPPING");
                    prepareStgHubXrefStatement.AppendLine("              WHERE ");
                    prepareStgHubXrefStatement.AppendLine("                     INTEGRATION_AREA_TABLE LIKE '" + hubTablePrefix + "'");
                    prepareStgHubXrefStatement.AppendLine("              AND VERSION_ID = " + versionId);
                    prepareStgHubXrefStatement.AppendLine("              AND [GENERATE_INDICATOR] = 'Y'");
                    prepareStgHubXrefStatement.AppendLine("       ) hub");
                    prepareStgHubXrefStatement.AppendLine("LEFT OUTER JOIN");
                    prepareStgHubXrefStatement.AppendLine("       ( ");
                    prepareStgHubXrefStatement.AppendLine("              SELECT STAGING_AREA_TABLE_ID, STAGING_AREA_TABLE_NAME");
                    prepareStgHubXrefStatement.AppendLine("              FROM MD_STG");
                    prepareStgHubXrefStatement.AppendLine("       ) stgsub");
                    prepareStgHubXrefStatement.AppendLine("ON hub.STAGING_AREA_TABLE=stgsub.STAGING_AREA_TABLE_NAME");
                    prepareStgHubXrefStatement.AppendLine("LEFT OUTER JOIN");
                    prepareStgHubXrefStatement.AppendLine("       ( ");
                    prepareStgHubXrefStatement.AppendLine("              SELECT HUB_TABLE_ID, HUB_TABLE_NAME");
                    prepareStgHubXrefStatement.AppendLine("              FROM MD_HUB");
                    prepareStgHubXrefStatement.AppendLine("       ) hubsub");
                    prepareStgHubXrefStatement.AppendLine("ON hub.INTEGRATION_AREA_TABLE=hubsub.HUB_TABLE_NAME");


                    var listXref = GetDataTable(ref connOmd, prepareStgHubXrefStatement.ToString());

                    foreach (DataRow tableName in listXref.Rows)
                    {
                        using (var connection = new SqlConnection(metaDataConnection))
                        {
                            _alert.SetTextLogging("-->  Processing the " + tableName["STAGING_AREA_TABLE_NAME"] + " / " + tableName["HUB_TABLE_NAME"] + " relationship\r\n");

                            var insertXrefStatement = new StringBuilder();
                            var filterCriterion = tableName["FILTER_CRITERIA"].ToString();
                            filterCriterion = filterCriterion.Replace("'", "''");

                            var businessKeyDefinition = tableName["BUSINESS_KEY_ATTRIBUTE"].ToString();
                            businessKeyDefinition = businessKeyDefinition.Replace("'", "''");

                            insertXrefStatement.AppendLine("INSERT INTO [MD_STG_HUB_XREF]");
                            insertXrefStatement.AppendLine("([HUB_TABLE_ID], [STAGING_AREA_TABLE_ID], [BUSINESS_KEY_DEFINITION], [FILTER_CRITERIA])");
                            insertXrefStatement.AppendLine("VALUES ('" + tableName["HUB_TABLE_ID"] + "','" + tableName["STAGING_AREA_TABLE_ID"] + "','" + businessKeyDefinition + "','" + filterCriterion + "')");

                            var command = new SqlCommand(insertXrefStatement.ToString(), connection);

                            try
                            {
                                connection.Open();
                                command.ExecuteNonQuery();
                            }
                            catch (Exception ex)
                            {
                                errorCounter++;
                                _alert.SetTextLogging("An issue has occured during preparation of the relationship between the Staging Area and the Hubs. Please check the Error Log for more details.\r\n");
                                errorLog.AppendLine("\r\nAn issue has occured during preparation of the Staging / Hub XREF: \r\n\r\n" + ex);
                            }
                        }
                    }

                    worker.ReportProgress(30);
                    _alert.SetTextLogging("Preparation of the relationship between Staging Area and Hubs completed.\r\n");
                }
                catch (Exception ex)
                {
                    errorCounter++;
                    _alert.SetTextLogging("An issue has occured during preparation of the relationship between the Staging Area and the Hubs. Please check the Error Log for more details.\r\n");
                    errorLog.AppendLine("\r\nAn issue has occured during preparation of the Staging / Hub XREF: \r\n\r\n" + ex);
                }
                #endregion



                #region Prepare attributes - 40%
                //7. Prepare Attributes
                _alert.SetTextLogging("\r\n");

                try
                {
                    var prepareAttStatement = new StringBuilder();
                    var attCounter = 1;

                    // Insert Not Applicable attribute for FKs
                    using (var connection = new SqlConnection(metaDataConnection))
                    {
                        var insertAttDummyStatement = new StringBuilder();

                        insertAttDummyStatement.AppendLine("INSERT INTO [MD_ATT]");
                        insertAttDummyStatement.AppendLine("([ATTRIBUTE_ID], [ATTRIBUTE_NAME])");
                        insertAttDummyStatement.AppendLine("VALUES ('-1','Not applicable')");

                        var command = new SqlCommand(insertAttDummyStatement.ToString(), connection);

                        try
                        {
                            connection.Open();
                            command.ExecuteNonQuery();
                            attCounter++;
                        }
                        catch (Exception ex)
                        {
                            errorCounter++;
                            _alert.SetTextLogging(
                                "An issue has occured during preparation of the attribute metadata. Please check the Error Log for more details.\r\n");
                            errorLog.AppendLine("\r\nAn issue has occured during preparation of attribute metadata: \r\n\r\n" + ex);
                        }
                    }

                    // Regular processing
                    if (checkBoxIgnoreVersion.Checked) // Read from live databasse
                    {
                        _alert.SetTextLogging("Commencing preparing the attributes directly from the database.\r\n");
                        prepareAttStatement.AppendLine("SELECT DISTINCT(COLUMN_NAME) AS COLUMN_NAME FROM");
                        prepareAttStatement.AppendLine("(");
                        prepareAttStatement.AppendLine("	SELECT COLUMN_NAME FROM " + linkedServer + stagingDatabase + ".INFORMATION_SCHEMA.COLUMNS");
                        prepareAttStatement.AppendLine("	UNION");
                        prepareAttStatement.AppendLine("	SELECT COLUMN_NAME FROM " + linkedServer + integrationDatabase + ".INFORMATION_SCHEMA.COLUMNS");
                        prepareAttStatement.AppendLine(") sub1");
                    }
                    else
                    {
                        _alert.SetTextLogging("Commencing preparing the attributes from the metadata.\r\n");
                        prepareAttStatement.AppendLine("SELECT DISTINCT COLUMN_NAME FROM MD_VERSION_ATTRIBUTE");
                        prepareAttStatement.AppendLine("WHERE VERSION_ID = " + versionId);
                    }

                    var listAtt = GetDataTable(ref connOmd, prepareAttStatement.ToString());

                    if (listAtt.Rows.Count == 0)
                    {
                        _alert.SetTextLogging("-->  No attributes were found in the metadata, did you reverse-engineer the model?\r\n");
                    }
                    else
                    {
                        foreach (DataRow tableName in listAtt.Rows)
                        {
                            using (var connection = new SqlConnection(metaDataConnection))
                            {
                                //_alert.SetTextLogging("-->  Processing " + tableName["COLUMN_NAME"] + ".\r\n");

                                var insertAttStatement = new StringBuilder();

                                insertAttStatement.AppendLine("INSERT INTO [MD_ATT]");
                                insertAttStatement.AppendLine("([ATTRIBUTE_ID], [ATTRIBUTE_NAME])");
                                insertAttStatement.AppendLine("VALUES (" + attCounter + ",'" + tableName["COLUMN_NAME"] + "')");

                                var command = new SqlCommand(insertAttStatement.ToString(), connection);

                                try
                                {
                                    connection.Open();
                                    command.ExecuteNonQuery();
                                    attCounter++;
                                }
                                catch (Exception ex)
                                {
                                    errorCounter++;
                                    _alert.SetTextLogging("An issue has occured during preparation of the attribute metadata. Please check the Error Log for more details.\r\n");
                                    errorLog.AppendLine("\r\nAn issue has occured during preparation of attribute metadata: \r\n\r\n" + ex);
                                }
                            }

                        }
                        _alert.SetTextLogging("-->  Processing " + attCounter + " attributes.\r\n");
                    }
                    worker.ReportProgress(40);

                    _alert.SetTextLogging("Preparation of the attributes completed.\r\n");
                }
                catch (Exception ex)
                {
                    errorCounter++;
                    _alert.SetTextLogging("An issue has occured during preparation of the attribute metadata. Please check the Error Log for more details.\r\n");
                    errorLog.AppendLine("\r\nAn issue has occured during preparation of attribute metadata: \r\n\r\n" + ex);
                }

                #endregion

                #region Business Key - 50%
                //8. Understanding the Business Key (MD_BUSINESS_KEY_COMPONENT)

                _alert.SetTextLogging("\r\n");
                _alert.SetTextLogging("Commencing the definition of the Business Key.\r\n");

                try
                {
                    var prepareKeyStatement = new StringBuilder();

                    prepareKeyStatement.AppendLine("SELECT ");
                    prepareKeyStatement.AppendLine("  STAGING_AREA_TABLE_ID,");
                    prepareKeyStatement.AppendLine("  STAGING_AREA_TABLE_NAME,");
                    prepareKeyStatement.AppendLine("  HUB_TABLE_ID,");
                    prepareKeyStatement.AppendLine("  HUB_TABLE_NAME,");
                    prepareKeyStatement.AppendLine("  BUSINESS_KEY_ATTRIBUTE,");
                    prepareKeyStatement.AppendLine("  ROW_NUMBER() OVER(PARTITION BY STAGING_AREA_TABLE_ID, HUB_TABLE_ID, BUSINESS_KEY_ATTRIBUTE ORDER BY STAGING_AREA_TABLE_ID, HUB_TABLE_ID, COMPONENT_ORDER ASC) AS COMPONENT_ID,");
                    prepareKeyStatement.AppendLine("  COMPONENT_ORDER,");
                    prepareKeyStatement.AppendLine("  REPLACE(COMPONENT_VALUE,'COMPOSITE(', '') AS COMPONENT_VALUE,");
                    prepareKeyStatement.AppendLine("    CASE");
                    prepareKeyStatement.AppendLine("            WHEN SUBSTRING(BUSINESS_KEY_ATTRIBUTE,1, 11)= 'CONCATENATE' THEN 'CONCATENATE()'");
                    prepareKeyStatement.AppendLine("            WHEN SUBSTRING(BUSINESS_KEY_ATTRIBUTE,1, 6)= 'PIVOT' THEN 'PIVOT()'");
                    prepareKeyStatement.AppendLine("            WHEN SUBSTRING(BUSINESS_KEY_ATTRIBUTE,1, 9)= 'COMPOSITE' THEN 'COMPOSITE()'");
                    prepareKeyStatement.AppendLine("            ELSE 'NORMAL'");
                    prepareKeyStatement.AppendLine("    END AS COMPONENT_TYPE");
                    prepareKeyStatement.AppendLine("FROM");
                    prepareKeyStatement.AppendLine("(");
                    prepareKeyStatement.AppendLine("    SELECT DISTINCT");
                    prepareKeyStatement.AppendLine("        A.STAGING_AREA_TABLE,");
                    prepareKeyStatement.AppendLine("        A.BUSINESS_KEY_ATTRIBUTE,");
                    prepareKeyStatement.AppendLine("        A.INTEGRATION_AREA_TABLE,");
                    prepareKeyStatement.AppendLine("        CASE");
                    prepareKeyStatement.AppendLine("            WHEN CHARINDEX('(', RTRIM(LTRIM(Split.a.value('.', 'VARCHAR(MAX)')))) > 0");
                    prepareKeyStatement.AppendLine("            THEN RTRIM(LTRIM(Split.a.value('.', 'VARCHAR(MAX)')))");
                    prepareKeyStatement.AppendLine("            ELSE REPLACE(RTRIM(LTRIM(Split.a.value('.', 'VARCHAR(MAX)'))), ')', '')");
                    prepareKeyStatement.AppendLine("        END AS COMPONENT_VALUE,");
                    prepareKeyStatement.AppendLine("        ROW_NUMBER() OVER(PARTITION BY STAGING_AREA_TABLE, INTEGRATION_AREA_TABLE, BUSINESS_KEY_ATTRIBUTE ORDER BY STAGING_AREA_TABLE, INTEGRATION_AREA_TABLE, BUSINESS_KEY_ATTRIBUTE ASC) AS COMPONENT_ORDER");
                    prepareKeyStatement.AppendLine("    FROM");
                    prepareKeyStatement.AppendLine("    (");

                    // Change to move from comma separate to semicolon separation for composite keys
                    prepareKeyStatement.AppendLine("      SELECT");
                    prepareKeyStatement.AppendLine("          STAGING_AREA_TABLE, ");
                    prepareKeyStatement.AppendLine("          INTEGRATION_AREA_TABLE, ");
                    prepareKeyStatement.AppendLine("          BUSINESS_KEY_ATTRIBUTE,");
                    prepareKeyStatement.AppendLine("          CASE SUBSTRING(BUSINESS_KEY_ATTRIBUTE, 0, CHARINDEX('(', BUSINESS_KEY_ATTRIBUTE))");
                    prepareKeyStatement.AppendLine("        	 WHEN 'COMPOSITE' THEN CONVERT(XML, '<M>' + REPLACE(BUSINESS_KEY_ATTRIBUTE, ';', '</M><M>') + '</M>') ");
                    prepareKeyStatement.AppendLine("        	 ELSE CONVERT(XML, '<M>' + REPLACE(BUSINESS_KEY_ATTRIBUTE, ',', '</M><M>') + '</M>') ");
                    prepareKeyStatement.AppendLine("          END AS BUSINESS_KEY_ATTRIBUTE_XML");
                    // End of composite key change

                    prepareKeyStatement.AppendLine("        FROM");
                    prepareKeyStatement.AppendLine("        (");
                    prepareKeyStatement.AppendLine("            SELECT DISTINCT STAGING_AREA_TABLE, INTEGRATION_AREA_TABLE, LTRIM(RTRIM(BUSINESS_KEY_ATTRIBUTE)) AS BUSINESS_KEY_ATTRIBUTE");
                    prepareKeyStatement.AppendLine("            FROM MD_TABLE_MAPPING");
                    prepareKeyStatement.AppendLine("            WHERE INTEGRATION_AREA_TABLE LIKE '" + hubTablePrefix + "'");
                    prepareKeyStatement.AppendLine("              AND VERSION_ID = " + versionId);
                    prepareKeyStatement.AppendLine("              AND [GENERATE_INDICATOR] = 'Y'");
                    prepareKeyStatement.AppendLine("        ) TableName");
                    prepareKeyStatement.AppendLine("    ) AS A CROSS APPLY BUSINESS_KEY_ATTRIBUTE_XML.nodes('/M') AS Split(a)");
                    prepareKeyStatement.AppendLine("");
                    prepareKeyStatement.AppendLine("    WHERE BUSINESS_KEY_ATTRIBUTE <> 'N/A' AND A.BUSINESS_KEY_ATTRIBUTE != ''");
                    prepareKeyStatement.AppendLine(") pivotsub");
                    prepareKeyStatement.AppendLine("LEFT OUTER JOIN");
                    prepareKeyStatement.AppendLine("       (");
                    prepareKeyStatement.AppendLine("              SELECT STAGING_AREA_TABLE_ID, STAGING_AREA_TABLE_NAME");
                    prepareKeyStatement.AppendLine("              FROM MD_STG");
                    prepareKeyStatement.AppendLine("       ) stgsub");
                    prepareKeyStatement.AppendLine("ON pivotsub.STAGING_AREA_TABLE = stgsub.STAGING_AREA_TABLE_NAME");
                    prepareKeyStatement.AppendLine("LEFT OUTER JOIN");
                    prepareKeyStatement.AppendLine("       (");
                    prepareKeyStatement.AppendLine("              SELECT HUB_TABLE_ID, HUB_TABLE_NAME");
                    prepareKeyStatement.AppendLine("              FROM MD_HUB");
                    prepareKeyStatement.AppendLine("       ) hubsub");
                    prepareKeyStatement.AppendLine("ON pivotsub.INTEGRATION_AREA_TABLE = hubsub.HUB_TABLE_NAME");
                    prepareKeyStatement.AppendLine("ORDER BY stgsub.STAGING_AREA_TABLE_ID, hubsub.HUB_TABLE_ID, COMPONENT_ORDER");

                    var listKeys = GetDataTable(ref connOmd, prepareKeyStatement.ToString());

                    if (listKeys.Rows.Count == 0)
                    {
                        _alert.SetTextLogging("-->  No attributes were found in the metadata, did you reverse-engineer the model?\r\n");
                    }
                    else
                    {
                        foreach (DataRow tableName in listKeys.Rows)
                        {
                            using (var connection = new SqlConnection(metaDataConnection))
                            {
                                _alert.SetTextLogging("-->  Processing the Business Key from " + tableName["STAGING_AREA_TABLE_NAME"] + " to " + tableName["HUB_TABLE_NAME"] + "\r\n");

                                var insertKeyStatement = new StringBuilder();
                                var keyComponent = tableName["COMPONENT_VALUE"]; //Handle quotes between SQL and C%
                                keyComponent = keyComponent.ToString().Replace("'", "''");

                                var businessKeyDefinition = tableName["BUSINESS_KEY_ATTRIBUTE"].ToString();
                                businessKeyDefinition = businessKeyDefinition.Replace("'", "''");

                                insertKeyStatement.AppendLine("INSERT INTO [MD_BUSINESS_KEY_COMPONENT]");
                                insertKeyStatement.AppendLine("(STAGING_AREA_TABLE_ID, HUB_TABLE_ID, BUSINESS_KEY_DEFINITION, COMPONENT_ID, COMPONENT_ORDER, COMPONENT_VALUE, COMPONENT_TYPE)");
                                insertKeyStatement.AppendLine("VALUES ('" + tableName["STAGING_AREA_TABLE_ID"] + "','" + tableName["HUB_TABLE_ID"] + "','" + businessKeyDefinition + "','" + tableName["COMPONENT_ID"] + "','" + tableName["COMPONENT_ORDER"] + "','" + keyComponent + "','" + tableName["COMPONENT_TYPE"] + "')");

                                var command = new SqlCommand(insertKeyStatement.ToString(), connection);

                                try
                                {
                                    connection.Open();
                                    command.ExecuteNonQuery();
                                }
                                catch (Exception ex)
                                {
                                    errorCounter++;
                                    _alert.SetTextLogging("An issue has occured during preparation of the Business Key metadata. Please check the Error Log for more details.\r\n");
                                    errorLog.AppendLine("\r\nAn issue has occured during preparation of Business Key metadata: \r\n\r\n" + ex);
                                }
                            }
                        }
                    }
                    worker.ReportProgress(50);
                    // _alert.SetTextLogging("-->  Processing " + keyCounter + " attributes.\r\n");
                    _alert.SetTextLogging("Preparation of the Business Key definition completed.\r\n");
                }
                catch (Exception ex)
                {
                    errorCounter++;
                    _alert.SetTextLogging("An issue has occured during preparation of the Business Key metadata. Please check the Error Log for more details.\r\n");
                    errorLog.AppendLine("\r\nAn issue has occured during preparation of Business Key metadata: \r\n\r\n" + ex);
                }

                #endregion

                #region Business Key components - 60%
                //9. Understanding the Business Key component parts

                _alert.SetTextLogging("\r\n");
                _alert.SetTextLogging("Commencing the Business Key component analysis.\r\n");

                try
                {
                    var prepareKeyComponentStatement = new StringBuilder();
                    var keyPartCounter = 1;

                    prepareKeyComponentStatement.AppendLine("SELECT DISTINCT");
                    prepareKeyComponentStatement.AppendLine("  STAGING_AREA_TABLE_ID,");
                    prepareKeyComponentStatement.AppendLine("  HUB_TABLE_ID,");
                    prepareKeyComponentStatement.AppendLine("  BUSINESS_KEY_DEFINITION,");
                    prepareKeyComponentStatement.AppendLine("  COMPONENT_ID,");
                    prepareKeyComponentStatement.AppendLine("  ROW_NUMBER() over(partition by STAGING_AREA_TABLE_ID, HUB_TABLE_ID, BUSINESS_KEY_DEFINITION, COMPONENT_ID order by nullif(0 * Split.a.value('count(.)', 'int'), 0)) AS COMPONENT_ELEMENT_ID,");
                    prepareKeyComponentStatement.AppendLine("  ROW_NUMBER() over(partition by STAGING_AREA_TABLE_ID, HUB_TABLE_ID, BUSINESS_KEY_DEFINITION, COMPONENT_ID order by nullif(0 * Split.a.value('count(.)', 'int'), 0)) AS COMPONENT_ELEMENT_ORDER,");
                    prepareKeyComponentStatement.AppendLine("  REPLACE(REPLACE(REPLACE(RTRIM(LTRIM(Split.a.value('.', 'VARCHAR(MAX)'))), 'CONCATENATE(', ''), ')', ''), 'COMPOSITE(', '') AS COMPONENT_ELEMENT_VALUE,");
                    prepareKeyComponentStatement.AppendLine("  CASE");
                    prepareKeyComponentStatement.AppendLine("     WHEN charindex(CHAR(39), REPLACE(REPLACE(RTRIM(LTRIM(Split.a.value('.', 'VARCHAR(MAX)'))), 'CONCATENATE(', ''), ')', '')) = 1 THEN 'User Defined Value'");
                    prepareKeyComponentStatement.AppendLine("    ELSE 'Attribute'");
                    prepareKeyComponentStatement.AppendLine("  END AS COMPONENT_ELEMENT_TYPE,");
                    prepareKeyComponentStatement.AppendLine("  COALESCE(att.ATTRIBUTE_ID, -1) AS ATTRIBUTE_ID");
                    prepareKeyComponentStatement.AppendLine("FROM");
                    prepareKeyComponentStatement.AppendLine("(");
                    prepareKeyComponentStatement.AppendLine("    SELECT");
                    prepareKeyComponentStatement.AppendLine("        STAGING_AREA_TABLE_ID,");
                    prepareKeyComponentStatement.AppendLine("        HUB_TABLE_ID,");
                    prepareKeyComponentStatement.AppendLine("        BUSINESS_KEY_DEFINITION,");
                    prepareKeyComponentStatement.AppendLine("        COMPONENT_ID,");
                    prepareKeyComponentStatement.AppendLine("        COMPONENT_VALUE,");
                    prepareKeyComponentStatement.AppendLine("        CONVERT(XML, '<M>' + REPLACE(COMPONENT_VALUE, ';', '</M><M>') + '</M>') AS COMPONENT_VALUE_XML");
                    prepareKeyComponentStatement.AppendLine("    FROM MD_BUSINESS_KEY_COMPONENT");
                    prepareKeyComponentStatement.AppendLine(") AS A CROSS APPLY COMPONENT_VALUE_XML.nodes('/M') AS Split(a)");
                    prepareKeyComponentStatement.AppendLine("LEFT OUTER JOIN MD_ATT att ON");
                    prepareKeyComponentStatement.AppendLine("    REPLACE(REPLACE(RTRIM(LTRIM(Split.a.value('.', 'VARCHAR(MAX)'))), 'CONCATENATE(', ''), ')', '') = att.ATTRIBUTE_NAME");
                    prepareKeyComponentStatement.AppendLine("WHERE COMPONENT_VALUE <> 'N/A' AND A.COMPONENT_VALUE != ''");
                    prepareKeyComponentStatement.AppendLine("ORDER BY A.STAGING_AREA_TABLE_ID, A.HUB_TABLE_ID, BUSINESS_KEY_DEFINITION, A.COMPONENT_ID, COMPONENT_ELEMENT_ORDER");


                    var listKeyParts = GetDataTable(ref connOmd, prepareKeyComponentStatement.ToString());

                    if (listKeyParts.Rows.Count == 0)
                    {
                        _alert.SetTextLogging("-->  No attributes were found in the metadata, did you reverse-engineer the model?\r\n");
                    }
                    else
                    {
                        foreach (DataRow tableName in listKeyParts.Rows)
                        {
                            using (var connection = new SqlConnection(metaDataConnection))
                            {
                                var insertKeyPartStatement = new StringBuilder();

                                var keyComponent = tableName["COMPONENT_ELEMENT_VALUE"]; //Handle quotes between SQL and C%
                                keyComponent = keyComponent.ToString().Replace("'", "''");

                                var businessKeyDefinition = tableName["BUSINESS_KEY_DEFINITION"];
                                businessKeyDefinition = businessKeyDefinition.ToString().Replace("'", "''");

                                insertKeyPartStatement.AppendLine("INSERT INTO [MD_BUSINESS_KEY_COMPONENT_PART]");
                                insertKeyPartStatement.AppendLine("(STAGING_AREA_TABLE_ID, HUB_TABLE_ID, BUSINESS_KEY_DEFINITION, COMPONENT_ID,COMPONENT_ELEMENT_ID,COMPONENT_ELEMENT_ORDER,COMPONENT_ELEMENT_VALUE,COMPONENT_ELEMENT_TYPE,ATTRIBUTE_ID)");
                                insertKeyPartStatement.AppendLine("VALUES ('" + tableName["STAGING_AREA_TABLE_ID"] + "','" + tableName["HUB_TABLE_ID"] + "','" + businessKeyDefinition + "','" + tableName["COMPONENT_ID"] + "','" + tableName["COMPONENT_ELEMENT_ID"] + "','" + tableName["COMPONENT_ELEMENT_ORDER"] + "','" + keyComponent + "','" + tableName["COMPONENT_ELEMENT_TYPE"] + "','" + tableName["ATTRIBUTE_ID"] + "')");

                                var command = new SqlCommand(insertKeyPartStatement.ToString(), connection);

                                try
                                {
                                    connection.Open();
                                    command.ExecuteNonQuery();
                                    keyPartCounter++;
                                }
                                catch (Exception ex)
                                {
                                    errorCounter++;
                                    _alert.SetTextLogging("An issue has occured during preparation of the Business Key component metadata. Please check the Error Log for more details.\r\n");
                                    errorLog.AppendLine("\r\nAn issue has occured during preparation of Business Key component metadata: \r\n\r\n" + ex);
                                    errorLog.AppendLine("The query that caused a problem was:\r\n");
                                    errorLog.AppendLine(insertKeyPartStatement.ToString());
                                }
                            }
                        }
                    }
                    worker.ReportProgress(60);
                    _alert.SetTextLogging("-->  Processing " + keyPartCounter + " Business Key component attributes\r\n");
                    _alert.SetTextLogging("Preparation of the Business Key components completed.\r\n");
                }
                catch (Exception ex)
                {
                    errorCounter++;
                    _alert.SetTextLogging("An issue has occured during preparation of the Business Key component metadata. Please check the Error Log for more details.\r\n");
                    errorLog.AppendLine("\r\nAn issue has occured during preparation of Business Key component metadata: \r\n\r\n" + ex);
                }

                #endregion

                #region Hub / Link relationship - 75%

                //10. Prepare HUB / LNK xref
                _alert.SetTextLogging("\r\n");
                _alert.SetTextLogging("Commencing preparing the relationship between Hubs and Links.\r\n");

                try
                {
                    var prepareHubLnkXrefStatement = new StringBuilder();

                    prepareHubLnkXrefStatement.AppendLine("SELECT");
                    prepareHubLnkXrefStatement.AppendLine("  hub_tbl.HUB_TABLE_ID,");
                    prepareHubLnkXrefStatement.AppendLine("  hub_tbl.HUB_TABLE_NAME,");
                    prepareHubLnkXrefStatement.AppendLine("  lnk_tbl.LINK_TABLE_ID,");
                    prepareHubLnkXrefStatement.AppendLine("  lnk_tbl.LINK_TABLE_NAME,");
                    prepareHubLnkXrefStatement.AppendLine("  lnk_hubkey_order.HUB_KEY_ORDER AS HUB_ORDER,");
                    prepareHubLnkXrefStatement.AppendLine("  lnk_target_model.HUB_TARGET_KEY_NAME_IN_LINK");
                    prepareHubLnkXrefStatement.AppendLine("FROM");
                    prepareHubLnkXrefStatement.AppendLine("-- This base query adds the Link and its Hubs and their order by pivoting on the full business key");
                    prepareHubLnkXrefStatement.AppendLine("(");
                    prepareHubLnkXrefStatement.AppendLine("  SELECT");
                    prepareHubLnkXrefStatement.AppendLine("    INTEGRATION_AREA_TABLE,");
                    prepareHubLnkXrefStatement.AppendLine("    STAGING_AREA_TABLE,");
                    prepareHubLnkXrefStatement.AppendLine("    BUSINESS_KEY_ATTRIBUTE,");
                    prepareHubLnkXrefStatement.AppendLine("    LTRIM(Split.a.value('.', 'VARCHAR(4000)')) AS BUSINESS_KEY_PART,");
                    prepareHubLnkXrefStatement.AppendLine("    ROW_NUMBER() OVER(PARTITION BY INTEGRATION_AREA_TABLE ORDER BY INTEGRATION_AREA_TABLE) AS HUB_KEY_ORDER");
                    prepareHubLnkXrefStatement.AppendLine("  FROM");
                    prepareHubLnkXrefStatement.AppendLine("  (");
                    prepareHubLnkXrefStatement.AppendLine("    SELECT");
                    prepareHubLnkXrefStatement.AppendLine("      INTEGRATION_AREA_TABLE,");
                    prepareHubLnkXrefStatement.AppendLine("      STAGING_AREA_TABLE,");
                    prepareHubLnkXrefStatement.AppendLine("      ROW_NUMBER() OVER(PARTITION BY INTEGRATION_AREA_TABLE ORDER BY INTEGRATION_AREA_TABLE) AS LINK_ORDER,");
                    prepareHubLnkXrefStatement.AppendLine("      BUSINESS_KEY_ATTRIBUTE, CAST('<M>' + REPLACE(BUSINESS_KEY_ATTRIBUTE, ',', '</M><M>') + '</M>' AS XML) AS BUSINESS_KEY_SOURCE_XML");
                    prepareHubLnkXrefStatement.AppendLine("    FROM  MD_TABLE_MAPPING");
                    prepareHubLnkXrefStatement.AppendLine("    WHERE [INTEGRATION_AREA_TABLE] LIKE '" + lnkTablePrefix + "'");
                    prepareHubLnkXrefStatement.AppendLine("      AND [VERSION_ID] = " + versionId);
                    prepareHubLnkXrefStatement.AppendLine("      AND [GENERATE_INDICATOR] = 'Y'");
                    prepareHubLnkXrefStatement.AppendLine("  ) AS A CROSS APPLY BUSINESS_KEY_SOURCE_XML.nodes('/M') AS Split(a)");
                    prepareHubLnkXrefStatement.AppendLine("  WHERE LINK_ORDER=1 --Any link will do, the order of the Hub keys in the Link will always be the same");
                    prepareHubLnkXrefStatement.AppendLine(") lnk_hubkey_order");

                    prepareHubLnkXrefStatement.AppendLine("-- Adding the information required for the target model in the query");
                    prepareHubLnkXrefStatement.AppendLine("JOIN ");
                    prepareHubLnkXrefStatement.AppendLine("(");
                    prepareHubLnkXrefStatement.AppendLine("SELECT ");
                    prepareHubLnkXrefStatement.AppendLine("	TABLE_NAME AS LINK_TABLE_NAME,");
                    prepareHubLnkXrefStatement.AppendLine("	COLUMN_NAME AS HUB_TARGET_KEY_NAME_IN_LINK ,");
                    prepareHubLnkXrefStatement.AppendLine("	ROW_NUMBER() OVER(PARTITION BY TABLE_NAME ORDER BY ORDINAL_POSITION) AS LINK_ORDER");
                    prepareHubLnkXrefStatement.AppendLine("FROM " + integrationDatabase + ".INFORMATION_SCHEMA.COLUMNS");
                    prepareHubLnkXrefStatement.AppendLine("WHERE [ORDINAL_POSITION]>4");
                    prepareHubLnkXrefStatement.AppendLine("AND [TABLE_NAME] LIKE '" + lnkTablePrefix + "'");
                    prepareHubLnkXrefStatement.AppendLine(") lnk_target_model");
                    prepareHubLnkXrefStatement.AppendLine("ON lnk_hubkey_order.INTEGRATION_AREA_TABLE = lnk_target_model.LINK_TABLE_NAME");
                    prepareHubLnkXrefStatement.AppendLine("AND lnk_hubkey_order.HUB_KEY_ORDER = lnk_target_model.LINK_ORDER");

                    prepareHubLnkXrefStatement.AppendLine("--Adding the Hub mapping data to get the business keys");
                    prepareHubLnkXrefStatement.AppendLine("JOIN MD_TABLE_MAPPING hub");
                    prepareHubLnkXrefStatement.AppendLine("  ON lnk_hubkey_order.[STAGING_AREA_TABLE] = hub.STAGING_AREA_TABLE");
                    prepareHubLnkXrefStatement.AppendLine(" AND lnk_hubkey_order.[BUSINESS_KEY_PART] = hub.BUSINESS_KEY_ATTRIBUTE-- This condition is required to remove the redundant rows caused by the Link key pivoting");
                    prepareHubLnkXrefStatement.AppendLine(" AND hub.[INTEGRATION_AREA_TABLE] LIKE '" + hubTablePrefix + "'");
                    prepareHubLnkXrefStatement.AppendLine(" AND hub.[VERSION_ID] = " + versionId);
                    prepareHubLnkXrefStatement.AppendLine(" AND hub.[GENERATE_INDICATOR] = 'Y'");
                    prepareHubLnkXrefStatement.AppendLine("--Lastly adding the IDs for the Hubs and Links");
                    prepareHubLnkXrefStatement.AppendLine("JOIN dbo.MD_HUB hub_tbl");
                    prepareHubLnkXrefStatement.AppendLine("  ON hub.INTEGRATION_AREA_TABLE = hub_tbl.HUB_TABLE_NAME");
                    prepareHubLnkXrefStatement.AppendLine("JOIN dbo.MD_LINK lnk_tbl");
                    prepareHubLnkXrefStatement.AppendLine("  ON lnk_hubkey_order.INTEGRATION_AREA_TABLE = lnk_tbl.LINK_TABLE_NAME");

                    var listHlXref = GetDataTable(ref connOmd, prepareHubLnkXrefStatement.ToString());

                    foreach (DataRow tableName in listHlXref.Rows)
                    {
                        using (var connection = new SqlConnection(metaDataConnection))
                        {
                            _alert.SetTextLogging("-->  Processing the " + tableName["HUB_TABLE_NAME"] + " / " + tableName["LINK_TABLE_NAME"] + " relationship\r\n");

                            var insertHlXrefStatement = new StringBuilder();

                            insertHlXrefStatement.AppendLine("INSERT INTO [MD_HUB_LINK_XREF]");
                            insertHlXrefStatement.AppendLine("([HUB_TABLE_ID], [LINK_TABLE_ID], [HUB_ORDER], [HUB_TARGET_KEY_NAME_IN_LINK])");
                            insertHlXrefStatement.AppendLine("VALUES ('" + tableName["HUB_TABLE_ID"] + "','" + tableName["LINK_TABLE_ID"] + "','" + tableName["HUB_ORDER"] + "','" + tableName["HUB_TARGET_KEY_NAME_IN_LINK"] + "')");

                            var command = new SqlCommand(insertHlXrefStatement.ToString(), connection);

                            try
                            {
                                connection.Open();
                                command.ExecuteNonQuery();
                            }
                            catch (Exception ex)
                            {
                                errorCounter++;
                                _alert.SetTextLogging("An issue has occured during preparation of the Hub / Link XREF metadata. Please check the Error Log for more details.\r\n");
                                errorLog.AppendLine("\r\nAn issue has occured during preparation of the Hub / Link XREF metadata: \r\n\r\n" + ex);
                            }
                        }
                    }

                    worker.ReportProgress(75);
                    _alert.SetTextLogging("Preparation of the relationship between Hubs and Links completed.\r\n");
                }
                catch (Exception ex)
                {
                    {
                        errorCounter++;
                        _alert.SetTextLogging("An issue has occured during preparation of the Hub / Link XREF metadata. Please check the Error Log for more details.\r\n");
                        errorLog.AppendLine("\r\nAn issue has occured during preparation of the Hub / Link XREF metadata: \r\n\r\n" + ex);
                    }
                }

                #endregion



                #region Stg / Link relationship - 80%

                //10. Prepare STG / LNK xref
                _alert.SetTextLogging("\r\n");
                _alert.SetTextLogging("Commencing preparing the relationship between Staging Area and Link tables.\r\n");

                try
                {
                    var preparestgLnkXrefStatement = new StringBuilder();

                    preparestgLnkXrefStatement.AppendLine("SELECT");
                    preparestgLnkXrefStatement.AppendLine("  lnk_tbl.LINK_TABLE_ID,");
                    preparestgLnkXrefStatement.AppendLine("  lnk_tbl.LINK_TABLE_NAME,");
                    preparestgLnkXrefStatement.AppendLine("  stg_tbl.STAGING_AREA_TABLE_ID,");
                    preparestgLnkXrefStatement.AppendLine("  stg_tbl.STAGING_AREA_TABLE_NAME,");
                    preparestgLnkXrefStatement.AppendLine("  lnk.FILTER_CRITERIA,");
                    preparestgLnkXrefStatement.AppendLine("  lnk.BUSINESS_KEY_ATTRIBUTE");
                    preparestgLnkXrefStatement.AppendLine("FROM [dbo].[MD_TABLE_MAPPING] lnk");
                    preparestgLnkXrefStatement.AppendLine("JOIN [dbo].[MD_LINK] lnk_tbl ON lnk.INTEGRATION_AREA_TABLE = lnk_tbl.LINK_TABLE_NAME");
                    preparestgLnkXrefStatement.AppendLine("JOIN [dbo].[MD_STG] stg_tbl ON lnk.STAGING_AREA_TABLE = stg_tbl.STAGING_AREA_TABLE_NAME");
                    preparestgLnkXrefStatement.AppendLine("WHERE lnk.INTEGRATION_AREA_TABLE like '" + lnkTablePrefix + "'");
                    preparestgLnkXrefStatement.AppendLine("AND lnk.VERSION_ID = " + versionId);
                    preparestgLnkXrefStatement.AppendLine("AND [GENERATE_INDICATOR] = 'Y'");

                    var listHlXref = GetDataTable(ref connOmd, preparestgLnkXrefStatement.ToString());

                    foreach (DataRow tableName in listHlXref.Rows)
                    {
                        using (var connection = new SqlConnection(metaDataConnection))
                        {
                            _alert.SetTextLogging("-->  Processing the " + tableName["STAGING_AREA_TABLE_NAME"] + " / " + tableName["LINK_TABLE_NAME"] + " relationship\r\n");

                            var insertStgLinkStatement = new StringBuilder();

                            var filterCriterion = tableName["FILTER_CRITERIA"].ToString();
                            filterCriterion = filterCriterion.Replace("'", "''");

                            var businessKeyDefinition = tableName["BUSINESS_KEY_ATTRIBUTE"].ToString();
                            businessKeyDefinition = businessKeyDefinition.Replace("'", "''");

                            insertStgLinkStatement.AppendLine("INSERT INTO [MD_STG_LINK_XREF]");
                            insertStgLinkStatement.AppendLine("([STAGING_AREA_TABLE_ID], [LINK_TABLE_ID], [FILTER_CRITERIA], [BUSINESS_KEY_DEFINITION])");
                            insertStgLinkStatement.AppendLine("VALUES ('" + tableName["STAGING_AREA_TABLE_ID"] + "','" + tableName["LINK_TABLE_ID"] + "','" + filterCriterion + "','" + businessKeyDefinition + "')");

                            var command = new SqlCommand(insertStgLinkStatement.ToString(), connection);

                            try
                            {
                                connection.Open();
                                command.ExecuteNonQuery();
                            }
                            catch (Exception ex)
                            {
                                errorCounter++;
                                _alert.SetTextLogging("An issue has occured during preparation of the Hub / Link XREF metadata. Please check the Error Log for more details.\r\n");
                                errorLog.AppendLine("\r\nAn issue has occured during preparation of the Hub / Link XREF metadata: \r\n\r\n" + ex);
                            }
                        }
                    }

                    worker.ReportProgress(80);
                    _alert.SetTextLogging("Preparation of the relationship between Staging Area and the Links completed.\r\n");
                }
                catch (Exception ex)
                {
                    {
                        errorCounter++;
                        _alert.SetTextLogging("An issue has occured during preparation of the Staging / Link XREF metadata. Please check the Error Log for more details.\r\n");
                        errorLog.AppendLine("\r\nAn issue has occured during preparation of the Staging / Link XREF metadata: \r\n\r\n" + ex);
                    }
                }

                #endregion

                #region Attribute mapping 90%
                //12. Prepare Attribute mapping
                _alert.SetTextLogging("\r\n");


                try
                {
                    var prepareMappingStatement = new StringBuilder();
                    var mappingCounter = 1;

                    if (checkBoxIgnoreVersion.Checked)
                    {
                        _alert.SetTextLogging("Commencing preparing the column-to-column mapping metadata based on what's available in the database.\r\n");

                        prepareMappingStatement.AppendLine("WITH MAPPED_ATTRIBUTES AS ");
                        prepareMappingStatement.AppendLine("(");
                        prepareMappingStatement.AppendLine("SELECT  stg.STAGING_AREA_TABLE_ID");
                        prepareMappingStatement.AppendLine("	   ,stg.STAGING_AREA_TABLE_NAME");
                        prepareMappingStatement.AppendLine("       ,sat.SATELLITE_TABLE_ID");
                        prepareMappingStatement.AppendLine("	   ,sat.SATELLITE_TABLE_NAME");
                        prepareMappingStatement.AppendLine("	   ,stg_attr.ATTRIBUTE_ID AS ATTRIBUTE_FROM_ID");
                        prepareMappingStatement.AppendLine("	   ,stg_attr.ATTRIBUTE_NAME AS ATTRIBUTE_FROM_NAME");
                        prepareMappingStatement.AppendLine("       ,target_attr.ATTRIBUTE_ID AS ATTRIBUTE_TO_ID   ");
                        prepareMappingStatement.AppendLine("	   ,target_attr.ATTRIBUTE_NAME AS ATTRIBUTE_TO_NAME");
                        prepareMappingStatement.AppendLine("	   ,'N' as MULTI_ACTIVE_KEY_INDICATOR");
                        prepareMappingStatement.AppendLine("	   ,'manually_mapped' as VERIFICATION");
                        prepareMappingStatement.AppendLine("FROM dbo.MD_ATTRIBUTE_MAPPING mapping");
                        prepareMappingStatement.AppendLine("       LEFT OUTER JOIN dbo.MD_SAT sat on sat.SATELLITE_TABLE_NAME=mapping.TARGET_TABLE");
                        prepareMappingStatement.AppendLine("	   LEFT OUTER JOIN dbo.MD_ATT target_attr on mapping.TARGET_COLUMN = target_attr.ATTRIBUTE_NAME");
                        prepareMappingStatement.AppendLine("	   LEFT OUTER JOIN dbo.MD_STG stg on stg.STAGING_AREA_TABLE_NAME = mapping.SOURCE_TABLE");
                        prepareMappingStatement.AppendLine("	   LEFT OUTER JOIN dbo.MD_ATT stg_attr on mapping.SOURCE_COLUMN = stg_attr.ATTRIBUTE_NAME");
                        prepareMappingStatement.AppendLine("	   LEFT OUTER JOIN dbo.MD_TABLE_MAPPING table_mapping");
                        prepareMappingStatement.AppendLine("	     on mapping.TARGET_TABLE = table_mapping.INTEGRATION_AREA_TABLE");
                        prepareMappingStatement.AppendLine("	    and mapping.SOURCE_TABLE = table_mapping.STAGING_AREA_TABLE");
                        prepareMappingStatement.AppendLine("WHERE mapping.TARGET_TABLE NOT LIKE '" + dwhKeyIdentifier + "' AND mapping.TARGET_TABLE NOT LIKE '" + lnkTablePrefix + "'");
                        prepareMappingStatement.AppendLine("      AND mapping.VERSION_ID = " + versionId);
                        prepareMappingStatement.AppendLine("      AND table_mapping.VERSION_ID = " + versionId);
                        prepareMappingStatement.AppendLine("      AND table_mapping.GENERATE_INDICATOR = 'Y'");
                        prepareMappingStatement.AppendLine("),");
                        prepareMappingStatement.AppendLine("ORIGINAL_ATTRIBUTES AS");
                        prepareMappingStatement.AppendLine("(");
                        prepareMappingStatement.AppendLine("SELECT");
                        prepareMappingStatement.AppendLine("	stg.STAGING_AREA_TABLE_ID,");
                        prepareMappingStatement.AppendLine("	stg.STAGING_AREA_TABLE_NAME,");
                        prepareMappingStatement.AppendLine("	sat.SATELLITE_TABLE_ID,");
                        prepareMappingStatement.AppendLine("	sat.SATELLITE_TABLE_NAME,");
                        prepareMappingStatement.AppendLine("	stg_attr.ATTRIBUTE_ID AS ATTRIBUTE_FROM_ID,");
                        prepareMappingStatement.AppendLine("	stg_attr.ATTRIBUTE_NAME AS ATTRIBUTE_FROM_NAME,");
                        prepareMappingStatement.AppendLine("	stg_attr.ATTRIBUTE_ID AS ATTRIBUTE_TO_ID,");
                        prepareMappingStatement.AppendLine("	stg_attr.ATTRIBUTE_NAME AS ATTRIBUTE_TO_NAME,");
                        prepareMappingStatement.AppendLine("    'N' as MULTI_ACTIVE_KEY_INDICATOR,");
                        prepareMappingStatement.AppendLine("    'automatically_mapped' AS VERIFICATION");
                        prepareMappingStatement.AppendLine("FROM " + linkedServer + stagingDatabase + ".INFORMATION_SCHEMA.COLUMNS mapping");
                        prepareMappingStatement.AppendLine("LEFT OUTER JOIN dbo.MD_STG stg ON stg.STAGING_AREA_TABLE_NAME = mapping.TABLE_NAME");
                        prepareMappingStatement.AppendLine("LEFT OUTER JOIN dbo.MD_ATT stg_attr ON mapping.COLUMN_NAME = stg_attr.ATTRIBUTE_NAME");
                        prepareMappingStatement.AppendLine("JOIN MD_STG_SAT_XREF xref ON xref.STAGING_AREA_TABLE_ID = stg.STAGING_AREA_TABLE_ID");
                        prepareMappingStatement.AppendLine("JOIN MD_SAT sat ON xref.SATELLITE_TABLE_ID = sat.SATELLITE_TABLE_ID");
                        prepareMappingStatement.AppendLine("JOIN " + linkedServer + integrationDatabase + ".INFORMATION_SCHEMA.COLUMNS satatts");
                        prepareMappingStatement.AppendLine("on sat.SATELLITE_TABLE_NAME = satatts.TABLE_NAME");
                        prepareMappingStatement.AppendLine("and UPPER(mapping.COLUMN_NAME) = UPPER(satatts.COLUMN_NAME)");
                        prepareMappingStatement.AppendLine("WHERE mapping.COLUMN_NAME NOT IN");
                        prepareMappingStatement.AppendLine("  ( ");

                        prepareMappingStatement.AppendLine("  '" + recordSource + "',");
                        prepareMappingStatement.AppendLine("  '" + alternativeRecordSource + "',");
                        prepareMappingStatement.AppendLine("  '" + sourceRowId + "',");
                        prepareMappingStatement.AppendLine("  '" + recordChecksum + "',");
                        prepareMappingStatement.AppendLine("  '" + changeDataCaptureIndicator + "',");
                        prepareMappingStatement.AppendLine("  '" + hubAlternativeLdts + "',");
                        prepareMappingStatement.AppendLine("  '" + eventDateTimeAtttribute + "',");
                        prepareMappingStatement.AppendLine("  '" + effectiveDateTimeAttribute + "',");
                        prepareMappingStatement.AppendLine("  '" + etlProcessId + "',");
                        prepareMappingStatement.AppendLine("  '" + loadDateTimeStamp + "'");

                        prepareMappingStatement.AppendLine("  ) ");
                        prepareMappingStatement.AppendLine(")");
                        prepareMappingStatement.AppendLine("SELECT ");
                        prepareMappingStatement.AppendLine("	STAGING_AREA_TABLE_ID,");
                        prepareMappingStatement.AppendLine("	STAGING_AREA_TABLE_NAME,");
                        prepareMappingStatement.AppendLine("	SATELLITE_TABLE_ID,");
                        prepareMappingStatement.AppendLine("	SATELLITE_TABLE_NAME,");
                        prepareMappingStatement.AppendLine("	ATTRIBUTE_FROM_ID,");
                        prepareMappingStatement.AppendLine("	ATTRIBUTE_FROM_NAME,");
                        prepareMappingStatement.AppendLine("	ATTRIBUTE_TO_ID,");
                        prepareMappingStatement.AppendLine("	ATTRIBUTE_TO_NAME,");
                        prepareMappingStatement.AppendLine("	MULTI_ACTIVE_KEY_INDICATOR,");
                        prepareMappingStatement.AppendLine("	VERIFICATION");
                        prepareMappingStatement.AppendLine("FROM MAPPED_ATTRIBUTES");
                        prepareMappingStatement.AppendLine("UNION");
                        prepareMappingStatement.AppendLine("SELECT ");
                        prepareMappingStatement.AppendLine("	a.STAGING_AREA_TABLE_ID,");
                        prepareMappingStatement.AppendLine("	a.STAGING_AREA_TABLE_NAME,");
                        prepareMappingStatement.AppendLine("	a.SATELLITE_TABLE_ID,");
                        prepareMappingStatement.AppendLine("	a.SATELLITE_TABLE_NAME,");
                        prepareMappingStatement.AppendLine("	a.ATTRIBUTE_FROM_ID,");
                        prepareMappingStatement.AppendLine("	a.ATTRIBUTE_FROM_NAME,");
                        prepareMappingStatement.AppendLine("	a.ATTRIBUTE_TO_ID,");
                        prepareMappingStatement.AppendLine("	a.ATTRIBUTE_TO_NAME,");
                        prepareMappingStatement.AppendLine("	a.MULTI_ACTIVE_KEY_INDICATOR,");
                        prepareMappingStatement.AppendLine("	a.VERIFICATION");
                        prepareMappingStatement.AppendLine("FROM ORIGINAL_ATTRIBUTES a");
                        prepareMappingStatement.AppendLine("LEFT OUTER JOIN MAPPED_ATTRIBUTES b ");
                        prepareMappingStatement.AppendLine("	ON a.STAGING_AREA_TABLE_ID=b.STAGING_AREA_TABLE_ID ");
                        prepareMappingStatement.AppendLine("  AND a.SATELLITE_TABLE_ID=b.SATELLITE_TABLE_ID");
                        prepareMappingStatement.AppendLine("  AND a.ATTRIBUTE_FROM_ID=b.ATTRIBUTE_FROM_ID");
                        prepareMappingStatement.AppendLine("WHERE b.ATTRIBUTE_TO_ID IS NULL");
                    }
                    else
                    {
                        _alert.SetTextLogging("Commencing preparing the column-to-column mapping metadata based on the model metadata.\r\n");

                        prepareMappingStatement.AppendLine("WITH MAPPED_ATTRIBUTES AS ");
                        prepareMappingStatement.AppendLine("(");
                        prepareMappingStatement.AppendLine("SELECT  stg.STAGING_AREA_TABLE_ID");
                        prepareMappingStatement.AppendLine("	   ,stg.STAGING_AREA_TABLE_NAME");
                        prepareMappingStatement.AppendLine("       ,sat.SATELLITE_TABLE_ID");
                        prepareMappingStatement.AppendLine("	   ,sat.SATELLITE_TABLE_NAME");
                        prepareMappingStatement.AppendLine("	   ,stg_attr.ATTRIBUTE_ID AS ATTRIBUTE_FROM_ID");
                        prepareMappingStatement.AppendLine("	   ,stg_attr.ATTRIBUTE_NAME AS ATTRIBUTE_FROM_NAME");
                        prepareMappingStatement.AppendLine("       ,target_attr.ATTRIBUTE_ID AS ATTRIBUTE_TO_ID   ");
                        prepareMappingStatement.AppendLine("	   ,target_attr.ATTRIBUTE_NAME AS ATTRIBUTE_TO_NAME");
                        prepareMappingStatement.AppendLine("	   ,'N' as MULTI_ACTIVE_KEY_INDICATOR");
                        prepareMappingStatement.AppendLine("	   ,'manually_mapped' as VERIFICATION");
                        prepareMappingStatement.AppendLine("FROM dbo.MD_ATTRIBUTE_MAPPING mapping");
                        prepareMappingStatement.AppendLine("       LEFT OUTER JOIN dbo.MD_SAT sat on sat.SATELLITE_TABLE_NAME=mapping.TARGET_TABLE");
                        prepareMappingStatement.AppendLine("	   LEFT OUTER JOIN dbo.MD_ATT target_attr on mapping.TARGET_COLUMN = target_attr.ATTRIBUTE_NAME");
                        prepareMappingStatement.AppendLine("	   LEFT OUTER JOIN dbo.MD_STG stg on stg.STAGING_AREA_TABLE_NAME = mapping.SOURCE_TABLE");
                        prepareMappingStatement.AppendLine("	   LEFT OUTER JOIN dbo.MD_ATT stg_attr on mapping.SOURCE_COLUMN = stg_attr.ATTRIBUTE_NAME");
                        prepareMappingStatement.AppendLine("	   LEFT OUTER JOIN dbo.MD_TABLE_MAPPING table_mapping");
                        prepareMappingStatement.AppendLine("	     on mapping.TARGET_TABLE = table_mapping.INTEGRATION_AREA_TABLE");
                        prepareMappingStatement.AppendLine("	    and mapping.SOURCE_TABLE = table_mapping.STAGING_AREA_TABLE");
                        prepareMappingStatement.AppendLine("WHERE mapping.TARGET_TABLE NOT LIKE '" + dwhKeyIdentifier + "' AND mapping.TARGET_TABLE NOT LIKE '" + lnkTablePrefix + "'");
                        prepareMappingStatement.AppendLine("      AND mapping.VERSION_ID = " + versionId);
                        prepareMappingStatement.AppendLine("      AND table_mapping.VERSION_ID = " + versionId);
                        prepareMappingStatement.AppendLine("      AND table_mapping.GENERATE_INDICATOR = 'Y'");
                        prepareMappingStatement.AppendLine("),");
                        prepareMappingStatement.AppendLine("ORIGINAL_ATTRIBUTES AS");
                        prepareMappingStatement.AppendLine("(");
                        prepareMappingStatement.AppendLine("SELECT ");
                        prepareMappingStatement.AppendLine("	stg.STAGING_AREA_TABLE_ID,");
                        prepareMappingStatement.AppendLine("	stg.STAGING_AREA_TABLE_NAME,");
                        prepareMappingStatement.AppendLine("	sat.SATELLITE_TABLE_ID,");
                        prepareMappingStatement.AppendLine("	sat.SATELLITE_TABLE_NAME,");
                        prepareMappingStatement.AppendLine("	stg_attr.ATTRIBUTE_ID AS ATTRIBUTE_FROM_ID,");
                        prepareMappingStatement.AppendLine("	stg_attr.ATTRIBUTE_NAME AS ATTRIBUTE_FROM_NAME,");
                        prepareMappingStatement.AppendLine("	stg_attr.ATTRIBUTE_ID AS ATTRIBUTE_TO_ID,");
                        prepareMappingStatement.AppendLine("	stg_attr.ATTRIBUTE_NAME AS ATTRIBUTE_TO_NAME,");
                        prepareMappingStatement.AppendLine("	'N' as MULTI_ACTIVE_KEY_INDICATOR,");
                        prepareMappingStatement.AppendLine("	'automatically_mapped' AS VERIFICATION");
                        prepareMappingStatement.AppendLine("FROM MD_VERSION_ATTRIBUTE mapping");
                        prepareMappingStatement.AppendLine("LEFT OUTER JOIN MD_STG stg ON stg.STAGING_AREA_TABLE_NAME = mapping.TABLE_NAME");
                        prepareMappingStatement.AppendLine("LEFT OUTER JOIN MD_STG_SAT_XREF xref ON stg.STAGING_AREA_TABLE_ID = xref.STAGING_AREA_TABLE_ID");
                        prepareMappingStatement.AppendLine("LEFT OUTER JOIN MD_SAT sat ON xref.SATELLITE_TABLE_ID = sat.SATELLITE_TABLE_ID");
                        prepareMappingStatement.AppendLine("LEFT OUTER JOIN MD_ATT stg_attr on mapping.COLUMN_NAME = stg_attr.ATTRIBUTE_NAME");
                        prepareMappingStatement.AppendLine("JOIN MD_VERSION_ATTRIBUTE satatts");
                        prepareMappingStatement.AppendLine("    on sat.SATELLITE_TABLE_NAME=satatts.TABLE_NAME");
                        prepareMappingStatement.AppendLine("    and UPPER(mapping.COLUMN_NAME) = UPPER(satatts.COLUMN_NAME)");
                        prepareMappingStatement.AppendLine("WHERE mapping.COLUMN_NAME NOT IN");
                        prepareMappingStatement.AppendLine("  ( ");

                        prepareMappingStatement.AppendLine("  '" + recordSource + "',");
                        prepareMappingStatement.AppendLine("  '" + alternativeRecordSource + "',");
                        prepareMappingStatement.AppendLine("  '" + sourceRowId + "',");
                        prepareMappingStatement.AppendLine("  '" + recordChecksum + "',");
                        prepareMappingStatement.AppendLine("  '" + changeDataCaptureIndicator + "',");
                        prepareMappingStatement.AppendLine("  '" + hubAlternativeLdts + "',");
                        prepareMappingStatement.AppendLine("  '" + eventDateTimeAtttribute + "',");
                        prepareMappingStatement.AppendLine("  '" + effectiveDateTimeAttribute + "',");
                        prepareMappingStatement.AppendLine("  '" + etlProcessId + "',");
                        prepareMappingStatement.AppendLine("  '" + loadDateTimeStamp + "'");

                        prepareMappingStatement.AppendLine("  ) ");

                        prepareMappingStatement.AppendLine("AND mapping.VERSION_ID = " + versionId + " AND satatts.VERSION_ID = " + versionId);
                        prepareMappingStatement.AppendLine(")");
                        prepareMappingStatement.AppendLine("SELECT ");
                        prepareMappingStatement.AppendLine("	STAGING_AREA_TABLE_ID,");
                        prepareMappingStatement.AppendLine("	STAGING_AREA_TABLE_NAME,");
                        prepareMappingStatement.AppendLine("	SATELLITE_TABLE_ID,");
                        prepareMappingStatement.AppendLine("	SATELLITE_TABLE_NAME,");
                        prepareMappingStatement.AppendLine("	ATTRIBUTE_FROM_ID,");
                        prepareMappingStatement.AppendLine("	ATTRIBUTE_FROM_NAME,");
                        prepareMappingStatement.AppendLine("	ATTRIBUTE_TO_ID,");
                        prepareMappingStatement.AppendLine("	ATTRIBUTE_TO_NAME,");
                        prepareMappingStatement.AppendLine("	MULTI_ACTIVE_KEY_INDICATOR,");
                        prepareMappingStatement.AppendLine("	VERIFICATION");
                        prepareMappingStatement.AppendLine("FROM MAPPED_ATTRIBUTES");
                        prepareMappingStatement.AppendLine("UNION");
                        prepareMappingStatement.AppendLine("SELECT ");
                        prepareMappingStatement.AppendLine("	a.STAGING_AREA_TABLE_ID,");
                        prepareMappingStatement.AppendLine("	a.STAGING_AREA_TABLE_NAME,");
                        prepareMappingStatement.AppendLine("	a.SATELLITE_TABLE_ID,");
                        prepareMappingStatement.AppendLine("	a.SATELLITE_TABLE_NAME,");
                        prepareMappingStatement.AppendLine("	a.ATTRIBUTE_FROM_ID,");
                        prepareMappingStatement.AppendLine("	a.ATTRIBUTE_FROM_NAME,");
                        prepareMappingStatement.AppendLine("	a.ATTRIBUTE_TO_ID,");
                        prepareMappingStatement.AppendLine("	a.ATTRIBUTE_TO_NAME,");
                        prepareMappingStatement.AppendLine("	a.MULTI_ACTIVE_KEY_INDICATOR,");
                        prepareMappingStatement.AppendLine("	a.VERIFICATION");
                        prepareMappingStatement.AppendLine("FROM ORIGINAL_ATTRIBUTES a");
                        prepareMappingStatement.AppendLine("LEFT OUTER JOIN MAPPED_ATTRIBUTES b ");
                        prepareMappingStatement.AppendLine("	ON a.STAGING_AREA_TABLE_ID=b.STAGING_AREA_TABLE_ID ");
                        prepareMappingStatement.AppendLine("  AND a.SATELLITE_TABLE_ID=b.SATELLITE_TABLE_ID");
                        prepareMappingStatement.AppendLine("  AND a.ATTRIBUTE_FROM_ID=b.ATTRIBUTE_FROM_ID");
                        prepareMappingStatement.AppendLine("WHERE b.ATTRIBUTE_TO_ID IS NULL");
                    }

                    var listMappings = GetDataTable(ref connOmd, prepareMappingStatement.ToString());

                    if (listMappings.Rows.Count == 0)
                    {
                        _alert.SetTextLogging("-->  No column-to-column mappings were detected.\r\n");
                    }
                    else
                    {
                        foreach (DataRow tableName in listMappings.Rows)
                        {
                            using (var connection = new SqlConnection(metaDataConnection))
                            {

                                var insertMappingStatement = new StringBuilder();

                                insertMappingStatement.AppendLine("INSERT INTO [MD_STG_SAT_ATT_XREF]");
                                insertMappingStatement.AppendLine("( [STAGING_AREA_TABLE_ID],[SATELLITE_TABLE_ID],[ATTRIBUTE_ID_FROM],[ATTRIBUTE_ID_TO],[MULTI_ACTIVE_KEY_INDICATOR])");
                                insertMappingStatement.AppendLine("VALUES ('" +
                                                               tableName["STAGING_AREA_TABLE_ID"] + "'," +
                                                               tableName["SATELLITE_TABLE_ID"] + "," +
                                                               tableName["ATTRIBUTE_FROM_ID"] + "," +
                                                               tableName["ATTRIBUTE_TO_ID"] + ",'" +
                                                               tableName["MULTI_ACTIVE_KEY_INDICATOR"] + "')");

                                var command = new SqlCommand(insertMappingStatement.ToString(), connection);

                                try
                                {
                                    connection.Open();
                                    command.ExecuteNonQuery();
                                    mappingCounter++;
                                }
                                catch (Exception)
                                {
                                    _alert.SetTextLogging("-----> An issue has occurred mapping columns from table " + tableName["STAGING_AREA_TABLE_NAME"] + " to " + tableName["SATELLITE_TABLE_NAME"] + ". \r\n");
                                    if (tableName["ATTRIBUTE_FROM_ID"].ToString() == "")
                                    {
                                        _alert.SetTextLogging("Both attributes are NULL.");
                                    }
                                }
                            }
                        }
                    }

                    worker.ReportProgress(90);
                    _alert.SetTextLogging("-->  Processing " + mappingCounter + " attribute mappings\r\n");
                    _alert.SetTextLogging("Preparation of the column-to-column mapping metadata completed.\r\n");
                }
                catch (Exception ex)
                {
                    errorCounter++;
                    _alert.SetTextLogging("An issue has occured during preparation of the Satellite Attribute mapping metadata. Please check the Error Log for more details.\r\n");
                    errorLog.AppendLine("\r\nAn issue has occured during preparation of the Satellite Attribute mapping metadata: \r\n\r\n" + ex);
                }

                #endregion

                #region Degenerate attribute mapping 95%
                //13. Prepare degenerate attribute mapping
                _alert.SetTextLogging("\r\n");

                try
                {
                    var prepareDegenerateMappingStatement = new StringBuilder();
                    var degenerateMappingCounter = 1;

                    if (checkBoxIgnoreVersion.Checked)
                    {
                        _alert.SetTextLogging("Commencing preparing the degenerate column metadata using the database.\r\n");

                        prepareDegenerateMappingStatement.AppendLine("WITH MAPPED_ATTRIBUTES AS");
                        prepareDegenerateMappingStatement.AppendLine("(");
                        prepareDegenerateMappingStatement.AppendLine("SELECT  stg.STAGING_AREA_TABLE_ID");
                        prepareDegenerateMappingStatement.AppendLine("       ,lnk.LINK_TABLE_ID");
                        prepareDegenerateMappingStatement.AppendLine("	   ,stg_attr.ATTRIBUTE_ID AS ATTRIBUTE_FROM_ID");
                        prepareDegenerateMappingStatement.AppendLine("       ,target_attr.ATTRIBUTE_ID AS ATTRIBUTE_TO_ID   ");
                        prepareDegenerateMappingStatement.AppendLine("	   ,'manually_mapped' as VERIFICATION");
                        prepareDegenerateMappingStatement.AppendLine("FROM dbo.MD_ATTRIBUTE_MAPPING mapping");
                        prepareDegenerateMappingStatement.AppendLine("       LEFT OUTER JOIN dbo.MD_LINK lnk");
                        prepareDegenerateMappingStatement.AppendLine("	     on lnk.LINK_TABLE_NAME=mapping.TARGET_TABLE");
                        prepareDegenerateMappingStatement.AppendLine("	   LEFT OUTER JOIN dbo.MD_ATT target_attr");
                        prepareDegenerateMappingStatement.AppendLine("	     on mapping.TARGET_COLUMN = target_attr.ATTRIBUTE_NAME");
                        prepareDegenerateMappingStatement.AppendLine("	   LEFT OUTER JOIN dbo.MD_STG stg");
                        prepareDegenerateMappingStatement.AppendLine("	     on stg.STAGING_AREA_TABLE_NAME = mapping.SOURCE_TABLE");
                        prepareDegenerateMappingStatement.AppendLine("	   LEFT OUTER JOIN dbo.MD_ATT stg_attr");
                        prepareDegenerateMappingStatement.AppendLine("	     on mapping.SOURCE_COLUMN = stg_attr.ATTRIBUTE_NAME");
                        prepareDegenerateMappingStatement.AppendLine("WHERE TARGET_TABLE NOT LIKE '" + dwhKeyIdentifier + "' AND TARGET_TABLE LIKE '" + lnkTablePrefix + "'");
                        prepareDegenerateMappingStatement.AppendLine("AND VERSION_ID = " + versionId);
                        prepareDegenerateMappingStatement.AppendLine("),");
                        prepareDegenerateMappingStatement.AppendLine("ORIGINAL_ATTRIBUTES AS");
                        prepareDegenerateMappingStatement.AppendLine("(");
                        prepareDegenerateMappingStatement.AppendLine("SELECT ");
                        prepareDegenerateMappingStatement.AppendLine("	--TABLE_NAME, ");
                        prepareDegenerateMappingStatement.AppendLine("	--COLUMN_NAME, ");
                        prepareDegenerateMappingStatement.AppendLine("	stg.STAGING_AREA_TABLE_ID,");
                        prepareDegenerateMappingStatement.AppendLine("	lnk.LINK_TABLE_ID,");
                        prepareDegenerateMappingStatement.AppendLine("	stg_attr.ATTRIBUTE_ID AS ATTRIBUTE_FROM_ID,");
                        prepareDegenerateMappingStatement.AppendLine("	stg_attr.ATTRIBUTE_ID AS ATTRIBUTE_TO_ID,");
                        prepareDegenerateMappingStatement.AppendLine("	'automatically_mapped' AS VERIFICATION");
                        prepareDegenerateMappingStatement.AppendLine("FROM " + linkedServer + stagingDatabase + ".INFORMATION_SCHEMA.COLUMNS mapping");
                        prepareDegenerateMappingStatement.AppendLine("LEFT OUTER JOIN dbo.MD_STG stg");
                        prepareDegenerateMappingStatement.AppendLine("	on stg.STAGING_AREA_TABLE_NAME = mapping.TABLE_NAME");
                        prepareDegenerateMappingStatement.AppendLine("LEFT OUTER JOIN dbo.MD_ATT stg_attr");
                        prepareDegenerateMappingStatement.AppendLine("	on mapping.COLUMN_NAME = stg_attr.ATTRIBUTE_NAME");
                        prepareDegenerateMappingStatement.AppendLine("JOIN MD_STG_LINK_ATT_XREF stglnk");
                        prepareDegenerateMappingStatement.AppendLine("    on 	stg.STAGING_AREA_TABLE_ID = stglnk.STAGING_AREA_TABLE_ID");
                        prepareDegenerateMappingStatement.AppendLine("JOIN MD_LINK lnk");
                        prepareDegenerateMappingStatement.AppendLine("    on stglnk.LINK_TABLE_ID = lnk.LINK_TABLE_ID");
                        prepareDegenerateMappingStatement.AppendLine("JOIN " + linkedServer + integrationDatabase + ".INFORMATION_SCHEMA.COLUMNS lnkatts");
                        prepareDegenerateMappingStatement.AppendLine("    on lnk.LINK_TABLE_NAME=lnkatts.TABLE_NAME");
                        prepareDegenerateMappingStatement.AppendLine("    and UPPER(mapping.COLUMN_NAME) = UPPER(lnkatts.COLUMN_NAME)");
                        prepareDegenerateMappingStatement.AppendLine("WHERE mapping.COLUMN_NAME NOT IN ");
                        prepareDegenerateMappingStatement.AppendLine("  ( ");

                        prepareDegenerateMappingStatement.AppendLine("  '" + recordSource + "',");
                        prepareDegenerateMappingStatement.AppendLine("  '" + alternativeRecordSource + "',");
                        prepareDegenerateMappingStatement.AppendLine("  '" + sourceRowId + "',");
                        prepareDegenerateMappingStatement.AppendLine("  '" + recordChecksum + "',");
                        prepareDegenerateMappingStatement.AppendLine("  '" + changeDataCaptureIndicator + "',");
                        prepareDegenerateMappingStatement.AppendLine("  '" + hubAlternativeLdts + "',");
                        prepareDegenerateMappingStatement.AppendLine("  '" + eventDateTimeAtttribute + "',");
                        prepareDegenerateMappingStatement.AppendLine("  '" + effectiveDateTimeAttribute + "',");
                        prepareDegenerateMappingStatement.AppendLine("  '" + etlProcessId + "',");
                        prepareDegenerateMappingStatement.AppendLine("  '" + loadDateTimeStamp + "'");

                        prepareDegenerateMappingStatement.AppendLine("  ) ");
                        prepareDegenerateMappingStatement.AppendLine(")");
                        prepareDegenerateMappingStatement.AppendLine("SELECT ");
                        prepareDegenerateMappingStatement.AppendLine("	STAGING_AREA_TABLE_ID,");
                        prepareDegenerateMappingStatement.AppendLine("	LINK_TABLE_ID,");
                        prepareDegenerateMappingStatement.AppendLine("	ATTRIBUTE_FROM_ID,");
                        prepareDegenerateMappingStatement.AppendLine("	ATTRIBUTE_TO_ID");
                        prepareDegenerateMappingStatement.AppendLine("	--VERIFICATION");
                        prepareDegenerateMappingStatement.AppendLine("FROM MAPPED_ATTRIBUTES");
                        prepareDegenerateMappingStatement.AppendLine("UNION");
                        prepareDegenerateMappingStatement.AppendLine("SELECT ");
                        prepareDegenerateMappingStatement.AppendLine("	a.STAGING_AREA_TABLE_ID,");
                        prepareDegenerateMappingStatement.AppendLine("	a.LINK_TABLE_ID,");
                        prepareDegenerateMappingStatement.AppendLine("	a.ATTRIBUTE_FROM_ID,");
                        prepareDegenerateMappingStatement.AppendLine("	a.ATTRIBUTE_TO_ID");
                        prepareDegenerateMappingStatement.AppendLine("	--a.VERIFICATION");
                        prepareDegenerateMappingStatement.AppendLine("FROM ORIGINAL_ATTRIBUTES a");
                        prepareDegenerateMappingStatement.AppendLine("LEFT OUTER JOIN MAPPED_ATTRIBUTES b ");
                        prepareDegenerateMappingStatement.AppendLine("	ON a.STAGING_AREA_TABLE_ID=b.STAGING_AREA_TABLE_ID ");
                        prepareDegenerateMappingStatement.AppendLine("  AND a.LINK_TABLE_ID=b.LINK_TABLE_ID");
                        prepareDegenerateMappingStatement.AppendLine("  AND a.ATTRIBUTE_FROM_ID=b.ATTRIBUTE_FROM_ID");
                        prepareDegenerateMappingStatement.AppendLine("WHERE b.ATTRIBUTE_TO_ID IS NULL");
                    }
                    else
                    {
                        _alert.SetTextLogging("Commencing preparing the degenerate column metadata using model metadata.\r\n");

                        prepareDegenerateMappingStatement.AppendLine("WITH MAPPED_ATTRIBUTES AS");
                        prepareDegenerateMappingStatement.AppendLine("(");
                        prepareDegenerateMappingStatement.AppendLine("SELECT  stg.STAGING_AREA_TABLE_ID");
                        prepareDegenerateMappingStatement.AppendLine("       ,lnk.LINK_TABLE_ID");
                        prepareDegenerateMappingStatement.AppendLine("	   ,stg_attr.ATTRIBUTE_ID AS ATTRIBUTE_FROM_ID");
                        prepareDegenerateMappingStatement.AppendLine("       ,target_attr.ATTRIBUTE_ID AS ATTRIBUTE_TO_ID   ");
                        prepareDegenerateMappingStatement.AppendLine("	   ,'manually_mapped' as VERIFICATION");
                        prepareDegenerateMappingStatement.AppendLine("FROM dbo.MD_ATTRIBUTE_MAPPING mapping");
                        prepareDegenerateMappingStatement.AppendLine("       LEFT OUTER JOIN dbo.MD_LINK lnk");
                        prepareDegenerateMappingStatement.AppendLine("	     on lnk.LINK_TABLE_NAME=mapping.TARGET_TABLE");
                        prepareDegenerateMappingStatement.AppendLine("	   LEFT OUTER JOIN dbo.MD_ATT target_attr");
                        prepareDegenerateMappingStatement.AppendLine("	     on mapping.TARGET_COLUMN = target_attr.ATTRIBUTE_NAME");
                        prepareDegenerateMappingStatement.AppendLine("	   LEFT OUTER JOIN dbo.MD_STG stg");
                        prepareDegenerateMappingStatement.AppendLine("	     on stg.STAGING_AREA_TABLE_NAME = mapping.SOURCE_TABLE");
                        prepareDegenerateMappingStatement.AppendLine("	   LEFT OUTER JOIN dbo.MD_ATT stg_attr");
                        prepareDegenerateMappingStatement.AppendLine("	     on mapping.SOURCE_COLUMN = stg_attr.ATTRIBUTE_NAME");
                        prepareDegenerateMappingStatement.AppendLine("WHERE TARGET_TABLE NOT LIKE '" + dwhKeyIdentifier + "' AND TARGET_TABLE LIKE '" + lnkTablePrefix + "'");
                        prepareDegenerateMappingStatement.AppendLine("AND VERSION_ID = " + versionId);
                        prepareDegenerateMappingStatement.AppendLine("),");
                        prepareDegenerateMappingStatement.AppendLine("ORIGINAL_ATTRIBUTES AS");
                        prepareDegenerateMappingStatement.AppendLine("(");
                        prepareDegenerateMappingStatement.AppendLine("SELECT ");
                        prepareDegenerateMappingStatement.AppendLine("	--TABLE_NAME, ");
                        prepareDegenerateMappingStatement.AppendLine("	--COLUMN_NAME, ");
                        prepareDegenerateMappingStatement.AppendLine("	stg.STAGING_AREA_TABLE_ID,");
                        prepareDegenerateMappingStatement.AppendLine("	lnk.LINK_TABLE_ID,");
                        prepareDegenerateMappingStatement.AppendLine("	stg_attr.ATTRIBUTE_ID AS ATTRIBUTE_FROM_ID,");
                        prepareDegenerateMappingStatement.AppendLine("	stg_attr.ATTRIBUTE_ID AS ATTRIBUTE_TO_ID,");
                        prepareDegenerateMappingStatement.AppendLine("	'automatically_mapped' AS VERIFICATION");
                        prepareDegenerateMappingStatement.AppendLine("FROM MD_VERSION_ATTRIBUTE mapping");
                        prepareDegenerateMappingStatement.AppendLine("LEFT OUTER JOIN dbo.MD_STG stg ON stg.STAGING_AREA_TABLE_NAME = mapping.TABLE_NAME");
                        prepareDegenerateMappingStatement.AppendLine("LEFT OUTER JOIN dbo.MD_ATT stg_attr ON mapping.COLUMN_NAME = stg_attr.ATTRIBUTE_NAME");
                        prepareDegenerateMappingStatement.AppendLine("JOIN MD_STG_LINK_ATT_XREF stglnk ON stg.STAGING_AREA_TABLE_ID = stglnk.STAGING_AREA_TABLE_ID");
                        prepareDegenerateMappingStatement.AppendLine("JOIN MD_LINK lnk ON stglnk.LINK_TABLE_ID = lnk.LINK_TABLE_ID");
                        prepareDegenerateMappingStatement.AppendLine("JOIN MD_VERSION_ATTRIBUTE lnkatts");
                        prepareDegenerateMappingStatement.AppendLine("    on lnk.LINK_TABLE_NAME=lnkatts.TABLE_NAME");
                        prepareDegenerateMappingStatement.AppendLine("    and UPPER(mapping.COLUMN_NAME) = UPPER(lnkatts.COLUMN_NAME)");
                        prepareDegenerateMappingStatement.AppendLine("WHERE mapping.COLUMN_NAME NOT IN ");
                        prepareDegenerateMappingStatement.AppendLine("  ( ");

                        prepareDegenerateMappingStatement.AppendLine("  '" + recordSource + "',");
                        prepareDegenerateMappingStatement.AppendLine("  '" + alternativeRecordSource + "',");
                        prepareDegenerateMappingStatement.AppendLine("  '" + sourceRowId + "',");
                        prepareDegenerateMappingStatement.AppendLine("  '" + recordChecksum + "',");
                        prepareDegenerateMappingStatement.AppendLine("  '" + changeDataCaptureIndicator + "',");
                        prepareDegenerateMappingStatement.AppendLine("  '" + hubAlternativeLdts + "',");
                        prepareDegenerateMappingStatement.AppendLine("  '" + eventDateTimeAtttribute + "',");
                        prepareDegenerateMappingStatement.AppendLine("  '" + effectiveDateTimeAttribute + "',");
                        prepareDegenerateMappingStatement.AppendLine("  '" + etlProcessId + "',");
                        prepareDegenerateMappingStatement.AppendLine("  '" + loadDateTimeStamp + "'");

                        prepareDegenerateMappingStatement.AppendLine("  ) ");
                        prepareDegenerateMappingStatement.AppendLine("AND mapping.VERSION_ID = " + versionId + " AND lnkatts.VERSION_ID = " + versionId);
                        prepareDegenerateMappingStatement.AppendLine(")");
                        prepareDegenerateMappingStatement.AppendLine("SELECT ");
                        prepareDegenerateMappingStatement.AppendLine("	STAGING_AREA_TABLE_ID,");
                        prepareDegenerateMappingStatement.AppendLine("	LINK_TABLE_ID,");
                        prepareDegenerateMappingStatement.AppendLine("	ATTRIBUTE_FROM_ID,");
                        prepareDegenerateMappingStatement.AppendLine("	ATTRIBUTE_TO_ID");
                        prepareDegenerateMappingStatement.AppendLine("	--VERIFICATION");
                        prepareDegenerateMappingStatement.AppendLine("FROM MAPPED_ATTRIBUTES");
                        prepareDegenerateMappingStatement.AppendLine("UNION");
                        prepareDegenerateMappingStatement.AppendLine("SELECT ");
                        prepareDegenerateMappingStatement.AppendLine("	a.STAGING_AREA_TABLE_ID,");
                        prepareDegenerateMappingStatement.AppendLine("	a.LINK_TABLE_ID,");
                        prepareDegenerateMappingStatement.AppendLine("	a.ATTRIBUTE_FROM_ID,");
                        prepareDegenerateMappingStatement.AppendLine("	a.ATTRIBUTE_TO_ID");
                        prepareDegenerateMappingStatement.AppendLine("	--a.VERIFICATION");
                        prepareDegenerateMappingStatement.AppendLine("FROM ORIGINAL_ATTRIBUTES a");
                        prepareDegenerateMappingStatement.AppendLine("LEFT OUTER JOIN MAPPED_ATTRIBUTES b ");
                        prepareDegenerateMappingStatement.AppendLine("	ON a.STAGING_AREA_TABLE_ID=b.STAGING_AREA_TABLE_ID ");
                        prepareDegenerateMappingStatement.AppendLine("  AND a.LINK_TABLE_ID=b.LINK_TABLE_ID");
                        prepareDegenerateMappingStatement.AppendLine("  AND a.ATTRIBUTE_FROM_ID=b.ATTRIBUTE_FROM_ID");
                        prepareDegenerateMappingStatement.AppendLine("WHERE b.ATTRIBUTE_TO_ID IS NULL");
                    }
                    var listDegenerateMappings = GetDataTable(ref connOmd, prepareDegenerateMappingStatement.ToString());

                    if (listDegenerateMappings.Rows.Count == 0)
                    {
                        _alert.SetTextLogging("-->  No degenerate columns were detected.\r\n");
                    }
                    else
                    {
                        foreach (DataRow tableName in listDegenerateMappings.Rows)
                        {
                            using (var connection = new SqlConnection(metaDataConnection))
                            {
                                // _alert.SetTextLogging("--> " + tableName["SATELLITE_TABLE_NAME"] + "\r\n");

                                var insertDegenerateMappingStatement = new StringBuilder();

                                insertDegenerateMappingStatement.AppendLine("INSERT INTO [dbo].MD_STG_LINK_ATT_XREF");
                                insertDegenerateMappingStatement.AppendLine("( [STAGING_AREA_TABLE_ID] ,[LINK_TABLE_ID] ,[ATTRIBUTE_ID_FROM] ,[ATTRIBUTE_ID_TO] )");
                                insertDegenerateMappingStatement.AppendLine("VALUES ");
                                insertDegenerateMappingStatement.AppendLine("(");
                                insertDegenerateMappingStatement.AppendLine("  " + tableName["STAGING_AREA_TABLE_ID"] + ",");
                                insertDegenerateMappingStatement.AppendLine("  " + tableName["LINK_TABLE_ID"] + ",");
                                insertDegenerateMappingStatement.AppendLine("  " + tableName["ATTRIBUTE_FROM_ID"] + ",");
                                insertDegenerateMappingStatement.AppendLine("  " + tableName["ATTRIBUTE_TO_ID"]);
                                insertDegenerateMappingStatement.AppendLine(")");

                                var command = new SqlCommand(insertDegenerateMappingStatement.ToString(), connection);

                                try
                                {
                                    connection.Open();
                                    command.ExecuteNonQuery();
                                    degenerateMappingCounter++;
                                }
                                catch (Exception ex)
                                {
                                    errorCounter++;
                                    _alert.SetTextLogging("An issue has occured during preparation of the degenerate attribute metadata. Please check the Error Log for more details.\r\n");
                                    errorLog.AppendLine("\r\nAn issue has occured during preparation of the degenerate attribute metadata: \r\n\r\n" + ex);
                                }
                            }
                        }
                        _alert.SetTextLogging("-->  Processing " + degenerateMappingCounter + " degenerate columns\r\n");
                    }

                    worker.ReportProgress(95);

                    _alert.SetTextLogging("Preparation of the degenerate column metadata completed.\r\n");
                }
                catch (Exception ex)
                {
                    errorCounter++;
                    _alert.SetTextLogging("An issue has occured during preparation of the degenerate attribute metadata. Please check the Error Log for more details.\r\n");
                    errorLog.AppendLine("\r\nAn issue has occured during preparation of the degenerate attribute metadata: \r\n\r\n" + ex);
                }

                #endregion

                #region 14. Multi-Active Key - 97%

                //14. Handle the Multi-Active Key
                _alert.SetTextLogging("\r\n");


                try
                {
                    var prepareMultiKeyStatement = new StringBuilder();

                    if (checkBoxIgnoreVersion.Checked)
                    {
                        _alert.SetTextLogging("Commencing Multi-Active Key handling using database.\r\n");

                        prepareMultiKeyStatement.AppendLine("SELECT ");
                        prepareMultiKeyStatement.AppendLine("   u.STAGING_AREA_TABLE_ID,");
                        prepareMultiKeyStatement.AppendLine("	u.SATELLITE_TABLE_ID,");
                        prepareMultiKeyStatement.AppendLine("	sat.SATELLITE_TABLE_NAME,");
                        prepareMultiKeyStatement.AppendLine("	u.ATTRIBUTE_ID_FROM,");
                        prepareMultiKeyStatement.AppendLine("	u.ATTRIBUTE_ID_TO,");
                        prepareMultiKeyStatement.AppendLine("	att.ATTRIBUTE_NAME");
                        prepareMultiKeyStatement.AppendLine("FROM MD_STG_SAT_ATT_XREF u");
                        prepareMultiKeyStatement.AppendLine("INNER JOIN MD_SAT sat ON sat.SATELLITE_TABLE_ID=u.SATELLITE_TABLE_ID");
                        prepareMultiKeyStatement.AppendLine("INNER JOIN MD_ATT att ON att.ATTRIBUTE_ID = u.ATTRIBUTE_ID_TO");
                        prepareMultiKeyStatement.AppendLine("INNER JOIN ");
                        prepareMultiKeyStatement.AppendLine("(");
                        prepareMultiKeyStatement.AppendLine("  SELECT ");
                        prepareMultiKeyStatement.AppendLine("  	sc.name AS SATELLITE_TABLE_NAME,");
                        prepareMultiKeyStatement.AppendLine("  	C.name AS ATTRIBUTE_NAME");
                        prepareMultiKeyStatement.AppendLine("  FROM " + linkedServer + integrationDatabase + ".sys.index_columns A");
                        prepareMultiKeyStatement.AppendLine("  JOIN " + linkedServer + integrationDatabase + ".sys.indexes B");
                        prepareMultiKeyStatement.AppendLine("    ON A.object_id=B.object_id AND A.index_id=B.index_id");
                        prepareMultiKeyStatement.AppendLine("  JOIN " + linkedServer + integrationDatabase + ".sys.columns C");
                        prepareMultiKeyStatement.AppendLine("    ON A.column_id=C.column_id AND A.object_id=C.object_id");
                        prepareMultiKeyStatement.AppendLine("  JOIN " + linkedServer + integrationDatabase + ".sys.tables sc on sc.object_id = A.object_id");
                        prepareMultiKeyStatement.AppendLine("    WHERE is_primary_key=1");
                        prepareMultiKeyStatement.AppendLine("  AND C.name!='" + effectiveDateTimeAttribute + "' AND C.name!='" + currentRecordAttribute + "' AND C.name!='" + eventDateTimeAtttribute + "'");
                        prepareMultiKeyStatement.AppendLine("  AND C.name NOT LIKE '" + dwhKeyIdentifier + "'");
                        prepareMultiKeyStatement.AppendLine(") ddsub");
                        prepareMultiKeyStatement.AppendLine("ON sat.SATELLITE_TABLE_NAME=ddsub.SATELLITE_TABLE_NAME");
                        prepareMultiKeyStatement.AppendLine("AND att.ATTRIBUTE_NAME=ddsub.ATTRIBUTE_NAME");
                        prepareMultiKeyStatement.AppendLine("  WHERE ddsub.SATELLITE_TABLE_NAME LIKE '" + satTablePrefix + "' OR ddsub.SATELLITE_TABLE_NAME LIKE '" + lsatTablePrefix + "'");
                    }
                    else
                    {
                        _alert.SetTextLogging("Commencing Multi-Active Key handling using model metadata.\r\n");

                        prepareMultiKeyStatement.AppendLine("SELECT ");
                        prepareMultiKeyStatement.AppendLine("   u.STAGING_AREA_TABLE_ID,");
                        prepareMultiKeyStatement.AppendLine("	u.SATELLITE_TABLE_ID,");
                        prepareMultiKeyStatement.AppendLine("	sat.SATELLITE_TABLE_NAME,");
                        prepareMultiKeyStatement.AppendLine("	u.ATTRIBUTE_ID_FROM,");
                        prepareMultiKeyStatement.AppendLine("	u.ATTRIBUTE_ID_TO,");
                        prepareMultiKeyStatement.AppendLine("	att.ATTRIBUTE_NAME");
                        prepareMultiKeyStatement.AppendLine("FROM MD_STG_SAT_ATT_XREF u");
                        prepareMultiKeyStatement.AppendLine("INNER JOIN MD_SAT sat ON sat.SATELLITE_TABLE_ID=u.SATELLITE_TABLE_ID");
                        prepareMultiKeyStatement.AppendLine("INNER JOIN MD_ATT att ON att.ATTRIBUTE_ID = u.ATTRIBUTE_ID_TO");
                        prepareMultiKeyStatement.AppendLine("INNER JOIN ");
                        prepareMultiKeyStatement.AppendLine("(");
                        prepareMultiKeyStatement.AppendLine("	SELECT");
                        prepareMultiKeyStatement.AppendLine("		TABLE_NAME AS SATELLITE_TABLE_NAME,");
                        prepareMultiKeyStatement.AppendLine("		COLUMN_NAME AS ATTRIBUTE_NAME");
                        prepareMultiKeyStatement.AppendLine("	FROM MD_VERSION_ATTRIBUTE");
                        prepareMultiKeyStatement.AppendLine("	WHERE MULTI_ACTIVE_INDICATOR='Y'");
                        prepareMultiKeyStatement.AppendLine("	AND VERSION_ID=" + versionId);
                        prepareMultiKeyStatement.AppendLine(") sub");
                        prepareMultiKeyStatement.AppendLine("ON sat.SATELLITE_TABLE_NAME=sub.SATELLITE_TABLE_NAME");
                        prepareMultiKeyStatement.AppendLine("AND att.ATTRIBUTE_NAME=sub.ATTRIBUTE_NAME");
                    }

                    var listMultiKeys = GetDataTable(ref connOmd, prepareMultiKeyStatement.ToString());

                    if (listMultiKeys.Rows.Count == 0)
                    {
                        _alert.SetTextLogging("-->  No Multi-Active Keys were detected.\r\n");
                    }
                    else
                    {
                        foreach (DataRow tableName in listMultiKeys.Rows)
                        {
                            using (var connection = new SqlConnection(metaDataConnection))
                            {
                                _alert.SetTextLogging("-->  Processing the Multi-Active Key attribute " +
                                                      tableName["ATTRIBUTE_NAME"] + " for " +
                                                      tableName["SATELLITE_TABLE_NAME"] + "\r\n");

                                var updateMultiActiveKeyStatement = new StringBuilder();

                                updateMultiActiveKeyStatement.AppendLine("UPDATE [MD_STG_SAT_ATT_XREF]");
                                updateMultiActiveKeyStatement.AppendLine("SET MULTI_ACTIVE_KEY_INDICATOR='Y'");
                                updateMultiActiveKeyStatement.AppendLine("WHERE STAGING_AREA_TABLE_ID = " + tableName["STAGING_AREA_TABLE_ID"]);
                                updateMultiActiveKeyStatement.AppendLine("AND SATELLITE_TABLE_ID = " + tableName["SATELLITE_TABLE_ID"]);
                                updateMultiActiveKeyStatement.AppendLine("AND ATTRIBUTE_ID_FROM = " + tableName["ATTRIBUTE_ID_FROM"]);
                                updateMultiActiveKeyStatement.AppendLine("AND ATTRIBUTE_ID_TO = " + tableName["ATTRIBUTE_ID_TO"]);


                                var command = new SqlCommand(updateMultiActiveKeyStatement.ToString(), connection);

                                try
                                {
                                    connection.Open();
                                    command.ExecuteNonQuery();
                                }
                                catch (Exception ex)
                                {
                                    errorCounter++;
                                    _alert.SetTextLogging("An issue has occured during preparation of the Multi-Active key metadata. Please check the Error Log for more details.\r\n");
                                    errorLog.AppendLine("\r\nAn issue has occured during preparation of the Multi-Active key metadata: \r\n\r\n" + ex);
                                }
                            }
                        }
                    }
                    worker.ReportProgress(80);
                    _alert.SetTextLogging("Preparation of the Multi-Active Keys completed.\r\n");
                }
                catch (Exception ex)
                {
                    errorCounter++;
                    _alert.SetTextLogging("An issue has occured during preparation of the Multi-Active key metadata. Please check the Error Log for more details.\r\n");
                    errorLog.AppendLine("\r\nAn issue has occured during preparation of the Multi-Active key metadata: \r\n\r\n" + ex);
                }

                #endregion

                #region Driving Key preparation
                //13. Prepare driving keys
                _alert.SetTextLogging("\r\n");
                _alert.SetTextLogging("Commencing preparing the Driving Key metadata.\r\n");

                try
                {
                    var prepareDrivingKeyStatement = new StringBuilder();

                    prepareDrivingKeyStatement.AppendLine("SELECT DISTINCT");
                    prepareDrivingKeyStatement.AppendLine("    -- base.[TABLE_MAPPING_HASH]");
                    prepareDrivingKeyStatement.AppendLine("    --,base.[VERSION_ID]");
                    prepareDrivingKeyStatement.AppendLine("    --,base.[STAGING_AREA_TABLE]");
                    prepareDrivingKeyStatement.AppendLine("    --,base.[BUSINESS_KEY_ATTRIBUTE]");
                    prepareDrivingKeyStatement.AppendLine("       sat.SATELLITE_TABLE_ID");
                    prepareDrivingKeyStatement.AppendLine("    --,base.[INTEGRATION_AREA_TABLE] AS LINK_SATELLITE_TABLE_NAME");
                    prepareDrivingKeyStatement.AppendLine("    --,base.[FILTER_CRITERIA]");
                    prepareDrivingKeyStatement.AppendLine("    --,base.[DRIVING_KEY_ATTRIBUTE]");
                    prepareDrivingKeyStatement.AppendLine("      ,COALESCE(hubkey.HUB_TABLE_ID, (SELECT HUB_TABLE_ID FROM MD_HUB WHERE HUB_TABLE_NAME = 'Not applicable')) AS HUB_TABLE_ID");
                    prepareDrivingKeyStatement.AppendLine("    --,hub.[INTEGRATION_AREA_TABLE] AS [HUB_TABLE]");
                    prepareDrivingKeyStatement.AppendLine("FROM");
                    prepareDrivingKeyStatement.AppendLine("(");
                    prepareDrivingKeyStatement.AppendLine("       SELECT");
                    prepareDrivingKeyStatement.AppendLine("              STAGING_AREA_TABLE,");
                    prepareDrivingKeyStatement.AppendLine("              INTEGRATION_AREA_TABLE,");
                    prepareDrivingKeyStatement.AppendLine("              VERSION_ID,");
                    prepareDrivingKeyStatement.AppendLine("              CASE");
                    prepareDrivingKeyStatement.AppendLine("                     WHEN CHARINDEX('(', RTRIM(LTRIM(Split.a.value('.', 'VARCHAR(MAX)')))) > 0");
                    prepareDrivingKeyStatement.AppendLine("                     THEN RTRIM(LTRIM(Split.a.value('.', 'VARCHAR(MAX)')))");
                    prepareDrivingKeyStatement.AppendLine("                     ELSE REPLACE(RTRIM(LTRIM(Split.a.value('.', 'VARCHAR(MAX)'))), ')', '')");
                    prepareDrivingKeyStatement.AppendLine("              END AS BUSINESS_KEY_ATTRIBUTE--For Driving Key");
                    prepareDrivingKeyStatement.AppendLine("       FROM");
                    prepareDrivingKeyStatement.AppendLine("       (");
                    prepareDrivingKeyStatement.AppendLine("              SELECT STAGING_AREA_TABLE, INTEGRATION_AREA_TABLE, DRIVING_KEY_ATTRIBUTE, VERSION_ID, CONVERT(XML, '<M>' + REPLACE(DRIVING_KEY_ATTRIBUTE, ',', '</M><M>') + '</M>') AS DRIVING_KEY_ATTRIBUTE_XML");
                    prepareDrivingKeyStatement.AppendLine("              FROM");
                    prepareDrivingKeyStatement.AppendLine("              (");
                    prepareDrivingKeyStatement.AppendLine("                     SELECT DISTINCT STAGING_AREA_TABLE, INTEGRATION_AREA_TABLE, VERSION_ID, LTRIM(RTRIM(DRIVING_KEY_ATTRIBUTE)) AS DRIVING_KEY_ATTRIBUTE");
                    prepareDrivingKeyStatement.AppendLine("                     FROM MD_TABLE_MAPPING");
                    prepareDrivingKeyStatement.AppendLine("                     WHERE INTEGRATION_AREA_TABLE LIKE '" + lsatTablePrefix + "' AND DRIVING_KEY_ATTRIBUTE IS NOT NULL AND DRIVING_KEY_ATTRIBUTE != ''");
                    prepareDrivingKeyStatement.AppendLine("                     AND VERSION_ID =" + versionId);
                    prepareDrivingKeyStatement.AppendLine("                     AND [GENERATE_INDICATOR] = 'Y'");
                    prepareDrivingKeyStatement.AppendLine("              ) TableName");
                    prepareDrivingKeyStatement.AppendLine("       ) AS A CROSS APPLY DRIVING_KEY_ATTRIBUTE_XML.nodes('/M') AS Split(a)");
                    prepareDrivingKeyStatement.AppendLine(")  base");
                    prepareDrivingKeyStatement.AppendLine("LEFT JOIN[dbo].[MD_TABLE_MAPPING]");
                    prepareDrivingKeyStatement.AppendLine("        hub");
                    prepareDrivingKeyStatement.AppendLine(" ON  base.STAGING_AREA_TABLE=hub.STAGING_AREA_TABLE");
                    prepareDrivingKeyStatement.AppendLine(" AND hub.INTEGRATION_AREA_TABLE LIKE '" + hubTablePrefix + "'");
                    prepareDrivingKeyStatement.AppendLine("  AND base.BUSINESS_KEY_ATTRIBUTE=hub.BUSINESS_KEY_ATTRIBUTE");
                    prepareDrivingKeyStatement.AppendLine("LEFT JOIN MD_SAT sat");
                    prepareDrivingKeyStatement.AppendLine("  ON base.INTEGRATION_AREA_TABLE = sat.SATELLITE_TABLE_NAME");
                    prepareDrivingKeyStatement.AppendLine("LEFT JOIN MD_HUB hubkey");
                    prepareDrivingKeyStatement.AppendLine("  ON hub.INTEGRATION_AREA_TABLE = hubkey.HUB_TABLE_NAME");
                    prepareDrivingKeyStatement.AppendLine("WHERE base.VERSION_ID = " + versionId);
                    prepareDrivingKeyStatement.AppendLine("AND base.BUSINESS_KEY_ATTRIBUTE IS NOT NULL");
                    prepareDrivingKeyStatement.AppendLine("AND base.BUSINESS_KEY_ATTRIBUTE!=''");
                    prepareDrivingKeyStatement.AppendLine("AND [GENERATE_INDICATOR] = 'Y'");

                    var listDrivingKeys = GetDataTable(ref connOmd, prepareDrivingKeyStatement.ToString());

                    if (listDrivingKeys.Rows.Count == 0)
                    {
                        _alert.SetTextLogging("-->  No Driving Key based Link-Satellites were detected.\r\n");
                    }
                    else
                    {
                        foreach (DataRow tableName in listDrivingKeys.Rows)
                        {
                            using (var connection = new SqlConnection(metaDataConnection))
                            {
                                var insertDrivingKeyStatement = new StringBuilder();

                                insertDrivingKeyStatement.AppendLine("INSERT INTO [MD_DRIVING_KEY_XREF]");
                                insertDrivingKeyStatement.AppendLine("( [SATELLITE_TABLE_ID] ,[HUB_TABLE_ID] )");
                                insertDrivingKeyStatement.AppendLine("VALUES ");
                                insertDrivingKeyStatement.AppendLine("(");
                                insertDrivingKeyStatement.AppendLine("  " + tableName["SATELLITE_TABLE_ID"] + ",");
                                insertDrivingKeyStatement.AppendLine("  " + tableName["HUB_TABLE_ID"]);
                                insertDrivingKeyStatement.AppendLine(")");

                                var command = new SqlCommand(insertDrivingKeyStatement.ToString(), connection);

                                try
                                {
                                    connection.Open();
                                    command.ExecuteNonQuery();
                                }
                                catch (Exception ex)
                                {
                                    errorCounter++;
                                    _alert.SetTextLogging(
                                        "An issue has occured during preparation of the Driving Key metadata. Please check the Error Log for more details.\r\n");
                                    errorLog.AppendLine(
                                        "\r\nAn issue has occured during preparation of the Driving Key metadata: \r\n\r\n" +
                                        ex);
                                }
                            }
                        }
                    }

                    worker.ReportProgress(95);
                    _alert.SetTextLogging("Preparation of the degenerate column metadata completed.\r\n");

                }
                catch (Exception ex)
                {
                    errorCounter++;
                    _alert.SetTextLogging("An issue has occured during preparation of the Driving Key metadata. Please check the Error Log for more details.\r\n");
                    errorLog.AppendLine("\r\nAn issue has occured during preparation of the Driving Key metadata: \r\n\r\n" + ex);
                }

                #endregion


                //Completed

                if (errorCounter > 0)
                {
                    _alert.SetTextLogging("\r\nWarning! There were " + errorCounter + " error(s) found while processing the metadata.\r\n");
                    _alert.SetTextLogging("Please check the Error Log for details \r\n");
                    _alert.SetTextLogging("\r\n");
                    //_alert.SetTextLogging(errorLog.ToString());
                    using (var outfile = new StreamWriter(GlobalParameters.ConfigurationPath + @"\Error_Log.txt"))
                    {
                        outfile.Write(errorLog.ToString());
                        outfile.Close();
                    }
                }
                else
                {
                    _alert.SetTextLogging("\r\nNo errors were detected.\r\n");
                }



                worker.ReportProgress(100);
            }
        }
        private void buttonSave_Click(object sender, EventArgs e)
        {
            //Clear the information textbox
            richTextBoxInformation.Clear();

            //Instantiate the global configuration settings
            var configurationSettings = new ConfigurationSettings();

            //Clear out metadata, if selected
            if (checkBoxClearMetadata.Checked)
            {
                TruncateMetadata();
            }

            if (dataGridViewTableMetadata.RowCount > 0) //Check if there are rows available in the grid view
            {
                //Create a datatable containing the changes, to check if there are ones to begin with
                DataTable dataTableKeyChanges = ((DataTable)bindingSourceTableMetadata.DataSource).GetChanges();

                if (dataTableKeyChanges != null && (dataTableKeyChanges.Rows.Count > 0)) //Check if there are any changes made at all
                {
                    //Create a new version, if required
                    CreateNewEmptyModelMetadataVersion();

                    //Commit the save of the metadata in whatever is the right version after the version handling
                    var connOmd = new SqlConnection { ConnectionString = configurationSettings.ConnectionStringOmd };
                    var versionId = GetMaxVersionId(connOmd);
                    SaveModelTableMetadata(versionId, dataTableKeyChanges);
                }
                else
                {
                    richTextBoxInformation.Text += "No changes were detected in the metadata, so no changes were saved.\r\n";
                }
            }
            else
            {
                richTextBoxInformation.Text += "There is no metadata to save!";
            }
        }

        private void CreateNewEmptyModelMetadataVersion()
        {
            //This method creates a new version ID including a snapshot of the other two metadata tables (MD_TABLE_MAPPING and MD_ATTRIBUTE_MAPPING).

            var configurationSettings = new ConfigurationSettings();

            // Handle the version changes
            var versionSelection = "None";
            if (radiobuttonMajorRelease.Checked)
            {
                versionSelection = "Major";
            }
            if (radioButtonMinorRelease.Checked)
            {
                versionSelection = "Minor";
            }

            var connOmd = new SqlConnection {ConnectionString = configurationSettings.ConnectionStringOmd};
            var maxVersion = GetMaxVersionId(connOmd);
            var versionKeyValuePair = GetVersion(maxVersion, connOmd);
            var majorVersion = versionKeyValuePair.Key;
            var minorVersion = versionKeyValuePair.Value;

            if (versionSelection == "Major")
            {
                try
                {
                    majorVersion++;
                    minorVersion = 0;

                    //Creates a new version
                    SaveVersion(majorVersion, minorVersion);

                    // Ensure the mapping metadata is updated to the newly created version
                    ManageTableMappingVersion();
                    ManageAttributeMappingVersion();

                    //Refresh the UI to display the newly created version
                    trackBarVersioning.Maximum = GetMaxVersionId(connOmd);
                    trackBarVersioning.TickFrequency = GetVersionCount();
                    trackBarVersioning.Value = GetMaxVersionId(connOmd);
                }
                catch (Exception ex)
                {
                    richTextBoxInformation.Text += "An issue occured when saving a new version: " + ex;
                }
            }

            if (versionSelection == "Minor")
            {
                try
                {
                    minorVersion++;

                    //Creates a new version
                    SaveVersion(majorVersion, minorVersion);

                    // Ensure the mapping metadata is updated to the newly created version
                    ManageTableMappingVersion();
                    ManageAttributeMappingVersion();

                    //Refresh the UI to display the newly created version
                    trackBarVersioning.Maximum = GetMaxVersionId(connOmd);
                    trackBarVersioning.TickFrequency = GetVersionCount();
                    trackBarVersioning.Value = GetMaxVersionId(connOmd);
                }
                catch (Exception ex)
                {
                    richTextBoxInformation.Text += "An issue occured when saving a new version: " + ex;
                }
            }

        }

        internal void CreateNewModelMetadataVersion(int versionId)
        {
            var configurationSettings = new ConfigurationSettings();
            var repositoryTarget = configurationSettings.metadataRepositoryType;

            var insertQueryTables = new StringBuilder();

            foreach (DataGridViewRow row in dataGridViewTableMetadata.Rows)
            {
                if (!row.IsNewRow)
                {
                    var tableName = "";
                    var columnName = "";
                    var dataType = "";
                    var maxLength = 0;
                    var numericPrecision = 0;
                    var ordinalPosition = 0;
                    var primaryKeyIndicator = "";
                    var multiActiveIndicator = "";

                    if (row.Cells[0].Value != DBNull.Value)
                    {
                        tableName = (string) row.Cells[0].Value;
                    }

                    if (row.Cells[1].Value != DBNull.Value)
                    {
                        columnName = (string) row.Cells[1].Value;
                    }

                    if (row.Cells[2].Value != DBNull.Value)
                    {
                        dataType = (string) row.Cells[2].Value;
                    }

                    if (row.Cells[3].Value != DBNull.Value)
                    {
                        maxLength = (int) row.Cells[3].Value;
                    }

                    if (row.Cells[4].Value != DBNull.Value)
                    {
                        numericPrecision = (int) row.Cells[4].Value;
                    }

                    if (row.Cells[5].Value != DBNull.Value)
                    {
                        ordinalPosition = (int) row.Cells[5].Value;
                    }

                    if (row.Cells[6].Value != DBNull.Value)
                    {
                        primaryKeyIndicator = (string) row.Cells[6].Value;
                    }

                    if (row.Cells[7].Value != DBNull.Value)
                    {
                        multiActiveIndicator = (string) row.Cells[8].Value;
                    }

                    insertQueryTables.AppendLine("INSERT INTO MD_VERSION_ATTRIBUTE");
                    insertQueryTables.AppendLine("([VERSION_ID], [TABLE_NAME],[COLUMN_NAME],[DATA_TYPE],[CHARACTER_MAXIMUM_LENGTH],[NUMERIC_PRECISION], [ORDINAL_POSITION], [PRIMARY_KEY_INDICATOR], [MULTI_ACTIVE_INDICATOR])");
                    insertQueryTables.AppendLine("VALUES");
                    insertQueryTables.AppendLine("(" + versionId + ",'" + tableName + "','" + columnName + "','" +
                                                 dataType + "','" + maxLength + "','" + numericPrecision + "','" +
                                                 ordinalPosition + "','" + primaryKeyIndicator + "','" +
                                                 multiActiveIndicator + "')");

                }
            }

            // Execute the statement, if the repository is SQL Server
            // If the source is JSON this is done in separate calls for now
            if (repositoryTarget == "SQLServer")
            {
                if (insertQueryTables.ToString() == "")
                {
                    richTextBoxInformation.Text += "No new version was saved.\r\n";
                }
                else
                {
                    using (var connection = new SqlConnection(configurationSettings.ConnectionStringOmd))
                    {
                        var command = new SqlCommand(insertQueryTables.ToString(), connection);

                        try
                        {
                            connection.Open();
                            command.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            richTextBoxInformation.Text += "An issue has occurred: " + ex;
                        }
                    }
                }
            }
        }

        private void SaveModelTableMetadata(int versionId, DataTable dataTableChanges)
        {
            //Grabbing the generic settings from the main forms
            var configurationSettings = new ConfigurationSettings();
            var repositoryTarget = configurationSettings.metadataRepositoryType;

            var insertQueryTables = new StringBuilder();

            //If the save version radiobutton is selected it means either minor or major version is checked and a full new snapshot needs to be created first
            if (!radiobuttonNoVersionChange.Checked) 
            {
                CreateNewModelMetadataVersion(versionId);
            }
            //An in-place update (no change) to the existing version is done
            else
            {
                if ((dataTableChanges != null && (dataTableChanges.Rows.Count > 0))) //Check if there are any changes made at all
                {
                    foreach (DataRow row in dataTableChanges.Rows) //Loop through the detected changes
                    {

                        //Changed rows
                        if ((row.RowState & DataRowState.Modified) != 0) 
                        {
                            var hashKey = (string) row["VERSION_ATTRIBUTE_HASH"];
                            var tableName = (string)row["TABLE_NAME"];
                            var columnName = (string)row["COLUMN_NAME"];
                            var dataType = (string)row["DATA_TYPE"];
                            var maxLength = (string)row["CHARACTER_MAXIMUM_LENGTH"];
                            var numericPrecision = (string)row["NUMERIC_PRECISION"];
                            var ordinalPosition = (string)row["ORDINAL_POSITION"];
                            var primaryKeyIndicator = (string)row["PRIMARY_KEY_INDICATOR"];
                            var multiActiveIndicator = (string)row["MULTI_ACTIVE_INDICATOR"];
                            var versionKey = (string)row["VERSION_ID"];

                            if (repositoryTarget == "SQLServer")
                            {
                                insertQueryTables.AppendLine("UPDATE MD_VERSION_ATTRIBUTE");
                                insertQueryTables.AppendLine("SET "+
                                                             "  [TABLE_NAME] = '" + tableName +
                                                             "',[COLUMN_NAME] = '" + columnName +
                                                             "',[DATA_TYPE] = '" + dataType +
                                                             "',[CHARACTER_MAXIMUM_LENGTH] = '" + maxLength +
                                                             "',[NUMERIC_PRECISION] = '" + numericPrecision +
                                                             "',[ORDINAL_POSITION] = '" + ordinalPosition +
                                                             "',[PRIMARY_KEY_INDICATOR] = '" + primaryKeyIndicator +
                                                             "',[MULTI_ACTIVE_INDICATOR] = '" + multiActiveIndicator +
                                                             "'");
                                insertQueryTables.AppendLine("WHERE [VERSION_ATTRIBUTE_HASH] = '" + hashKey +
                                                             "' AND [VERSION_ID] = " + versionKey);
                            }
                            else if (repositoryTarget == "JSON") //Insert a new segment (row) in the JSON
                            {

                                try
                                {
                                    ModelMetadataJson[] jsonArray = JsonConvert.DeserializeObject<ModelMetadataJson[]>(File.ReadAllText(configurationSettings.ConfigurationPath + GlobalParameters.jsonModelMetadataFileName));

                                    var jsonHash = jsonArray.FirstOrDefault(obj => obj.versionAttributeHash == hashKey); //Retrieves the json segment in the file for the given hash returns value or NULL

                                    if (jsonHash.versionAttributeHash == "")
                                    {
                                        richTextBoxInformation.Text +=
                                            "The correct segment in the JSON file was not found.\r\n";
                                    }
                                    else
                                    {
                                        // Update the values in the JSON segment
                                        jsonHash.tableName = tableName;
                                        jsonHash.columnName = columnName;
                                        jsonHash.dataType = dataType;
                                        jsonHash.characterMaximumLength = maxLength;
                                        jsonHash.numericPrecision = numericPrecision;
                                        jsonHash.ordinalPosition = ordinalPosition;
                                        jsonHash.primaryKeyIndicator = primaryKeyIndicator;
                                        jsonHash.multiActiveIndicator = multiActiveIndicator;
                                    }

                                    string output = JsonConvert.SerializeObject(jsonArray, Formatting.Indented);
                                    File.WriteAllText(configurationSettings.ConfigurationPath + GlobalParameters.jsonModelMetadataFileName, output);
                                 }
                                catch (JsonReaderException ex)
                                {
                                    richTextBoxInformation.Text += "There were issues applying the JSON update.\r\n" + ex;
                                }
                            }
                            else
                            {
                                richTextBoxInformation.Text += "There were issues identifying the repository type to apply changes.\r\n";
                            }
                        }

                        // Insert new rows
                        if ((row.RowState & DataRowState.Added) != 0)
                        {
                            string tableName = "";
                            string columnName = "";
                            string dataType = "";
                            string maxLength = "0";
                            string numericPrecision = "0";
                            string ordinalPosition = "0";
                            string primaryKeyIndicator = "";
                            string multiActiveIndicator = "";
                       
                            if (row[0] != DBNull.Value)
                            {
                                tableName = (string)row[0];
                            }

                            if (row[1] != DBNull.Value)
                            {
                                columnName = (string)row[1];
                            }

                            if (row[2] != DBNull.Value)
                            {
                                dataType = (string)row[2];
                            }

                            if (row[3] != DBNull.Value)
                            {
                                maxLength = (string)row[3];
                            }

                            if (row[4] != DBNull.Value)
                            {
                                numericPrecision = (string)row[4];
                            }

                            if (row[5] != DBNull.Value)
                            {
                                ordinalPosition = (string)row[5];
                            }

                            if (row[6] != DBNull.Value)
                            {
                                primaryKeyIndicator = (string)row[6];
                            }

                            if (row[7] != DBNull.Value)
                            {
                                multiActiveIndicator = (string)row[7];
                            }

                            if (repositoryTarget == "SQLServer")
                            {
                                insertQueryTables.AppendLine("IF NOT EXISTS (SELECT * FROM [MD_VERSION_ATTRIBUTE] WHERE [VERSION_ID]= " +versionId + " AND [TABLE_NAME]='" + tableName + "' AND [COLUMN_NAME]='" + columnName +"')");
                                insertQueryTables.AppendLine("INSERT INTO [MD_VERSION_ATTRIBUTE]");
                                insertQueryTables.AppendLine("([VERSION_ID], [TABLE_NAME],[COLUMN_NAME],[DATA_TYPE],[CHARACTER_MAXIMUM_LENGTH],[NUMERIC_PRECISION], [ORDINAL_POSITION], [PRIMARY_KEY_INDICATOR], [MULTI_ACTIVE_INDICATOR])");
                                insertQueryTables.AppendLine("VALUES");
                                insertQueryTables.AppendLine("(" + versionId + ",'" + tableName + "','" + columnName +
                                                             "','" + dataType + "','" + maxLength + "','" +
                                                             numericPrecision + "','" + ordinalPosition + "','" +
                                                             primaryKeyIndicator + "','" + multiActiveIndicator + "')");
                            }
                            else if (repositoryTarget == "JSON") //Update the JSON
                            {
                                try
                                {
                                    //Generate a unique key using a hash
                                    var hashKey = CreateMd5(tableName + '|' + columnName + '|' + versionId);

                                    // Load the file
                                    ModelMetadataJson[] jsonArray = JsonConvert.DeserializeObject<ModelMetadataJson[]>(File.ReadAllText(configurationSettings.ConfigurationPath + GlobalParameters.jsonModelMetadataFileName));

                                    // Conver it into a JArray so segments can be added easily
                                    var jsonTableMappingFull = JArray.FromObject(jsonArray);

                                    JObject newJsonSegment = new JObject(
                                            new JProperty("versionAttributeHash", hashKey),
                                            new JProperty("tableName", tableName),
                                            new JProperty("columnName", columnName),
                                            new JProperty("dataType", dataType),
                                            new JProperty("characterMaximumLength", maxLength),
                                            new JProperty("numericPrecision", numericPrecision),
                                            new JProperty("ordinalPosition", ordinalPosition),
                                            new JProperty("primaryKeyIndicator", primaryKeyIndicator),
                                            new JProperty("multiActiveIndicator", multiActiveIndicator)
                                        );

                                    jsonTableMappingFull.Add(newJsonSegment);

                                    string output = JsonConvert.SerializeObject(jsonTableMappingFull, Formatting.Indented);
                                    File.WriteAllText(configurationSettings.ConfigurationPath + GlobalParameters.jsonModelMetadataFileName, output);

                                    //Making sure the hash key value is added to the datatable as well
                                    row[0] = hashKey;

                                }
                                catch (JsonReaderException ex)
                                {
                                    richTextBoxInformation.Text += "There were issues inserting the JSON segment / record.\r\n" + ex;
                                }
                            }
                            else
                            {
                                richTextBoxInformation.Text += "There were issues identifying the repository type to apply changes.\r\n";
                            }
                        }


                        //Deleted rows
                        if ((row.RowState & DataRowState.Deleted) != 0) 
                        {
                            var hashKey = row["VERSION_ATTRIBUTE_HASH", DataRowVersion.Original].ToString();
                            var versionKey = row["VERSION_ID", DataRowVersion.Original].ToString();

                            if (repositoryTarget == "SQLServer")
                            {
                                insertQueryTables.AppendLine("DELETE FROM MD_VERSION_ATTRIBUTE");
                                insertQueryTables.AppendLine("WHERE [VERSION_ATTRIBUTE_HASH] = '" + hashKey + "' AND [VERSION_ID] = " + versionKey);
                            }
                            else if (repositoryTarget == "JSON") //Remove a segment (row) from the JSON
                            {
                                try
                                {
                                    var jsonArray =
                                        JsonConvert.DeserializeObject<ModelMetadataJson[]>(
                                            File.ReadAllText(configurationSettings.ConfigurationPath +
                                                             GlobalParameters.jsonModelMetadataFileName)).ToList();

                                    //Retrieves the json segment in the file for the given hash returns value or NULL
                                    var jsonSegment = jsonArray.FirstOrDefault(obj => obj.versionAttributeHash == hashKey);

                                    jsonArray.Remove(jsonSegment);

                                    if (jsonSegment.versionAttributeHash == "")
                                    {
                                        richTextBoxInformation.Text += "The correct segment in the JSON file was not found.\r\n";
                                    }
                                    else
                                    {
                                        //Remove the segment from the JSON
                                        jsonArray.Remove(jsonSegment);
                                    }

                                    string output = JsonConvert.SerializeObject(jsonArray, Formatting.Indented);
                                    File.WriteAllText(configurationSettings.ConfigurationPath + GlobalParameters.jsonModelMetadataFileName,output);

                                }
                                catch (JsonReaderException ex)
                                {
                                    richTextBoxInformation.Text += "There were issues applying the JSON update.\r\n" + ex;
                                }
                            }
                            else
                            {
                                richTextBoxInformation.Text += "There were issues identifying the repository type to apply changes.\r\n";
                            }
                        }
                    } // All changes have been processed.

                    #region Statement execution
                    // Execute the statement, if the repository is SQL Server
                    // If the source is JSON this is done in separate calls for now
                    if (repositoryTarget == "SQLServer")
                    {
                        if (insertQueryTables.ToString() == null || insertQueryTables.ToString() == "")
                        {
                            richTextBoxInformation.Text += "No model metadata changes were saved.\r\n";
                        }
                        else
                        {
                            using (var connection = new SqlConnection(configurationSettings.ConnectionStringOmd))
                            {
                                var command = new SqlCommand(insertQueryTables.ToString(), connection);

                                try
                                {
                                    connection.Open();
                                    command.ExecuteNonQuery();
                                    richTextBoxInformation.Text += "The model metadata has been saved.\r\n";
                                    dataTableChanges.AcceptChanges();
                                    ((DataTable)bindingSourceTableMetadata.DataSource).AcceptChanges();
                                }
                                catch (Exception ex)
                                {
                                    richTextBoxInformation.Text += "An issue has occurred: " + ex;
                                }
                            }
                        }
                    }

                    //Committing the changes to the datatable
                    dataTableChanges.AcceptChanges();
                    ((DataTable)bindingSourceTableMetadata.DataSource).AcceptChanges();

                    //The JSON needs to be re-bound to the datatable / datagrid after being updated to allow all values to be present
                    if (repositoryTarget == "JSON")
                    {
                        BindModelMetadataJsonToDataTable();
                    }
                    #endregion
                }
            }


            richTextBoxInformation.Text += "The (physical) model metadata has been saved.\r\n";
        }

        private void BindModelMetadataJsonToDataTable()
        {
            var configurationSettings = new ConfigurationSettings();

            // Load the table mapping file, convert it to a DataTable and bind it to the source
            List<ModelMetadataJson> jsonArray = JsonConvert.DeserializeObject<List<ModelMetadataJson>>(File.ReadAllText(configurationSettings.ConfigurationPath + GlobalParameters.jsonModelMetadataFileName));
            DataTable dt = ConvertToDataTable(jsonArray);
            dt.AcceptChanges(); //Make sure the changes are seen as committed, so that changes can be detected later on
            dt.Columns[0].ColumnName = "VERSION_ATTRIBUTE_HASH";
            dt.Columns[1].ColumnName = "VERSION_ID";
            dt.Columns[2].ColumnName = "TABLE_NAME";
            dt.Columns[3].ColumnName = "COLUMN_NAME";
            dt.Columns[4].ColumnName = "DATA_TYPE";
            dt.Columns[5].ColumnName = "CHARACTER_MAXIMUM_LENGTH";
            dt.Columns[6].ColumnName = "NUMERIC_PRECISION";
            dt.Columns[7].ColumnName = "ORDINAL_POSITION";
            dt.Columns[8].ColumnName = "PRIMARY_KEY_INDICATOR";
            dt.Columns[9].ColumnName = "MULTI_ACTIVE_INDICATOR";
            bindingSourceTableMetadata.DataSource = dt;
        }

        private void FormModelMetadata_SizeChanged(object sender, EventArgs e)
        {
           // GridAutoLayout();
        }
        private DialogResult STAShowDialog(FileDialog dialog)
        {
            var state = new FormManageMetadata.DialogState { FileDialog = dialog };
            var t = new System.Threading.Thread(state.ThreadProcShowDialog);
            t.SetApartmentState(System.Threading.ApartmentState.STA);

            t.Start();
            t.Join();

            return state.DialogResult;
        }

        private void saveModelMetadataFileAsJSONToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                var configurationSettings = new ConfigurationSettings();

                var theDialog = new SaveFileDialog
                {
                    Title = @"Save Model Metadata File",
                    Filter = @"JSON files|*.json",
                    InitialDirectory = configurationSettings.ConfigurationPath //Application.StartupPath + @"\Configuration\"
                };

                var ret = STAShowDialog(theDialog);

                if (ret == DialogResult.OK)
                {
                    try
                    {
                        var chosenFile = theDialog.FileName;

                        DataTable gridDataTable = (DataTable)bindingSourceTableMetadata.DataSource;

                        gridDataTable.TableName = "ModelMetadata";

                        JArray outputFileArray = new JArray();
                        foreach (DataRow singleRow in gridDataTable.Rows)
                        {
                            JObject individualRow = JObject.FromObject(new
                            {
                                versionAttributeHash = singleRow[0].ToString(),
                                versionId = singleRow[1].ToString(),
                                tableName = singleRow[2].ToString(),
                                columnName = singleRow[3].ToString(),
                                dataType = singleRow[4].ToString(),
                                characterMaximumLength = singleRow[5].ToString(),
                                numericPrecision = singleRow[6].ToString(),
                                ordinalPosition = singleRow[7].ToString(),
                                primaryKeyIndicator = singleRow[8].ToString(),
                                multiActiveIndicator = singleRow[9].ToString()
                            });
                            outputFileArray.Add(individualRow);
                        }

                        string json = JsonConvert.SerializeObject(outputFileArray, Formatting.Indented);

                        File.WriteAllText(chosenFile, json);

                        richTextBoxInformation.Text = "The model metadata file " + chosenFile + " saved successfully.";
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("A problem occure when attempting to save the file to disk. The detail error message is: " + ex.Message);
            }
        }

        private void FormModelMetadata_ResizeEnd(object sender, EventArgs e)
        {

    
        }

        private void FormModelMetadata_Resize(object sender, EventArgs e)
        {
            GridAutoLayout();
        }

        private void openModelMetadataFileToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }
    }
}
