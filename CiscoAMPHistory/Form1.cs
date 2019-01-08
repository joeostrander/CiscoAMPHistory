using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SQLite;
using System.Collections.Specialized;

namespace CiscoAMPHistory
{
    public partial class Form1 : Form
    {
        
        
        public delegate void AddDataGridViewRowCallback(DataGridView dgv, DataGridViewRow row);
        

        private string dbPath;

        private string strParentDir = Path.GetDirectoryName(Application.ExecutablePath) + "\\";
       
        NameValueCollection listActionType = new NameValueCollection()
        {
            { "1", "Create" },
            { "2", "Execute" },
            { "4", "Scan" },
            { "6", "Quarantine" },
            { "7", "Quarantine" },
            { "22", "Move" },
            { "40", "Open" }
        };

             

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.Text = Application.ProductName;
            
            dataGridView1.Columns.Add("columnCount", "#");
            dataGridView1.Columns.Add("columnPath", "Path");
            dataGridView1.Columns.Add("columnLastRef", "LastRef");
            dataGridView1.Columns.Add("columnDescription", "Description");
            dataGridView1.Columns.Add("columnHash", "Hash");

            dataGridView1.Columns["columnCount"].Width = 40;
            dataGridView1.Columns["columnPath"].Width = 350;
            dataGridView1.Columns["columnLastRef"].Width = 140;
            dataGridView1.Columns["columnDescription"].Width = 100;
            dataGridView1.Columns["columnHash"].Width = 200;



            AddDLL();

        }

        private void AddDLL()
        {
            string strCurrentPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Process);
            System.Environment.SetEnvironmentVariable("PATH", strParentDir + ";" + strCurrentPath, EnvironmentVariableTarget.Process);
            Directory.SetCurrentDirectory(strParentDir);


            string strFile1 = "System.Data.SQLite.dll";
            string strFile2 = "SQLite.Interop.dll";

            System.Reflection.AssemblyName file1;

            try
            {
                file1 = System.Reflection.AssemblyName.GetAssemblyName(strFile1);

            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception:  {0}", ex.Message);
                try
                {
                    try
                    {
                        //Create DLL from resources
                        if (!File.Exists(strParentDir + strFile2))
                        {
                            MemoryStream ms1 = new MemoryStream(Properties.Resources.System_Data_SQLite);
                            FileStream fs1 = File.OpenWrite(strParentDir + strFile1);
                            ms1.WriteTo(fs1);
                        }

                        if (!File.Exists(strParentDir + strFile2))
                        {
                            MemoryStream ms2 = new MemoryStream(Properties.Resources.SQLite_Interop);
                            FileStream fs2 = File.OpenWrite(strParentDir + strFile2);
                            ms2.WriteTo(fs2);
                        }

                        //file1 = System.Reflection.AssemblyName.GetAssemblyName(strFile1);
                        MessageBox.Show("DLLs created, please relaunch the application.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                        Application.Exit();
                    }
                    catch (Exception ex3)
                    {
                        //Do nothing?
                    }
                    
                }
                catch (Exception ex2)
                {
                    MessageBox.Show(ex.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    Application.Exit();
                }
            }



        }

        private void button1_Click(object sender, EventArgs e)
        {
            openFileDialog1.FileName = "";
            openFileDialog1.InitialDirectory = strParentDir;
            openFileDialog1.Title = "Select the Cisco AMP history file...";
            openFileDialog1.Filter = "historyex.db file | *.db";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                textBoxFilePath.Text = openFileDialog1.FileName;
            }
        }

        private void buttonLoadData_Click(object sender, EventArgs e)
        {
            LoadDataAsync();
        }

