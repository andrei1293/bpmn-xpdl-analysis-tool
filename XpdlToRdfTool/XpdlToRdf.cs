using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BPMNAnalysisToolCore;

namespace XpdlToRdfTool
{
    public class XpdlToRdf
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                throw new Exception("Invalid arguments!");
            }

            string xpdlDocument = args[0];

            WorkflowProcesses workflowProcesses = new WorkflowProcesses()
            {
                Document = xpdlDocument
            };

            workflowProcesses.ReadDocument();

            foreach (WorkflowProcess workflowProcess in workflowProcesses.Processes)
            {
                if (workflowProcess.Activities != null)
                {
                    workflowProcess.SaveAsRDFTriplesSet(xpdlDocument);
                }
            }
        }
    }
}
