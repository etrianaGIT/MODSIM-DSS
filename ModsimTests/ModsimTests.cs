using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using Csu.Modsim.ModsimIO;
using Csu.Modsim.ModsimModel;
using Csu.Modsim.NetworkUtils;
using System.Linq;

namespace Csu.Modsim.ModsimTests
{
    public class ModsimTests
    {
        private static StringBuilder log = new StringBuilder();

        /// <summary>Runs ModsimTest.</summary>
        /// <param name="args">The arguments, which contain only one element: the directory to search for xy files.
        /// OPTIONAL [compare] skips the running of the network, requires the test output to exist in the folders.
        ///          [replaceoriginal] forces a replacement of the original files if no differences found.
        ///          [forcesqliteout] Forces the MODSIM output of the test networks to be in SQLite format.</param>
        static void Main(string[] args)
        {
            try
            {
                if (args.Length == 0 || !Directory.Exists(args[0]))
                {
                    Console.WriteLine("Wrong number of arguments. There should only be one argument: the directory to search for xy files. This directory must exist.");
                    return;
                }

                bool justCompare = false;
                bool replaceOriginal = false;
                bool forceSqliteOUTPUT = false;
                foreach (string s in args)
                {

                    if (s.ToLower().Contains("compare"))
                        justCompare = true;
                    
                    if (s.ToLower().Contains("replaceoriginal"))
                        replaceOriginal = true;

                    if (s.ToLower().Contains("forcesqliteout"))
                        forceSqliteOUTPUT = true;
                }

                // Get original files and create new files to run
                string[] origFiles = Directory.GetFiles(args[0], "* - orig.xy", SearchOption.AllDirectories);
                string[] testFiles = new string[origFiles.Length];
                string[] testFilesOUT = new string[origFiles.Length];

                for (int i = 0; i < origFiles.Length; i++)
                {
                    string testFileName = GetTestFileName(origFiles[i]);
                    Model mi = new Model();
                    XYFileReader.Read(mi, origFiles[i]);
                    mi.outputVersion = new OutputVersion(OutputVersionType.LatestVersion);
                    XYFileWriter.Write(mi, testFileName);
                    testFiles[i] = testFileName;
                    if (forceSqliteOUTPUT)
                        Modsim.ModsimModel.OutputControlInfo.SQLiteOutputFiles = true;
                    testFilesOUT[i] = testFileName.Replace(".xy",$"OUTPUT.{(Modsim.ModsimModel.OutputControlInfo.SQLiteOutputFiles ? "sqlite" : "mdb")}");
                }

                // Run all test files
                if (!justCompare)
                {
                    RunNetworks(testFiles,forceSqliteOUTPUT);
                }

                // Compare outputs
                
                CompareOutputs(testFilesOUT, origFiles, args[0],replaceOriginal);
            }
            catch (Exception ex)
            {
                MessageOut("Routine failed: " + ex.ToString());
                //log.AppendLine("Routine failed: " + ex.ToString());
            }
            finally
            {
                //send results to log file
                DirectoryInfo binBuild = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
                string logFile = Path.Combine(binBuild.Parent.Parent.Parent.FullName, "TestNetworks/TestNetorks.log");
                using (StreamWriter sw = new StreamWriter(logFile))
                {
                    sw.Write(log);
                }

                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
            }
        }

        // Run the networks and compare the outputs
        private static void RunNetworks(string[] files, bool forceSQLiteOUT)
        {
            for (int i = 0; i < files.Length; i++)
            {
                if (File.Exists(files[i]))
                {
                    string name = Path.GetFileName(files[i]);

                    try
                    {
                        //send messages to log and console
                        Model mi = new Model();
                        mi.OnMessage += mi_OnMessage;
                        mi.OnModsimError += mi_OnModsimError;

                                                //read network
                        XYFileReader.Read(mi, files[i]);

                        if (forceSQLiteOUT)
                        {
                            //mi.controlinfo = new OutputControlInfo();
                            OutputControlInfo.SQLiteOutputFiles = true;
                        }
                        OutputControlInfo.ver8MSDBOutputFiles = !Modsim.ModsimModel.OutputControlInfo.SQLiteOutputFiles;

                        //run network
                        Modsim.ModsimModel.Modsim.RunSolver(mi, mi.backRouting);

                    }
                    catch (Exception ex)
                    {
                        MessageOut("\nError reading or running: " + name);
                        MessageOut("Error Message: \n" + ex.Message + "\n" + ex.StackTrace + "\n");
                    }
                }
            }
        }

