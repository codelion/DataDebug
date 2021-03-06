﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using Excel = Microsoft.Office.Interop.Excel;
using System.Reflection;
using DataDebugMethods;

namespace ErrorClassifier
{
    public partial class ExperimentTool : Form
    {
        public ExperimentTool()
        {
            InitializeComponent();
        }
        string[] lines = null;
        string errorTypesTable = "";
        int errorCount = 0;
        List<string> errorAddresses = null;
        
        string folderPath = "";

        string csvFilePath = null;
        string xlsFilePath = null;
        string arrFilePath = null;
        bool strawManDone = false;

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.ShowDialog();
            
            //After the file is selected, and it's a .csv, start parsing it.
            if (openFileDialog.FileName != "" && openFileDialog.FileName.Substring(openFileDialog.FileName.LastIndexOf(".")) == ".csv")
            {
                //Read in the file
                //string fileText = System.IO.File.ReadAllText(@openFileDialog.FileName);
                string[] fileLines = System.IO.File.ReadAllLines(@openFileDialog.FileName);
                lines = System.IO.File.ReadAllLines(@openFileDialog.FileName);
                List<int> inputIndices = new List<int>();
                List<int> outputIndices = new List<int>();
                for (int i = 0; i < fileLines.Length; i++)
                {
                    string line = fileLines[i];
                    string lineTokens = "";
                    if (i == 0)
                    {
                        int tokenIndex = 0;
                        while (line.Length > 0)
                        {
                            string token = chomp(ref line);
                            if (token.Length >= "Input".Length && token.Contains("Input"))
                            {
                                inputIndices.Add(tokenIndex);
                            }
                            if (token.Length >= "Answer".Length && token.Contains("Answer"))
                            {
                                outputIndices.Add(tokenIndex);
                            }
                            lineTokens += token + " | ";
                            tokenIndex++;
                        }
                        lineTokens += Environment.NewLine;
                        textBox1.AppendText(lineTokens);
                        textBox1.AppendText(Environment.NewLine + "inputIndices: ");
                        foreach (int inputIndex in inputIndices)
                        {
                            textBox1.AppendText(inputIndex + ", ");
                        }
                        textBox1.AppendText(Environment.NewLine + "outputIndices: ");
                        foreach (int outputIndex in outputIndices)
                        {
                            textBox1.AppendText(outputIndex + ", ");
                        }
                    }
                    else
                    {
                        textBox1.AppendText(Environment.NewLine);
                        List<string> tokensList = new List<string>();
                        while (line.Length > 0)
                        {
                            string token = chomp(ref line);
                            tokensList.Add(token);
                        }
                        string[] tokensArray = tokensList.ToArray();
                        foreach (int inputIndex in inputIndices)
                        {
                            tokensArray[inputIndex] = "INPUT: " + tokensArray[inputIndex];
                        }
                        foreach (int outputIndex in outputIndices)
                        {
                            tokensArray[outputIndex] = "OUTPUT: " + tokensArray[outputIndex];
                        }
                        foreach (string tok in tokensArray)
                        {
                            lineTokens += tok + " | ";
                        }
                        textBox1.AppendText(lineTokens + Environment.NewLine);
                    }
                }
            }
        }

        private string chomp(ref string line)
        {
            //line = line.Remove(0, 1); //remove the quotation mark in the beginning
            //string token = line.Substring(0, line.IndexOf("\"")); //get the token until the next quotation mark
            //line = line.Substring(line.IndexOf("\"") + 1); //remove the token from the line along with the following comma
            //return token;
            line = line.Remove(0, 1); //remove the quotation mark in the beginning
            string token = line.Substring(0, line.IndexOf("\"")); //get the token until the next quotation mark
            line = line.Substring(line.IndexOf("\"") + 1); //remove the token from the line along with the following comma
            if (line.Length > 0)
            {
                line = line.Remove(0, 1);
            }
            string[] results = new string[2];
            results[0] = token;
            results[1] = line;
            return token;
        }   //End chomp(string ref)

        private string[] chomp(string line)
        {
            line = line.Remove(0, 1); //remove the quotation mark in the beginning
            string token = line.Substring(0, line.IndexOf("\"")); //get the token until the next quotation mark
            line = line.Substring(line.IndexOf("\"") + 1); //remove the token from the line along with the following comma
            if (line.Length > 0)
            {
                line = line.Remove(0,1);
            }
            string[] results = new string[2];
            results[0] = token;
            results[1] = line;
            return results;
        }   //End chomp(string)

        private void chompButton_Click(object sender, EventArgs e)
        {
            string[] results = chomp(lines[1]);
            lines[1] = results[1];
            textBox1.AppendText(results[0] + Environment.NewLine);
        }  //End chompButton_Click

        private void selectFolder_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog selectFolderDialog = new FolderBrowserDialog();
            selectFolderDialog.ShowDialog();
            if (selectFolderDialog.SelectedPath == "")
            {
                return;
            }
            //a folder was chosen 
            folderPath = @selectFolderDialog.SelectedPath;
            textBox1.AppendText(Environment.NewLine + "Folder was selected: " + folderPath);
            textBox1.AppendText(Environment.NewLine + "Checking for necessary files");
            string[] csvFilePaths = Directory.GetFiles(folderPath, "*_results.csv");
            if (csvFilePaths.Length == 0)
            {
                textBox1.AppendText(Environment.NewLine + "ERROR: CSV file not found");
                return;
            }
            textBox1.AppendText(Environment.NewLine + "CSV: " + csvFilePaths[0]);
            csvFilePath = csvFilePaths[0];
            string[] arrFilePaths = Directory.GetFiles(folderPath, "*.arr");
            if (arrFilePaths.Length == 0)
            {
                textBox1.AppendText(Environment.NewLine + "ERROR: Array file not found");
                return;
            }
            textBox1.AppendText(Environment.NewLine + "Array file: " + arrFilePaths[0]);
            arrFilePath = arrFilePaths[0];

