﻿using System;
using System.Collections.Generic;
using Microsoft.Office.Tools.Ribbon;
using Microsoft.Office.Tools.Excel;
using Excel = Microsoft.Office.Interop.Excel;
using DataDebugMethods;
using TreeNode = DataDebugMethods.TreeNode;
using TreeScore = System.Collections.Generic.Dictionary<DataDebugMethods.TreeNode, int>;
using ColorDict = System.Collections.Generic.Dictionary<Microsoft.Office.Interop.Excel.Workbook, System.Collections.Generic.List<DataDebugMethods.TreeNode>>;
using Microsoft.FSharp.Core;
using System.IO;
using System.Linq;

namespace DataDebug
{
    public partial class Ribbon
    {
        Dictionary<Excel.Workbook,List<RibbonHelper.CellColor>> color_dict; // list for storing colors
        Excel.Application app;
        Excel.Workbook current_workbook;

        private void Ribbon1_Load(object sender, RibbonUIEventArgs e)
        {
            // init color storage
            color_dict = new Dictionary<Excel.Workbook, List<RibbonHelper.CellColor>>();

            // Get current app
            app = Globals.ThisAddIn.Application;

            // Get current workbook
            //current_workbook = app.ActiveWorkbook;
            //if (current_workbook != null)
            //{
            //    color_dict.Add(current_workbook, RibbonHelper.SaveColors2(current_workbook));
            //}

            // register event handlers
            //app.WorkbookOpen += new Microsoft.Office.Interop.Excel.AppEvents_WorkbookOpenEventHandler(app_WorkbookOpen);
            app.WorkbookBeforeClose += new Microsoft.Office.Interop.Excel.AppEvents_WorkbookBeforeCloseEventHandler(app_WorkbookBeforeClose);
            //app.WorkbookActivate += new Microsoft.Office.Interop.Excel.AppEvents_WorkbookActivateEventHandler(app_WorkbookActivate);
        }

        //void app_WorkbookOpen(Excel.Workbook wb)
        //{
        //    current_workbook = wb;
        //    color_dict.Add(current_workbook, RibbonHelper.SaveColors2(current_workbook));
        //}

        void app_WorkbookBeforeClose(Excel.Workbook wb, ref bool cancel)
        {
            color_dict.Remove(wb);
            if (current_workbook == wb)
            {
                current_workbook = null;
            }
        }

        //void app_WorkbookActivate(Excel.Workbook wb)
        //{
        //    current_workbook = wb;
        //    if (!color_dict.ContainsKey(current_workbook))
        //    {
        //        color_dict.Add(current_workbook, RibbonHelper.SaveColors2(current_workbook));
        //    }
        //}