        private bool checkInputFile()
        {
            dbPath = textBoxFilePath.Text;

            if (!File.Exists(dbPath))
            {
                MessageBox.Show("Please select a valid historyex.db file!", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return false;
            }
            return true;
        }

        private void enableControls(bool enable)
        {
            textBoxFilePath.Enabled = enable;
            button1.Enabled = enable;
            buttonLoadData.Enabled = enable;
            dataGridView1.Enabled = enable;
        }


        private async Task LoadDataAsync()
        {
            
            dataGridView1.Rows.Clear();
            enableControls(false);
            if (checkInputFile())
            {
                await Task.Run(() => LoadData());
            }

            enableControls(true);
        }

        private async void LoadData() { 
            
       
            string connString = string.Format("Data Source={0}", dbPath);

            var sql = "SELECT path, hash, lastref, type FROM path_history ORDER by lastref desc";
            
           try
            {
           
                using (SQLiteConnection con = new SQLiteConnection())
                {
                    con.ConnectionString = connString;

                
                    using (SQLiteCommand cmd = new SQLiteCommand(sql, con))
                    {
                        con.Open();

                        int count = 0;

                        DataSet ds = new DataSet();
                        ds.Tables.Add("history");
                        ds.Tables["history"].Columns.Add("#");
                        ds.Tables["history"].Columns.Add("Path");
                        ds.Tables["history"].Columns.Add("LastRef");
                        ds.Tables["history"].Columns.Add("Description");
                        ds.Tables["history"].Columns.Add("Hash");


                        using (var dr = await cmd.ExecuteReaderAsync())
                        {

                            while (await dr.ReadAsync())
                            {

                                string strHash = dr.GetString(1);
                                long secs = (long)dr.GetValue(2);
                                DateTime dtLastRef = DateTimeOffset.FromUnixTimeSeconds(secs).LocalDateTime;

                                string desc = listActionType[dr.GetValue(3).ToString()];

                                count++;
                                DataGridViewRow row = new DataGridViewRow();
                                row.CreateCells(dataGridView1);
                                row.Cells[0].Value = count;
                                row.Cells[1].Value = "";
                                row.Cells[2].Value = dtLastRef;
                                row.Cells[3].Value = desc;
                                row.Cells[4].Value = strHash;




                                var path_numeric = dr.GetString(0);
                                string[] arrPath = path_numeric.Split('\\');

                                for (int i = 0; i < arrPath.Length; i++)
                                {
                                    // foreach (var path in arrPath)
                                    var path = arrPath[i];
                                    var sqlTmp = string.Format("select name from component where id={0} LIMIT 1", path);

                                    using (SQLiteCommand cmdTmp = new SQLiteCommand(sqlTmp, con))
                                    {
                                        using (var drTmp = await cmdTmp.ExecuteReaderAsync())
                                        {
                                            while (await drTmp.ReadAsync())
                                            {
                                                arrPath[i] = drTmp.GetString(0);
                                                //Console.WriteLine("??? {0}", drTmp.GetValue(0).ToString());
                                            }
                                        }
                                    }

                                }

                                string fullPath = string.Join("\\", arrPath);
                                row.Cells[1].Value = fullPath;


                                if (dataGridView1.InvokeRequired)
                                {
                                    dataGridView1.Invoke(new AddDataGridViewRowCallback(AddToDataGridView), new object[] { dataGridView1, row });
                                }
                                else
                                {
                                    AddToDataGridView(dataGridView1, row);

                                }
                            }
                        }




                    }



                }
            } 
            catch (Exception ex)
            {
                MessageBox.Show("Error:\n\n"+ex.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }

            
            

        }

        private void AddToDataGridView(DataGridView dgv, DataGridViewRow row)
        {
            dgv.Rows.Add(row);
        }

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length > 0)
            {
                textBoxFilePath.Text = files[0];

                LoadDataAsync();

            }
        }

        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutBox1 ab = new AboutBox1();
            ab.ShowDialog();
        }

        private void ExportToCSV(DataGridView dgv)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "CSV Files|*.csv";

            if (sfd.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            string strFilename = sfd.FileName;
            StreamWriter writer = new StreamWriter(strFilename);

            string strHeaders = "";

            foreach (DataGridViewColumn col in dgv.Columns)
            {
                strHeaders += "\"" + col.HeaderText + "\",";
            }
            strHeaders = strHeaders.TrimEnd(',');
            writer.WriteLine(strHeaders);

            foreach (DataGridViewRow row in dgv.Rows)
            {
                string strRowText = "";
                foreach (DataGridViewCell cell in row.Cells)
                {
                    strRowText += "\"" + cell.Value.ToString().Replace("\"", "\"\"") + "\",";
                }
                
                strRowText = strRowText.TrimEnd(',');
                writer.WriteLine(strRowText);
            }

            writer.Close();
            MessageBox.Show("See file:  " + strFilename, "CSV Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void exportToCSVToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ExportToCSV(dataGridView1);
        }
    }


}