        private static void MessageOut(string msg)
        {
            Console.WriteLine(msg);
            log.AppendLine(msg);
        }

        private static void mi_OnModsimError(string message)
        {
            log.AppendLine(message);
        }

        private static void mi_OnMessage(string message)
        {
            if (!message.Contains("percent done") && !message.Contains("Last Iter") && message != "writing output")
            {
                Console.WriteLine(message);
                log.AppendLine(message);
            }
            if (message == "Done")
            {
                log.AppendLine(Environment.NewLine);
            }
        }

        private static void CompareOutputs(string[] testFiles, string[] origFiles, string testDir, bool replaceOriginal)
        {
            // Create directory to store diff files, or remove existing diff files
            string diffDir = Path.Combine(testDir, "_diffs");
            if (Directory.Exists(diffDir))
            {
                Array.ForEach(Directory.GetFiles(diffDir), File.Delete);
            }
            else
            {
                Directory.CreateDirectory(diffDir);
            }


            string[] origMdbFiles = Array.ConvertAll(origFiles, element => element.Replace(".xy", $"OUTPUT.mdb"));
            string[] origSqliteFiles = Array.ConvertAll(origFiles, element => element.Replace(".xy", $"OUTPUT.sqlite"));
            string[] testOUTFiles =  (string[])testFiles.Clone(); //Array.ConvertAll(testFiles, element => element.Replace(, $"OUTPUT.{extOUT}"));
            List<string> filesToDelete = new List<string>();
            //set file names back to xy files.
            for (int i=0;i<testFiles.Length; i++)
            {
                testFiles[i] = testFiles[i].Replace("OUTPUT", "");
                testFiles[i] = testFiles[i].Replace(Path.GetExtension(testFiles[i]),".xy");
            }
            
            for (int i = 0; i < origMdbFiles.Length; i++)
            {
                MessageOut("\nWorking on file: " + Path.GetFileName(testOUTFiles[i]));
                if (File.Exists(testOUTFiles[i]))
                {
                    DataSet testDS=null;
                    if (Path.GetExtension(testOUTFiles[i]) == ".mdb")
                    {
                        // Get the test network database
                        DBUtil db = new DBUtil(testOUTFiles[i], true, ",", false, MSType.Access);
                        db.Open();
                        testDS = db.GetTables();
                        db.Close();
                    }
                    else
                    {
                        using (SQLiteHelper sqlDB = new SQLiteHelper(testOUTFiles[i]))
                        {
                            testDS = sqlDB.GetTables();
                        }
                    }

                    // Get the original network database
                    DataSet origDS = null;
                    if (File.Exists(origMdbFiles[i]))
                    {
                        DBUtil db = new DBUtil(origMdbFiles[i], true, ",", false, MSType.Access);
                        db.Open();
                        origDS = db.GetTables();
                        db.Close();
                    }
                    else if (File.Exists(origSqliteFiles[i]))
                    {
                        using (SQLiteHelper sqlDB = new SQLiteHelper(origSqliteFiles[i]))
                        {
                            origDS = sqlDB.GetTables();
                        }
                    }

                    // Compare
                    if (testDS != null && origDS != null)
                    {
                        List<string> diffs = DataSetDifferences(testDS, origDS);
                        if (diffs.Count != 0)
                        {
                            MessageOut("!!! File has differences: " + Path.GetFileName(testOUTFiles[i]) + "... Biggest difference is in table " + BiggestDiff_TableName + " in row " + BiggestDiff_RowValue.ToString() + ": " + BiggestDiff_Value.ToString());
                            BiggestDiff_Value = 0.0;
                            string diffFile = Path.Combine(diffDir, Path.GetFileNameWithoutExtension(testOUTFiles[i]) + " - diff.csv");
                            MessageOut("Writing differences to file: " + Path.GetFileName(diffFile));
                            try
                            {
                                StreamWriter sw = new StreamWriter(diffFile);
                                sw.Write(string.Join("\n", diffs.ToArray()));
                                sw.Close();
                            }
                            catch (Exception ex)
                            {
                                MessageOut("Could not open file " + diffFile + "\n" + ex.ToString());
                            }
                            if (replaceOriginal)
                            {
                                MessageOut("  Overwriting original files ... ");
                                ReplaceOriginalFiles(origFiles[i], testFiles[i], origMdbFiles[i], testOUTFiles[i], origSqliteFiles[i]);
                            }
                        }
                        else
                        {
                            MessageOut("File does not have differences: " + Path.GetFileName(testOUTFiles[i]));
                            
                            if (replaceOriginal)
                            {
                                MessageOut("Creating new original files...");
                                ReplaceOriginalFiles(origFiles[i], testFiles[i], origMdbFiles[i], testOUTFiles[i], origSqliteFiles[i]);
                                
                            }
                            else
                            {
                                MessageOut("\tDeleting test files...");
                                try
                                {
                                    // delete test files
                                    System.Threading.Thread.Sleep(100);
                                    File.Delete(testFiles[i]);
                                }
                                catch (Exception)
                                {
                                    filesToDelete.Add(testFiles[i]);
                                }
                                try
                                {
                                    // delete test files
                                   System.Threading.Thread.Sleep(100);
                                    File.Delete(testOUTFiles[i]);
                                }
                                catch (Exception)
                                {
                                    filesToDelete.Add(testOUTFiles[i]);
                                }
                            }
                        }
                    }
                    else
                    {
                        MessageOut("FAILED TEST!!!: One of the datasets is empty!");
                    }
                }
                else
                {
                    MessageOut("FAILED TEST!!!: An output file does not exist (either " + Path.GetFileName(testOUTFiles[i]) + " or " + Path.GetFileName(origMdbFiles[i]) + ")");
                    string diffFile = Path.Combine(diffDir, Path.GetFileNameWithoutExtension(testOUTFiles[i]) + " - diff.csv");
                    try
                    {
                        StreamWriter sw = new StreamWriter(diffFile);
                        sw.Write("OUTPUT FILE IS MISSING.  Potential error in running the network");
                        sw.Close();
                    }
                    catch (Exception ex)
                    {
                        MessageOut("Could not open file " + diffFile + "\n" + ex.ToString());
                    }
                }
            }

            // If no differences remove diff dir
            if (Directory.GetFiles(diffDir).Length == 0)
            {
                Directory.Delete(diffDir);
            }

            if (filesToDelete.Count > 0)
                foreach (string file in filesToDelete)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
                    
        }