        // Action for "Analyze Worksheet" button
        private void button1_Click(object sender, RibbonControlEventArgs e)
        {
            current_workbook = app.ActiveWorkbook;
            try
            {
                if (current_workbook != null)
                {
                    RibbonHelper.RestoreColors2(color_dict[current_workbook]);
                    color_dict.Remove(current_workbook);
                }
            }
            catch { }

            if (!color_dict.ContainsKey(current_workbook))
            {
                color_dict.Add(current_workbook, RibbonHelper.SaveColors2(current_workbook));
            }

            //Disable screen updating during perturbation and analysis to speed things up
            app.ScreenUpdating = false;

            // Make a new analysisData object
            AnalysisData data = new AnalysisData(app);
            data.worksheets = app.Worksheets;
            data.global_stopwatch.Reset();
            data.global_stopwatch.Start();

            // Construct a new tree every time the tool is run
            data.Reset();

            //TODO This needs to be improved -- it doesn't make sense that it restores the colors to whatever they were when the workbook was opened. It removes any coloring changes the user has made since opening the file, which is not good.
            // reset colors
            //if (current_workbook != null)
            //{
            //    RibbonHelper.RestoreColors2(color_dict[current_workbook]);
            //}
            
            // Build dependency graph (modifies data)
            ConstructTree.constructTree(data, app);

            // Perturb data (modifies data)
            Analysis.perturbationAnalysis(data);
            
            // Find outliers (modifies data)
            Analysis.outlierAnalysis(data);

            string fileName = Globals.ThisAddIn.Application.ActiveWorkbook.Name;
            string folderPath = Globals.ThisAddIn.Application.ActiveWorkbook.Path;
            string reportsText = "";
            //If there is an existing report file, get its text, otherwise a new report file will be created.
            try
            {
                reportsText = System.IO.File.ReadAllText(@folderPath + @"\" + @fileName.Remove(fileName.LastIndexOf(".")) + " - Report.txt");
            }
            catch { }

            reportsText += "Worksheet Index\tAddress\tOriginal Color" + Environment.NewLine;
            foreach (string[] dataEntry in data.reportData)
            {
                foreach (string dataItem in dataEntry)
                {
                    reportsText += dataItem + "\t";
                }
                reportsText += Environment.NewLine;
            }
            if (folderPath == "")
            {
                System.Windows.Forms.MessageBox.Show("A report cannot be created because this file has not been saved yet.");
            }
            else
            {
                System.IO.File.WriteAllText(@folderPath + @"\" + @fileName.Remove(fileName.LastIndexOf(".")) + " - Report.txt", reportsText);
            }

            // Enable screen updating when we're done
            app.ScreenUpdating = true;
            if (showGVTree.Checked)
            {
                Display d = new Display();
                d.textBox1.Text = ConstructTree.GenerateGraphVizTree(data.formula_nodes);
                d.ShowDialog();
            }
        }

        // Button for outputting MTurk HIT CSVs
        private void button7_Click(object sender, RibbonControlEventArgs e)
        {
            // the longest input field we can represent on MTurk
            const int MAXLEN = 20;

            // get MTurk jobs or fail is spreadsheet data cells are too long
            TurkJob[] turkjobs;
            var turkjobs_opt = ConstructTree.DataForMTurk(Globals.ThisAddIn.Application, MAXLEN);
            if (FSharpOption<TurkJob[]>.get_IsSome(turkjobs_opt))
            {
                turkjobs = turkjobs_opt.Value;
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("This spreadsheet contains data items with lengths longer than " + MAXLEN + " characters and cannot be converted into an MTurk job.");
                return;
            }

            // get workbook name
            var wbname = app.ActiveWorkbook.Name;

            // prompt for folder name
            var sFD = new System.Windows.Forms.FolderBrowserDialog();
            sFD.ShowDialog();

            // If the path is not an empty string, go ahead
            if (sFD.SelectedPath != "")
            {
                // write key file
                var outfile = Path.Combine(sFD.SelectedPath, wbname + ".arr");
                TurkJob.SerializeArray(outfile, turkjobs);

                // write images, 2 for each TurkJob
                foreach (TurkJob tj in turkjobs)
                {
                    tj.WriteAsImages(sFD.SelectedPath, wbname);
                }

                // write CSV
                var csvfile = Path.Combine(sFD.SelectedPath, wbname + ".csv");
                var lines = new List<string>();
                lines.Add(turkjobs[0].ToCSVHeaderLine(wbname));
                lines.AddRange(turkjobs.Select(turkjob => turkjob.ToCSVLine(wbname)));
                File.WriteAllLines(csvfile, lines);
            }
        }

        private void TestNewProcedure_Click(object sender, RibbonControlEventArgs e)
        {
            current_workbook = app.ActiveWorkbook;
            try
            {
                if (current_workbook != null)
                {
                    RibbonHelper.RestoreColors2(color_dict[current_workbook]);
                    color_dict.Remove(current_workbook);
                }
            }
            catch { }

            if (!color_dict.ContainsKey(current_workbook))
            {
                color_dict.Add(current_workbook, RibbonHelper.SaveColors2(current_workbook));
            }
            
            // Disable screen updating during perturbation and analysis to speed things up
            app.ScreenUpdating = false;

            //TODO This needs to be improved -- it doesn't make sense that it restores the colors to whatever they were when the workbook was opened. It removes any coloring changes the user has made since opening the file, which is not good.
            // reset colors
            //if (current_workbook != null)
            //{
            //    RibbonHelper.RestoreColors2(color_dict[current_workbook]);
            //}

            // Make a new analysisData object
            AnalysisData data = new AnalysisData(app);
            data.worksheets = app.Worksheets;

            // Construct a new tree every time the tool is run
            data.Reset();

            // Build dependency graph (modifies data)
            ConstructTree.constructTree(data, app);

            if (data.TerminalInputNodes().Length == 0)
            {
                System.Windows.Forms.MessageBox.Show("There are no ranges that can be analyzed in this spreadsheet.");
                data.pb.Close();
                app.ScreenUpdating = true;
                return;
            }

            // e * 1000
            var NBOOTS = (int)(Math.Ceiling(1000 * Math.Exp(1.0)));

            // Get bootstraps
            var scores = Analysis.Bootstrap(NBOOTS, data, app, this.weighted.Checked);
            
            // Color outputs
            Analysis.ColorOutliers(scores);

            // Enable screen updating when we're done
            app.ScreenUpdating = true;
        }

        private void performanceExperiments_Click(object sender, RibbonControlEventArgs e)
        {
            PerformanceExperiments experimentsForm = new PerformanceExperiments();
            experimentsForm.ShowDialog();
        }

        private void countFormulas_Click(object sender, RibbonControlEventArgs e)
        {
            int countFormulas = 0;
            foreach (Excel.Range cell in current_workbook.ActiveSheet.UsedRange)
            {
                if (cell.HasFormula)
                {
                    countFormulas++;
                }
            }
            System.Windows.Forms.MessageBox.Show(countFormulas + " formulas in this workbook.");
        }
        private void undoButton_Click(object sender, RibbonControlEventArgs e)
        {
            string fileName = Globals.ThisAddIn.Application.ActiveWorkbook.Name;
            string folderPath = Globals.ThisAddIn.Application.ActiveWorkbook.Path;
            string reportsText = "";
            try
            {
                reportsText = System.IO.File.ReadAllText(@folderPath + @"\" + @fileName.Remove(fileName.LastIndexOf(".")) + " - Report.txt");
            }
            catch 
            { 
                return; 
            }

            int startIndex = reportsText.LastIndexOf("Worksheet Index\tAddress\tOriginal Color" + Environment.NewLine);
            
            //If the reports file is empty, there is nothing more to undo
            if (startIndex == -1)
            {
                return;
            }
            //Restore colors will go here

            string lastReport = reportsText.Substring(startIndex);
            //string[] lastReportLines = lastReport.lin
            System.Windows.Forms.MessageBox.Show("Last report: " + Environment.NewLine + lastReport);
            reportsText = reportsText.Remove(startIndex);
            System.Windows.Forms.MessageBox.Show("Remaining report: "  + reportsText);
            System.IO.File.WriteAllText(@folderPath + @"\" + @fileName.Remove(fileName.LastIndexOf(".")) + " - Report.txt", reportsText);
        }

        // Action for "Clear coloring" button
        private void clearColoringButton_Click(object sender, RibbonControlEventArgs e)
        {
            if (current_workbook != null)
            {
                RibbonHelper.RestoreColors2(color_dict[current_workbook]);
            }
        }
    }
}
