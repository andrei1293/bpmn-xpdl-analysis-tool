using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BPMNAnalysisToolCore;
using System.IO;

namespace XpdlToRdfTool
{
    public class XpdlToRdf
    {
        static void Main(string[] args)
        {
            try
            {
                if (args.Length == 0)
                {
                    throw new InvalidOperationException();
                }

                if (args[0].Equals("-a"))
                {
                    string[] filePaths = Directory.GetFiles(@"models\", "*.xpdl", 
                        SearchOption.TopDirectoryOnly);

                    foreach (string filePath in filePaths)
                    {
                        WorkflowProcesses workflowProcesses = new WorkflowProcesses()
                        {
                            Document = filePath
                        };

                        workflowProcesses.ReadDocument();
                        workflowProcesses.SaveAsRDFTriplesSet(filePath);
                    }
                }
                else if (args[0].Equals("-f"))
                {
                    string xpdlDocument = @"models\" + args[1];

                    WorkflowProcesses workflowProcesses = new WorkflowProcesses()
                    {
                        Document = xpdlDocument
                    };

                    workflowProcesses.ReadDocument();
                    workflowProcesses.SaveAsRDFTriplesSet(xpdlDocument);

                    int tasks = 0;
                    int gateways = 0;
                    int start = 0;
                    int intermediate = 0;
                    int end = 0;
                    int issuesCount = 0;
                    double csc = 0;
                    string issues = "";

                    foreach (WorkflowProcess process in workflowProcesses.Processes)
                    {
                        if (process.Activities != null && process.Transitions != null)
                        {
                            WorkflowProcessAnalysis analysis = new WorkflowProcessAnalysis();
                            analysis.Process = process;
                            analysis.CheckProcess();

                            tasks += analysis.Tasks;
                            gateways += analysis.Gateways;
                            start += analysis.StartEvents;
                            intermediate += analysis.IntermediateEvents;
                            end += analysis.EndEvents;
                            csc += analysis.CSC;
                            issuesCount += analysis.Issues.Count;
                            issues += analysis.IssuesToString();
                        }
                    }

                    Console.WriteLine("{0};{1};{2};{3};{4};{5};{6};{7}",
                        tasks,
                        gateways,
                        start,
                        intermediate,
                        end,
                        issuesCount,
                        csc,
                        issues);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
