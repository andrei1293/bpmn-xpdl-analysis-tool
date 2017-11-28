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

                    foreach (WorkflowProcess process in workflowProcesses.Processes)
                    {
                        if (process.Activities != null && process.Transitions != null)
                        {
                            WorkflowProcessAnalysis analysis = new WorkflowProcessAnalysis();
                            analysis.Process = process;
                            analysis.CheckProcess();

                            Console.WriteLine("{0};{1};{2};{3};{4};{5};{6};{7}",
                                analysis.Tasks,
                                analysis.Gateways,
                                analysis.StartEvents,
                                analysis.IntermediateEvents,
                                analysis.EndEvents,
                                analysis.Issues.Count,
                                analysis.CSC,
                                analysis.IssuesToString());
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