            //Look for xls or xlsx
            string[] xlsFilePaths = Directory.GetFiles(folderPath, "*.xls");
            string[] xlsxFilePaths = Directory.GetFiles(folderPath, "*.xlsx");
            if (xlsFilePaths.Length == 0 && xlsxFilePaths.Length == 0)
            {
                textBox1.AppendText(Environment.NewLine + "ERROR: XLS/XLSX file not found");
                return;
            }
            if (xlsxFilePaths.Length != 0)
            {
                textBox1.AppendText(Environment.NewLine + "Excel file: " + xlsxFilePaths[0]);
                xlsFilePath = xlsxFilePaths[0];
            }
            else
            {
                textBox1.AppendText(Environment.NewLine + "Excel file: " + xlsFilePaths[0]);
                xlsFilePath = xlsFilePaths[0];
            }
        }  //End selectFolder_Click

        private void generateFuzzed_Click(object sender, EventArgs e)
        {
            errorAddresses = new List<string>();
            string[] tokenHeadersArray = null;
            TurkJob[] turkJobs = TurkJob.DeserializeArray(arrFilePath); //Indexed by jobID, this holds the addresses of all the cells
            errorCount = 0;
            errorTypesTable = "Error Number\tJobID\tResponder\tCell Index\tMisplaced Decimal\tSign Omission\tDecimal Point Omission\t" +
            "Digit Repeat\tExtra Digit\tWrong Digit\tDigit Omission\tBlank Input\tDigit Transposition\tOther" + Environment.NewLine;
            
            // create new file
            Excel.Workbooks wbs = OpenExcelFile(xlsFilePath, new Excel.Application());
            Excel.Workbook wb = wbs[1];
            Excel.Worksheet ws = wb.Worksheets[1];

            textBox1.AppendText(Environment.NewLine + Environment.NewLine + "Parsing CSV file." + Environment.NewLine);
            
            //Parse csv file
            //Read in the file
            string[] fileLines = System.IO.File.ReadAllLines(csvFilePath);
            lines = System.IO.File.ReadAllLines(csvFilePath);
            int jobIdIndex = -1;
            List<int> inputIndices = new List<int>();
            List<int> answerIndices = new List<int>();
            for (int i = 0; i < fileLines.Length; i++)
            {
                string line = fileLines[i];
                string lineTokens = "Headers: ";
                List<string> tokenHeaders = new List<string>();
                if (i == 0)
                {
                    int tokenIndex = 0;
                    while (line.Length > 0)
                    {
                        string token = chomp(ref line);
                        if (token.Equals("Input.job_id"))
                        {
                            jobIdIndex = tokenIndex;
                        }
                        if (token.Length >= "Input.cell".Length && token.Contains("Input.cell"))
                        {
                            inputIndices.Add(tokenIndex);
                        }
                        if (token.Length >= "Answer.cell".Length && token.Contains("Answer.cell"))
                        {
                            answerIndices.Add(tokenIndex);
                        }
                        tokenHeaders.Add(token);
                        lineTokens += token + " | ";
                        tokenIndex++;
                    }
                    lineTokens += Environment.NewLine;
                    textBox1.AppendText("\t" + lineTokens);
                    tokenHeadersArray = tokenHeaders.ToArray();
                    textBox1.AppendText(Environment.NewLine + "\tinputIndices: ");
                    foreach (int inputIndex in inputIndices)
                    {
                        textBox1.AppendText(inputIndex + " ");
                    }
                    textBox1.AppendText(Environment.NewLine + "\tanswerIndices: ");
                    foreach (int outputIndex in answerIndices)
                    {
                        textBox1.AppendText(outputIndex + " ");
                    }
                    textBox1.AppendText(Environment.NewLine + Environment.NewLine + "Creating a new Excel file from each error:");
                }                
                else
                {
                    textBox1.AppendText(Environment.NewLine + Environment.NewLine);
                    List<string> tokensList = new List<string>();
                    int jobID = -1; 
                    while (line.Length > 0)
                    {
                        string token = chomp(ref line);
                        tokensList.Add(token);
                    }
                    string[] tokensArray = tokensList.ToArray();

                    jobID = int.Parse(tokensArray[jobIdIndex]);
                    string createdFiles = "";
                    for (int index = 0; index < 10; index++)
                    {
                        //if the input and the answer are different
                        if (!tokensArray[inputIndices[index]].Equals(tokensArray[answerIndices[index]]))
                        {
                            // get error cell's address -- look it up in turkJobs
                            TurkJob t = turkJobs[jobID];
                            string errorCellAddress = t.GetAddrAt(index);
                            if (errorCellAddress.Equals("ZAA221"))
                            {
                                continue;
                            }
                            
                            errorCount++;
                            
                            //Create a new Excel file for this error
                            string errorFileName = xlsFilePath.Substring(0, xlsFilePath.IndexOf(".xls")) + "_error_" + errorCount + xlsFilePath.Substring(xlsFilePath.IndexOf(".xls"));
                            
                            errorAddresses.Add(errorCellAddress);
                            
                            Excel.Range errorCell = ws.get_Range(errorCellAddress);
                            
                            //Store original value
                            var oldValue = errorCell.Value;
                            var errorCellOrigColor = errorCell.Interior.ColorIndex;

                            // modify
                            errorCell.Value = tokensArray[answerIndices[index]];
                            errorCell.Interior.Color = Color.Blue;

                            createdFiles += "\tCreated file " + errorFileName + Environment.NewLine;

                            // save
                            wb.SaveAs(errorFileName);
                            
                            //restore to original 
                            errorCell.Value = oldValue;
                            errorCell.Interior.ColorIndex = errorCellOrigColor;

                            //Classify error:
                            bool[] errorTypes = new bool[10];
                            bool errorIdentified = false;
                            if (DataDebugMethods.ErrorClassifiers.TestMisplacedDecimal(tokensArray[answerIndices[index]], tokensArray[inputIndices[index]]))
                            {
                                errorIdentified = true;
                                errorTypes[0] = true;
                            }
                            if (DataDebugMethods.ErrorClassifiers.TestSignOmission(tokensArray[answerIndices[index]], tokensArray[inputIndices[index]]))
                            {
                                errorIdentified = true;
                                errorTypes[1] = true;
                            }
                            if (DataDebugMethods.ErrorClassifiers.TestDecimalOmission(tokensArray[answerIndices[index]], tokensArray[inputIndices[index]]))
                            {
                                errorIdentified = true;
                                errorTypes[2] = true;
                            }
                            if (DataDebugMethods.ErrorClassifiers.TestDigitRepeat(tokensArray[answerIndices[index]], tokensArray[inputIndices[index]]))
                            {
                                errorIdentified = true;
                                errorTypes[3] = true;
                            }
                            if (DataDebugMethods.ErrorClassifiers.TestExtraDigit(tokensArray[answerIndices[index]], tokensArray[inputIndices[index]]))
                            {
                                errorIdentified = true;
                                errorTypes[4] = true;
                            }
                            if (DataDebugMethods.ErrorClassifiers.TestWrongDigit(tokensArray[answerIndices[index]], tokensArray[inputIndices[index]]))
                            {
                                errorIdentified = true;
                                errorTypes[5] = true;
                            }
                            if (DataDebugMethods.ErrorClassifiers.TestDigitOmission(tokensArray[answerIndices[index]], tokensArray[inputIndices[index]]))
                            {
                                errorIdentified = true;
                                errorTypes[6] = true;
                            }
                            if (DataDebugMethods.ErrorClassifiers.TestBlank(tokensArray[answerIndices[index]], tokensArray[inputIndices[index]]))
                            {
                                errorIdentified = true;
                                errorTypes[7] = true;
                            }
                            if (DataDebugMethods.ErrorClassifiers.TestDigitTransposition(tokensArray[answerIndices[index]], tokensArray[inputIndices[index]]))
                            {
                                errorIdentified = true;
                                errorTypes[8] = true;
                            }
                            if (errorIdentified == false)
                            {
                                errorTypes[9] = true;
                            }
                            string errorTypesString = "";
                            foreach (bool b in errorTypes)
                            {
                                if (b == true)
                                {
                                    errorTypesString += "1\t";
                                }
                                else
                                {
                                    errorTypesString += "0\t";
                                }
                            }
                            errorTypesString = errorTypesString.Remove(errorTypesString.Length - 1); //Remove the last tab character from the string
                            errorTypesTable += errorCount + "\t"+ jobID + "\t" + i + "\t" + index + "\t" + errorTypesString + Environment.NewLine;
                            tokensArray[answerIndices[index]] = "<" + tokensArray[answerIndices[index]] + ">";
                        }
                    }
                    textBox1.AppendText("JobID " + jobID + ", Responder " + i + ":" + Environment.NewLine + "Inputs:" + Environment.NewLine);
                    for (int ind = 0; ind < 10; ind++)
                    {
                        textBox1.AppendText(tokensArray[inputIndices[ind]] + "\t");
                    }
                    textBox1.AppendText(Environment.NewLine + "Answers:" + Environment.NewLine);
                    for (int ind = 0; ind < 10; ind++)
                    {
                        textBox1.AppendText(tokensArray[answerIndices[ind]] + "\t");
                    }
                    textBox1.AppendText(Environment.NewLine + createdFiles + Environment.NewLine + Environment.NewLine);
                }
            }
            textBox2.AppendText(errorTypesTable + Environment.NewLine);
            System.IO.File.WriteAllText(@folderPath + @"\ErrorTypesTable.xls", errorTypesTable);
            wb.Close(false);
            wbs.Close();
        } //end generateFuzzed_click

        static Excel.Workbooks OpenExcelFile(String xlfilename, Excel.Application app)
        {
            // open Excel file
            app.Workbooks.Open(xlfilename);
            return app.Workbooks;
        } //End OpenExcelFile

        private void runTool_Click(object sender, EventArgs e)
        {
            textBox1.AppendText("Opening original Excel file: " + xlsFilePath + Environment.NewLine);
            
            //Run the bootstrapping tool on original file (before any errors are introduced) and store the initial highlighting. 

            // Get current app
            Excel.Application app = Globals.ThisAddIn.Application;
            Excel.Workbook originalWB = app.Workbooks.Open(xlsFilePath, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value);
            textBox1.AppendText("Running bootstrap analysis." + Environment.NewLine);
            
            //Disable screen updating during perturbation and analysis to speed things up
            Globals.ThisAddIn.Application.ScreenUpdating = false;

            // Make a new analysisData object
            AnalysisData data = new AnalysisData(Globals.ThisAddIn.Application);
            data.worksheets = app.Worksheets;
            data.global_stopwatch.Reset();
            data.global_stopwatch.Start();

            // Construct a new tree every time the tool is run
            data.Reset();

            // Build dependency graph (modifies data)
            ConstructTree.constructTree(data, app);
            
            if (data.TerminalInputNodes().Length == 0)
            {
                System.Windows.Forms.MessageBox.Show("This spreadsheet has no input ranges.  Sorry, dude.");
                data.pb.Close();
                Globals.ThisAddIn.Application.ScreenUpdating = true;
                return;
            }

            // e * bootstrapMultiplier
            int bootstrapMultiplier = (int)numericUpDown1.Value;
            var NBOOTS = (int)(Math.Ceiling(bootstrapMultiplier * Math.Exp(1.0)));

            // Get bootstraps
            var scores = Analysis.Bootstrap(NBOOTS, data, app, true);

            List<string> originalHighlightAddresses = new List<string>();
            foreach (KeyValuePair<DataDebugMethods.TreeNode, int> pair in scores)
            {
                if (pair.Value != 0)
                {
                    originalHighlightAddresses.Add(pair.Key.getCOMObject().Address);
                }
            }

            // Color outputs
            Analysis.ColorOutliers(scores);

            // Enable screen updating when we're done
            Globals.ThisAddIn.Application.ScreenUpdating = true;
            textBox1.AppendText("Done." + Environment.NewLine);

            string outText = System.IO.File.ReadAllText(@folderPath + @"\ErrorTypesTable.xls");
            outText += Environment.NewLine + Environment.NewLine + "Bootstrap Results:" + Environment.NewLine + "Detected\tTotal Flagged\tTotal Newly Flagged\tTotal Inputs\tBootstraps" + Environment.NewLine;

            //int errorIndex = 0;
            string[] xlsFilePaths = Directory.GetFiles(folderPath, "*.xls");
            string[] xlsxFilePaths = Directory.GetFiles(folderPath, "*.xlsx");
            
            int errorsNotSkippedCount = 0; //This is the number of errors that we are trying to detect. (Number of errors that were not being highlighted by the initial run.)
            int errorsDetectedCount = 0; //This is the number of errors we are able to detect.
            int totalNewlyFlagged = 0; //This is the total number of newly flagged cells (ones that were not flagged on the original run, but were flagged on the fuzzed run.)

            //Run the bootstrapping tool on each fuzzed file (there is one for each error)
            for (int errorIndex = 1; errorIndex <= errorCount; errorIndex++)
            {
                string file = xlsFilePath.Substring(0, xlsFilePath.IndexOf(".xls")) + "_error_" + errorIndex + xlsFilePath.Substring(xlsFilePath.IndexOf(".xls"));
                if (file.Equals(xlsFilePath) || file.Contains("~$") || file.Contains("ErrorTypesTable.xls"))
                {
                    continue;
                }
                //Any cells flagged during the original run will be assumed to be correct. If an error happens to be in one of those cells, it will be skipped. 
                if (originalHighlightAddresses.Contains(errorAddresses[errorIndex - 1]))
                {
                    textBox3.AppendText("Error " + errorIndex + " was already highlighted in the original. Skipping." + Environment.NewLine);
                    outText += "Skipped." + Environment.NewLine;
                    continue;
                }
                errorsNotSkippedCount++;

                textBox1.AppendText("Error " + errorIndex + " out of " + errorAddresses.Count + "." + Environment.NewLine);
                textBox1.AppendText("\tOpening fuzzed Excel file: " + file + Environment.NewLine);
                Excel.Workbook wb = app.Workbooks.Open(file);
                Excel.Worksheet ws = wb.Worksheets[1];

                textBox1.AppendText("\tRunning analysis. Error was in cell " + errorAddresses[errorIndex - 1] + "." + Environment.NewLine);
                
                //Disable screen updating during perturbation and analysis to speed things up
                Globals.ThisAddIn.Application.ScreenUpdating = false;

                // Make a new analysisData object
                data = new AnalysisData(Globals.ThisAddIn.Application);
                data.worksheets = app.Worksheets;
                data.global_stopwatch.Reset();
                data.global_stopwatch.Start();

                // Construct a new tree every time the tool is run
                data.Reset();

                // Build dependency graph (modifies data)
                ConstructTree.constructTree(data, app);

                if (data.TerminalInputNodes().Length == 0)
                {
                    System.Windows.Forms.MessageBox.Show("This spreadsheet has no input ranges.  Sorry, dude.");
                    data.pb.Close();
                    Globals.ThisAddIn.Application.ScreenUpdating = true;
                    return;
                }

                // e * bootstrapMultiplier
                var NBOOTS1 = (int)(Math.Ceiling(bootstrapMultiplier * Math.Exp(1.0)));

                // Get bootstraps
                var scores1 = Analysis.Bootstrap(NBOOTS1, data, app, true);
                int countFlagged = 0;
                int countNewFlagged = 0;
                foreach (KeyValuePair<DataDebugMethods.TreeNode, int> pair in scores1)
                {
                    if (pair.Value != 0)
                    {
                        //See if it was flagged originally -- if yes, don't count it
                        if (originalHighlightAddresses.Contains(pair.Key.getCOMObject().Address))
                        {
                            //This flagged cell doesn't count because it was flagged in the original
                            countFlagged++;
                        }
                        else
                        {
                            countFlagged++;
                            countNewFlagged++;
                            totalNewlyFlagged++;
                        }
                    }
                }

                // Color outputs
                Analysis.ColorOutliers(scores1);

                // Enable screen updating when we're done
                Globals.ThisAddIn.Application.ScreenUpdating = true;
                Excel.Range errorAddress = ws.get_Range(errorAddresses[errorIndex - 1]);
                if (errorAddress.Interior.Color != 16711680)
                {
                    textBox3.AppendText("Error " + errorIndex + " DETECTED." + " Flagged " + countFlagged + " out of " + scores1.Count + " inputs." + "(" + NBOOTS1 + " bootstraps.) Newly flagged: " + countNewFlagged + Environment.NewLine);
                    outText += 1 + "\t" + countFlagged + "\t" + countNewFlagged + "\t" + scores1.Count + "\t" + NBOOTS1 + Environment.NewLine;
                    errorsDetectedCount++;
                }
                else
                {
                    textBox3.AppendText("Error " + errorIndex + " NOT detected." + " Flagged " + countFlagged + " out of " + scores1.Count + " inputs." + "(" + NBOOTS1 + " bootstraps.) Newly flagged: " + countNewFlagged + Environment.NewLine);
                    outText += 0 + "\t" + countFlagged + "\t" + countNewFlagged + "\t" + scores1.Count + "\t" + NBOOTS1 + Environment.NewLine;
                }
                textBox1.AppendText("Done." + Environment.NewLine);
                wb.SaveAs(xlsFilePath.Substring(0, xlsFilePath.IndexOf(".xls")) + "_error_" + errorIndex + "_NBOOTS_" + NBOOTS1 + xlsFilePath.Substring(xlsFilePath.IndexOf(".xls")));
                wb.Close(false);
            }
            outText += "Recall = " + ((double)errorsDetectedCount / errorsNotSkippedCount * 100.0) + "% (" + errorsDetectedCount + " out of " + errorsNotSkippedCount + " errors were detected.)" + Environment.NewLine + "Precision = " + ((double)errorsDetectedCount / totalNewlyFlagged * 100.0) + "% (" + errorsDetectedCount + " out of " + totalNewlyFlagged + " newly flagged cells were errors.)" + Environment.NewLine;
            textBox3.AppendText("Recall = " + ((double)errorsDetectedCount / errorsNotSkippedCount * 100.0) + "% (" + errorsDetectedCount + " out of " + errorsNotSkippedCount + " errors were detected.)" + Environment.NewLine + "Precision = " + ((double)errorsDetectedCount / totalNewlyFlagged * 100.0) + "% (" + errorsDetectedCount + " out of " + totalNewlyFlagged + " newly flagged cells were errors.)" + Environment.NewLine);
            originalWB.Close(false);
            System.IO.File.WriteAllText(@folderPath + @"\ErrorTypesTable.xls", outText);

            if (strawManCheckBox.Checked == true)
            {
                doStrawManTest();
                strawManCheckBox.Checked = false;
            }
        }

        private void strawMan_Click(object sender, EventArgs e)
        {
            doStrawManTest();
        } //end strawMan_Click()

        public void doStrawManTest()
        {
            textBox1.AppendText("Opening original Excel file: " + xlsFilePath + Environment.NewLine);
            
            //Run the z-score tool on original file (before any errors are introduced) and store the initial highlighting. 
            
            // Get current app
            Excel.Application app = Globals.ThisAddIn.Application;
            Excel.Workbook originalWB = app.Workbooks.Open(xlsFilePath, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value);
            
            textBox1.AppendText("Running z-score analysis." + Environment.NewLine);
            
            //Disable screen updating during perturbation and analysis to speed things up
            Globals.ThisAddIn.Application.ScreenUpdating = false;

            // Make a new analysisData object
            AnalysisData data = new AnalysisData(Globals.ThisAddIn.Application);
            data.worksheets = app.Worksheets;
            data.global_stopwatch.Reset();
            data.global_stopwatch.Start();

            // Construct a new tree every time the tool is run
            data.Reset();

            // Build dependency graph (modifies data)
            ConstructTree.constructTree(data, app);
            
            if (data.TerminalInputNodes().Length == 0)
            {
                System.Windows.Forms.MessageBox.Show("This spreadsheet has no input ranges.  Sorry, dude.");
                data.pb.Close();
                Globals.ThisAddIn.Application.ScreenUpdating = true;
                return;
            }

            List<string> originalHighlightAddresses = new List<string>();

            int outliersCountOrig = 0;
            double max_zscore_orig = -1;
            Excel.Range max_zscore_cell_orig = null;

            foreach (DataDebugMethods.TreeNode treeNode in data.TerminalInputNodes())
            {
                bool notNumeric = false;
                Excel.Range r = treeNode.getCOMObject();
                foreach (Excel.Range cell in r)
                {
                    if (cell.Value2 == null)
                    {
                        continue;
                    }
                    else
                    {
                        if (!ExcelParser.isNumeric(System.Convert.ToString(cell.Value2)))
                        {
                            notNumeric = true;
                        }
                    }
                }
                if (notNumeric)
                {
                    continue;
                }
                int size = r.Count;
                double mean = __mean(r);
                double variance = __variance(r);
                double standard_deviation = __standard_deviation(variance);


                foreach (Excel.Range cell in r)
                {
                    double z_score = 0;
                    if (cell.Value != null)
                    {
                        z_score = (cell.Value - mean) / standard_deviation;
                    }
                    if (Math.Abs(z_score) > max_zscore_orig)
                    {
                        max_zscore_orig = Math.Abs(z_score);
                        max_zscore_cell_orig = cell;
                    }
                    if (Math.Abs(z_score) > 3.0)
                    {
                        outliersCountOrig++;
                        originalHighlightAddresses.Add(cell.Address);
                        cell.Interior.Color = Color.Red;
                    }
                }
            }
            if (max_zscore_orig > 3.0)
            {
                max_zscore_cell_orig.Interior.Color = Color.MediumPurple;
            }
            data.pb.Close();

            // Enable screen updating when we're done
            Globals.ThisAddIn.Application.ScreenUpdating = true;
            textBox1.AppendText("Done." + Environment.NewLine);

            string outText = System.IO.File.ReadAllText(@folderPath + @"\ErrorTypesTable.xls");
            outText += Environment.NewLine + Environment.NewLine + "Z-Score results:" + Environment.NewLine + "Detected\tTotal Flagged Outliers\tNew Flagged Outliers" + Environment.NewLine;
            
            string[] xlsFilePaths = Directory.GetFiles(folderPath, "*.xls");
            string[] xlsxFilePaths = Directory.GetFiles(folderPath, "*.xlsx");

            int errorsNotSkippedCount = 0; //This is the number of errors that we are trying to detect. (Number of errors that were not being highlighted by the initial run.)
            int errorsDetectedCount = 0; //This is the number of errors we are able to detect.
            int totalNewlyFlagged = 0; //This is the total number of newly flagged cells (ones that were not flagged on the original run, but were flagged on the fuzzed run.)

            //Run the z-score tool on each fuzzed file (there is one for each error)
            for (int errorIndex = 1; errorIndex <= errorCount; errorIndex++)
            {
                string file = xlsFilePath.Substring(0, xlsFilePath.IndexOf(".xls")) + "_error_" + errorIndex + xlsFilePath.Substring(xlsFilePath.IndexOf(".xls"));
                if (file.Equals(xlsFilePath) || file.Contains("~$") || file.Contains("ErrorTypesTable.xls"))
                {
                    continue;
                }
                //Any cells flagged during the original run will be assumed to be correct. If an error happens to be in one of those cells, it will be skipped. 
                if (originalHighlightAddresses.Contains(errorAddresses[errorIndex - 1]))
                {
                    textBox3.AppendText("Error " + errorIndex + " was already highlighted in the original. Skipping." + Environment.NewLine);
                    outText += "Skipped." + Environment.NewLine;
                    continue;
                }
                errorsNotSkippedCount++;

                textBox1.AppendText("Error " + errorIndex + " out of " + errorAddresses.Count + "." + Environment.NewLine);
                textBox1.AppendText("\tOpening fuzzed Excel file: " + file + Environment.NewLine);
                Excel.Workbook wb = app.Workbooks.Open(file);
                Excel.Worksheet ws = wb.Worksheets[1];

                textBox1.AppendText("\tRunning z-score analysis. Error was in cell " + errorAddresses[errorIndex - 1] + "." + Environment.NewLine);
                
                //Disable screen updating during perturbation and analysis to speed things up
                Globals.ThisAddIn.Application.ScreenUpdating = false;

                // Make a new analysisData object
                data = new AnalysisData(Globals.ThisAddIn.Application);
                data.worksheets = app.Worksheets;
                data.global_stopwatch.Reset();
                data.global_stopwatch.Start();

                // Construct a new tree every time the tool is run
                data.Reset();

                // Build dependency graph (modifies data)
                ConstructTree.constructTree(data, app);

                if (data.TerminalInputNodes().Length == 0)
                {
                    System.Windows.Forms.MessageBox.Show("This spreadsheet has no input ranges.  Sorry, dude.");
                    data.pb.Close();
                    Globals.ThisAddIn.Application.ScreenUpdating = true;
                    return;
                }

                int outliersCount = 0;
                int outliersNewCount = 0; 
                double max_zscore = -1;
                Excel.Range max_zscore_cell = null;

                foreach (DataDebugMethods.TreeNode treeNode in data.TerminalInputNodes())
                {
                    Excel.Range r = treeNode.getCOMObject();
                    int size = r.Count;
                    double mean = __mean(r);
                    double variance = __variance(r);
                    double standard_deviation = __standard_deviation(variance);
                    
                    foreach (Excel.Range cell in r)
                    {
                        double z_score = 0;
                        try
                        {
                            z_score = (cell.Value - mean) / standard_deviation;
                        }
                        catch { }
                        if (Math.Abs(z_score) > max_zscore)
                        {
                            max_zscore = Math.Abs(z_score);
                            max_zscore_cell = cell;
                        }
                        if (Math.Abs(z_score) > 3.0)
                        {
                            //See if it was flagged originally -- if yes, don't count it
                            if (originalHighlightAddresses.Contains(cell.Address))
                            {
                                //This flagged cell doesn't count because it was flagged in the original
                                outliersCount++;
                            }
                            else
                            {
                                outliersCount++;
                                outliersNewCount++;
                                totalNewlyFlagged++;
                            }
                            cell.Interior.Color = Color.Red;
                        }
                    }
                }
                if (max_zscore > 3.0)
                {
                    max_zscore_cell.Interior.Color = Color.MediumPurple;
                }
                data.pb.Close();

                // Enable screen updating when we're done
                Globals.ThisAddIn.Application.ScreenUpdating = true;
                
                Excel.Range errorAddress = ws.get_Range(errorAddresses[errorIndex - 1]);
                if (errorAddress.Interior.Color != 16711680)
                {
                    textBox3.AppendText("Error " + errorIndex + " DETECTED. Outliers flagged: " + outliersCount + " (" + outliersNewCount + " new.)" + Environment.NewLine);
                    outText += 1 + "\t" + outliersCount + "\t" + outliersNewCount + Environment.NewLine;
                    errorsDetectedCount++;
                }
                else
                {
                    textBox3.AppendText("Error " + errorIndex + " NOT detected. Outliers flagged: " + outliersCount + " (" + outliersNewCount + " new.)" + Environment.NewLine);
                    outText += 0 + "\t" + outliersCount + "\t" + outliersNewCount + Environment.NewLine;
                }
                textBox1.AppendText("Done." + Environment.NewLine);
                wb.SaveAs(xlsFilePath.Substring(0, xlsFilePath.IndexOf(".xls")) + "_error_" + errorIndex + "_ZSCORES_" + xlsFilePath.Substring(xlsFilePath.IndexOf(".xls")));
                wb.Close(false);
            }
            outText += "Recall = " + ((double)errorsDetectedCount / errorsNotSkippedCount * 100.0) + "% (" + errorsDetectedCount + " out of " + errorsNotSkippedCount + " errors were detected.)" + Environment.NewLine + "Precision = " + ((double)errorsDetectedCount / totalNewlyFlagged * 100.0) + "% (" + errorsDetectedCount + " out of " + totalNewlyFlagged + " newly flagged cells were errors.)" + Environment.NewLine;
            textBox3.AppendText("Recall = " + ((double)errorsDetectedCount / errorsNotSkippedCount * 100.0) + "% (" + errorsDetectedCount + " out of " + errorsNotSkippedCount + " errors were detected.)" + Environment.NewLine + "Precision = " + ((double)errorsDetectedCount / totalNewlyFlagged * 100.0) + "% (" + errorsDetectedCount + " out of " + totalNewlyFlagged + " newly flagged cells were errors.)" + Environment.NewLine);
            originalWB.Close(false);

            System.IO.File.WriteAllText(@folderPath + @"\ErrorTypesTable.xls", outText);
        } //end doStrawManTest

        private Double __mean(Excel.Range r)
        {

            double sum = 0;
            foreach (Excel.Range cell in r)
            {
                try
                {
                    sum += cell.Value;
                }
                catch { }
            }
            return sum / r.Count;
        }

        //Standard deviation of the sample
        private Double __standard_deviation(double variance)
        {
            return Math.Sqrt(variance);
        }

        //Variance of the sample
        private Double __variance(Excel.Range r)
        {
            Double distance_sum_sq = 0;
            Double mymean = __mean(r);
            foreach (Excel.Range cell in r)
            {
                try
                {
                    distance_sum_sq += Math.Pow(mymean - cell.Value, 2);
                }
                catch {}
            }
            return distance_sum_sq / (r.Count - 1);
        }

        private void oldAnalysis_Click(object sender, EventArgs e)
        {
            //Run the old tool on original file (before any errors are introduced) and store the initial highlighting. 
            textBox1.AppendText("Opening original Excel file: " + xlsFilePath + Environment.NewLine);
            
            // Get current app
            Excel.Application app = Globals.ThisAddIn.Application;
            Excel.Workbook originalWB = app.Workbooks.Open(xlsFilePath, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value);
            
            textBox1.AppendText("Running old tool analysis." + Environment.NewLine);
            
            //Disable screen updating during perturbation and analysis to speed things up
            Globals.ThisAddIn.Application.ScreenUpdating = false;

            // Make a new analysisData object
            AnalysisData data = new AnalysisData(Globals.ThisAddIn.Application);
            data.worksheets = app.Worksheets;
            data.global_stopwatch.Reset();
            data.global_stopwatch.Start();

            // Construct a new tree every time the tool is run
            data.Reset();

            // Build dependency graph (modifies data)
            ConstructTree.constructTree(data, app);
            
            // Perturb data (modifies data)
            Analysis.perturbationAnalysis(data);


            //Find outliers (modifies data)
            Analysis.outlierAnalysis(data);

            List<string> originalHighlightAddresses = data.oldToolOutlierAddresses;

            // Enable screen updating when we're done
            Globals.ThisAddIn.Application.ScreenUpdating = true;
            textBox1.AppendText("Done. Highlighted " + originalHighlightAddresses.Count + " cells in original." + Environment.NewLine);

            string outText = System.IO.File.ReadAllText(@folderPath + @"\ErrorTypesTable.xls");
            outText += Environment.NewLine + Environment.NewLine + "Old tool results:" + Environment.NewLine;
            outText += "Detected\tTotal flagged\tNewly flagged" + Environment.NewLine;

            string[] xlsFilePaths = Directory.GetFiles(folderPath, "*.xls");
            string[] xlsxFilePaths = Directory.GetFiles(folderPath, "*.xlsx");

            int errorsNotSkippedCount = 0; //This is the number of errors that we are trying to detect. (Number of errors that were not being highlighted by the initial run.)
            int errorsDetectedCount = 0; //This is the number of errors we are able to detect.
            int totalNewlyFlagged = 0; //This is the total number of newly flagged cells (ones that were not flagged on the original run, but were flagged on the fuzzed run.)
            
            //Run the tool on each fuzzed file (there is one for each error)
            for (int errorIndex = 1; errorIndex <= errorCount; errorIndex++)
            {
                string file = xlsFilePath.Substring(0, xlsFilePath.IndexOf(".xls")) + "_error_" + errorIndex + xlsFilePath.Substring(xlsFilePath.IndexOf(".xls"));
                if (file.Equals(xlsFilePath) || file.Contains("~$") || file.Contains("ErrorTypesTable.xls")) //ignore the original file, the temp .xls file created by Excel (it contains an $ in the name), and the ErrorTypesTable file
                {
                    continue;
                }
                //Any cells flagged during the original run will be assumed to be correct. If an error happens to be in one of those cells, it will be skipped. 
                if (originalHighlightAddresses.Contains(errorAddresses[errorIndex - 1]))
                {
                    textBox3.AppendText("Error " + errorIndex + " was already highlighted in the original. Skipping." + Environment.NewLine);
                    outText += "Skipped." + Environment.NewLine;
                    continue;
                }
                errorsNotSkippedCount++;

                textBox1.AppendText("Error " + errorIndex + " out of " + errorAddresses.Count + "." + Environment.NewLine);
                textBox1.AppendText("\tOpening fuzzed Excel file: " + file + Environment.NewLine);
                Excel.Workbook wb = app.Workbooks.Open(file);
                Excel.Worksheet ws = wb.Worksheets[1];

                textBox1.AppendText("\tRunning old tool analysis. Error was in cell " + errorAddresses[errorIndex - 1] + "." + Environment.NewLine);
                
                //Disable screen updating during perturbation and analysis to speed things up
                Globals.ThisAddIn.Application.ScreenUpdating = false;

                // Make a new analysisData object
                data = new AnalysisData(Globals.ThisAddIn.Application);
                data.worksheets = app.Worksheets;
                data.global_stopwatch.Reset();
                data.global_stopwatch.Start();

                // Construct a new tree every time the tool is run
                data.Reset();

                // Build dependency graph (modifies data)
                ConstructTree.constructTree(data, app);

                // Perturb data (modifies data)
                Analysis.perturbationAnalysis(data);

                // Find outliers (modifies data)
                Analysis.outlierAnalysis(data);
                List<string> newOutlierAddresses = data.oldToolOutlierAddresses;
                
                int countFlagged = 0;
                int countNewFlagged = 0;
                foreach (string newAddress in newOutlierAddresses)
                {
                    //See if it was flagged originally -- if yes, don't count it
                    if (originalHighlightAddresses.Contains(newAddress))
                    {
                        //This flagged cell doesn't count because it was flagged in the original
                        countFlagged++;
                    }
                    else
                    {
                        countFlagged++;
                        countNewFlagged++;
                        totalNewlyFlagged++;
                    }
                }
                
                Globals.ThisAddIn.Application.ScreenUpdating = true;
                Excel.Range errorAddress = ws.get_Range(errorAddresses[errorIndex - 1]);
                if (errorAddress.Interior.Color != 16711680)
                {
                    textBox3.AppendText("Error " + errorIndex + " DETECTED. (Newly flagged: " + countNewFlagged + ")"  + Environment.NewLine);
                    outText += 1 + "\t" + countFlagged + "\t" + countNewFlagged + "\t" + Environment.NewLine;
                    errorsDetectedCount++;
                }
                else
                {
                    textBox3.AppendText("Error " + errorIndex + " NOT detected. (Newly flagged: " + countNewFlagged + ")" + Environment.NewLine);
                    outText += 0 + "\t" + countFlagged + "\t" + countNewFlagged + "\t" + Environment.NewLine;
                }
                textBox1.AppendText("Done." + Environment.NewLine);
                wb.SaveAs(xlsFilePath.Substring(0, xlsFilePath.IndexOf(".xls")) + "_error_" + errorIndex + "_OldTool_" + xlsFilePath.Substring(xlsFilePath.IndexOf(".xls")));
                wb.Close(false);
            }
            outText += "Recall = " + ((double)errorsDetectedCount/errorsNotSkippedCount*100.0) + "% (" + errorsDetectedCount + " out of " + errorsNotSkippedCount + " errors were detected.)" + Environment.NewLine + "Precision = " + ((double)errorsDetectedCount/totalNewlyFlagged*100.0) + "% (" + errorsDetectedCount + " out of " + totalNewlyFlagged + " newly flagged cells were errors.)" + Environment.NewLine;
            textBox3.AppendText("Recall = " + ((double)errorsDetectedCount/errorsNotSkippedCount*100.0) + "% (" + errorsDetectedCount + " out of " + errorsNotSkippedCount + " errors were detected.)" + Environment.NewLine + "Precision = " + ((double)errorsDetectedCount/totalNewlyFlagged*100.0) + "% (" + errorsDetectedCount + " out of " + totalNewlyFlagged + " newly flagged cells were errors.)" + Environment.NewLine);
            originalWB.Close(false);
            System.IO.File.WriteAllText(@folderPath + @"\ErrorTypesTable.xls", outText);

            if (strawManCheckBox.Checked == true)
            {
                doStrawManTest();
                strawManCheckBox.Checked = false;
            }
        } //End oldAnalysis_Click
    }
}