        private static void ReplaceOriginalFiles(string origFiles, string testFiles, string origMdbFiles, string testOUTFiles, string origSqliteFiles)
        {
            // delete original files
            File.Delete(origFiles);
            File.Move(testFiles, origFiles);
            File.Delete(origMdbFiles);
            File.Delete(origSqliteFiles);

            if (Path.GetExtension(testOUTFiles)==".mdb")
            {
                // rename test files as original files
                File.Move(testOUTFiles, origMdbFiles);
            }
            else 
            {
                // rename test files as original files
                File.Move(testOUTFiles, origSqliteFiles);
            }
        }

        // Retrieve dataset differences
        private static bool CanCheckAgain = true;
        private static double BiggestDiff_Value = 0.0;
        private static string BiggestDiff_TableName = "";
        private static int BiggestDiff_RowValue = 0;
        private static List<string> DataSetDifferences(DataSet a, DataSet b)
        {
            List<string> differences = new List<string>();
            foreach (DataTable aTable in a.Tables)
            {
                // Table existence
                if (!b.Tables.Contains(aTable.TableName))
                {
                    if (!(aTable.TableName.Contains("Hydro") && aTable.TableName.Contains("Info")))
                        differences.Add("Table " + aTable.TableName + " does not exist in one of the databases.");
                    else
                        MessageOut($"\t *** skipping missing {aTable.TableName} table.");
                }
                else if (CanCheckAgain)
                {
                    DataTable bTable = b.Tables[aTable.TableName];

                    // Check table equivalence
                    if (aTable.Rows.Count != bTable.Rows.Count || aTable.Columns.Count != bTable.Columns.Count)
                    {
                        differences.Add("Table " + aTable.TableName + " does not have the same number of rows in each database.");
                    }
                    else
                    {
                        bool hasDiffs = false;
                        double biggestDiff = 0.0;
                        int biggestDiffRow = 0;
                        //sorting the rows to match order
                        string sortingCols = GetSortingColumns(aTable);
                        DataRow[] aTableDR = aTable.Select("", sortingCols);
                        DataRow[] bTableDR = aTable.Select("", sortingCols);
                        for (int i = 0; i < aTableDR.Length; i++)
                        {
                            for (int j = 0; j < aTable.Columns.Count; j++)
                            {
                                if (!aTableDR[i][j].Equals(bTableDR[i][j]))
                                {
                                    hasDiffs = true;
                                    biggestDiffRow = i + 1;
                                    if (aTable.Columns[j].DataType.Equals(typeof(double)))
                                    {
                                        double aVal = Math.Abs(Convert.ToDouble(aTableDR[i][j]));
                                        double bVal = Math.Abs(Convert.ToDouble(bTableDR[i][j]));
                                        if (!aVal.Equals(bVal))
                                        {
                                            double currDiff = Math.Abs(aVal - bVal) / ((aVal == 0.0 && bVal == 0.0) ? 1 : Math.Max(aVal, bVal));
                                            if (biggestDiff < currDiff)
                                            {
                                                biggestDiff = currDiff;
                                                biggestDiffRow = i + 1;
                                            }
                                        }
                                        else
                                        {
                                            hasDiffs = false;
                                        }
                                    }
                                }
                            }
                        }

                        if (hasDiffs)
                        {
                            differences.Add("Table " + aTable.TableName + " has data differences,database a data,database b data,Biggest Difference (percent) = " + biggestDiff.ToString() + " on row " + biggestDiffRow.ToString());

                            // Column headers
                            string line = "";
                            for (int j = 0; j < aTable.Columns.Count; j++)
                            {
                                line += aTable.Columns[j].ColumnName + ",";
                            }
                            line += "___,";
                            for (int j = 0; j < bTable.Columns.Count; j++)
                            {
                                line += bTable.Columns[j].ColumnName + ",";
                            }
                            differences.Add(line);

                            // Data in rows
                            for (int i = 0; i < aTable.Rows.Count; i++)
                            {
                                differences.Add(string.Join(",", Array.ConvertAll(aTable.Rows[i].ItemArray, element => element.ToString())) + ",___," + string.Join(",", Array.ConvertAll(bTable.Rows[i].ItemArray, element => element.ToString())));
                            }
                        }

                        if (biggestDiff > BiggestDiff_Value)
                        {
                            BiggestDiff_Value = biggestDiff;
                            BiggestDiff_TableName = aTable.TableName;
                            BiggestDiff_RowValue = biggestDiffRow;
                        }
                    }
                }
            }

            if (CanCheckAgain)
            {
                CanCheckAgain = false;
                differences.AddRange(DataSetDifferences(b, a));
            }
            else
            {
                CanCheckAgain = true;
            }

            return differences;
        }

