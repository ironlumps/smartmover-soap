using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using System.Net;
using System.Diagnostics;
using System.Data.OleDb;
using System.Globalization;


namespace SmartData
{
    public partial class Form1 : Form
    {
        // License String: DCkQd6ueSb72K87oKi-XlS**

        SmartMover.SmartMoverSOAP smartClient;
        SmartMover.Request reqArray;
        SmartMover.Response responseArray;
     
        // Smart Mover can handle 100 lines of data at a time
        int maxArraySize = 100;
        public Form1()
        {
            InitializeComponent();

            smartClient = new SmartMover.SmartMoverSOAP();
        }


        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                // Intialize the web service call and response 
                smartClient = new SmartData.SmartMover.SmartMoverSOAP();
                reqArray = new SmartData.SmartMover.Request();
                responseArray = new SmartData.SmartMover.Response();

                reqArray.CustomerID = textBox1.Text;

                StreamReader sr = new StreamReader(textBox2.Text);
                string fileName = textBox2.Text;
                string outputFile = fileName.Replace(".csv", "DateSmartOutput.csv");

               StreamWriter sw = new StreamWriter(outputFile);

                int recordCounter = 0;
                string input = "";

                reqArray.Records = new SmartMover.RequestRecord[maxArraySize];

                // Initialize the variables for counting and for timeout loop
                int increment = 0;
                bool timeout = false;
                double cm01Counter = 0;
                int cm02Counter = 0;
                int cm03Counter = 0;
                int cm04Counter = 0;
                float totalCounter = 0;
                List<int> dates = new List<int>();

                while ((input = sr.ReadLine()) != null)
                {
                    InsertRecord(input, recordCounter);
                    
                    recordCounter++;

                    if(recordCounter == maxArraySize)
                    {
                        // The do while is used to prevent against timeout exceptions for large data sets
                        do
                        {
                            try
                            {
                                // Used to check into the web service and actually verify the data
                                responseArray = smartClient.doSmartMover(reqArray);
                                timeout = false;
                            }
                            catch (Exception ex)
                            {
                                timeout = true;
                                increment++;
                                if (increment > 10)
                                    timeout = false;
                            }
                        } while (timeout == true);

                        if (responseArray.TransmissionResults != "")
                        {
                            MessageBox.Show(responseArray.TransmissionResults);
                        }

                      

                        for (int count = 0; count < recordCounter; count++)
                        {
                            // Writes the data from file and web service including the result codes to output file for
                            // each of the 100 lines at a time
                            sw.WriteLine(reqArray.Records[count].AddressLine1 + "," + reqArray.Records[count].NameFull + "," + reqArray.Records[count].City +
                                "," + reqArray.Records[count].State + "," + reqArray.Records[count].PostalCode + "," + responseArray.Records[count].AddressLine1
                                + "," + responseArray.Records[count].NameFull + "," + responseArray.Records[count].City + "," + responseArray.Records[count].State +
                                "," + responseArray.Records[count].PostalCode + "," + responseArray.Records[count].MoveEffectiveDate + "," + responseArray.Records[count].Results);

                            totalCounter++;
                            // Counts the amount of result codes and stores them into their variables
                            if (responseArray.Records[count].Results.Contains("CM01"))
                            {
                                cm01Counter++;
                            }
                            if (responseArray.Records[count].Results.Contains("CM02"))
                            {
                                cm02Counter++;
                            }
                            if (responseArray.Records[count].Results.Contains("CM03"))
                            {
                                cm03Counter++;
                            }
                            if (responseArray.Records[count].Results.Contains("CM04"))
                            {
                                cm04Counter++;
                            }
                            int x = 0;
                          
                            if(int.TryParse(responseArray.Records[count].MoveEffectiveDate, out x) == true)
                                dates.Add(int.Parse(responseArray.Records[count].MoveEffectiveDate));


                        }
                        // Resets the counter to start a new stack
                        recordCounter = 0;
                        // Resets the records to start a new stack
                        reqArray.Records = new SmartMover.RequestRecord[maxArraySize];
                    }
                }
                // Writyes the counter amounts and percentages to the file after the data has been processed 
                sw.WriteLine();
                sw.WriteLine("CM01: COA Match: " + " " + cm01Counter + "/" + totalCounter + " " + " " + "%" + String.Format("{0:P2}", cm01Counter/totalCounter));
                sw.WriteLine("CM02: Foreign Move: " + " " + cm02Counter + "/" + totalCounter + " " + " " + "%" + String.Format("{0:P2}", cm02Counter / totalCounter));
                sw.WriteLine("CM03: Moved no Forwarding: " + " " + cm03Counter + "/" + totalCounter + " " + " " + "%" + String.Format("{0:P2}", cm03Counter / totalCounter));
                sw.WriteLine("CM04: Box Closed" + " " + cm04Counter + "/" + totalCounter + " " + " " + "%" + String.Format("{0:P2}", cm04Counter / totalCounter));
                dates.Sort();
                Dictionary<int, int> dated = new Dictionary<int, int>();
                foreach(int x in dates)
                {
                    if(!dated.Keys.Contains<int>(x))
                    {
                        dated.Add(x, 1);

                    }
                    else
                    {
                        dated[x]++;
                    }
                }
                foreach(int x in dated.Keys)
                {
                    sw.WriteLine("Date: " + x + " " + " " + "Count: "+ dated[x] + " " +"Percent: " + String.Format("{0:P2}", dated[x] / cm01Counter));
                }
                sw.Close();
                sw.Dispose();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            Application.Exit();
        }
        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            // Opens file browser in order to choose a file for the data set
            OpenFileDialog fdlg = new OpenFileDialog();
            fdlg.InitialDirectory = System.IO.Path.GetDirectoryName(Application.ExecutablePath);

            fdlg.Title = "Browse File";

            if(fdlg.ShowDialog() == DialogResult.Cancel)
            {
                return;
            }
            try
            {
                textBox2.Text = fdlg.FileName;
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
      
        
        }
        // This method is used to split and organize the data from each line
        private void InsertRecord(String inputLine, int RecordCounter)
        {
            String FullName = "";
            String Address = "";
            String City = "";
            String State = "";
            String Zip = "";

            String[] values = inputLine.Split('|');

            FullName = values[1] + " " + values[2];
            Address = values[4];
            City = values[5];
            State = values[6];
            Zip = values[7];

            SmartMover.RequestRecord SMWSRequestRecord = new SmartMover.RequestRecord();
            // Assigns the values of the split line variables to the request counterpart so the webservice
            // knows what to process
            SMWSRequestRecord.NameFull = FullName;
            SMWSRequestRecord.AddressLine1 = Address;
            SMWSRequestRecord.City = City;
            SMWSRequestRecord.State = State;
            SMWSRequestRecord.PostalCode = Zip;


            reqArray.Records[RecordCounter] = SMWSRequestRecord;
        }
    }
}