        private static List<string> sortingCols = new List<string>(new string[] {"NNo","TSIndex","LNumber","Object","OutputName","NNumber","HydroUnitID","HydroTargetID"});
        private static string GetSortingColumns(DataTable tbl)
        {
            string mCols = "";
            for (int j = 0; j < tbl.Columns.Count; j++)
            {
                if (sortingCols.Contains(tbl.Columns[j].ColumnName))
                {
                    if (mCols != "") mCols += ",";
                    mCols += tbl.Columns[j].ColumnName;
                }
            }
            return mCols;
        }

        // File names
        private static string GetOrigFileName(string testFileName)
        {
            return testFileName.Replace(".xy", " - orig.xy").Replace("OUTPUT.mdb", " - origOUTPUT.mdb");
        }
        private static string GetTestFileName(string origFileName)
        {
            return origFileName.Replace(" - orig.xy", ".xy").Replace(" - origOUTPUT.mdb", "OUTPUT.mdb");
        }
        private static string GetOrigFilePath(string testFileName, string[] origFiles)
        {
            for (int i = 0; i < origFiles.Length; i++)
            {
                if (GetTestFileName(Path.GetFileName(origFiles[i])).Equals(testFileName))
                {
                    return origFiles[i];
                }
            }
            return "";
        }

    }
}
