using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.IO;

namespace BPMNAnalysisToolCore
{
    public enum ActivityType
    {
        StartEvent,
        Implementation,
        IntermediateEvent,
        Route,
        EndEvent
    }

    public class WorkflowProcess
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public List<Activity> Activities { get; set; }

        public List<Transition> Transitions { get; set; }

        public ProcessMatrix Matrix { get; set; }

        public Activity GetActivityById(string id)
        {
            foreach (Activity activity in Activities)
            {
                if (activity.Id.Equals(id))
                {
                    return activity;
                }
            }

            return null;
        }

        public int GetActivityIndexById(string id)
        {
            for (int index = 0; index < Activities.Count; index++)
            {
                if (Activities[index].Id.Equals(id))
                {
                    return index;
                }
            }

            return -1;
        }

        public void CalculateProcessMatrix()
        {
            Matrix = new ProcessMatrix()
            {
                Size = Activities.Count
            };

            Matrix.InitialFill();

            foreach (Transition transition in Transitions)
            {
                int from = GetActivityIndexById(transition.From);
                int to = GetActivityIndexById(transition.To);

                Matrix.AddTransition(from, to);
            }
        }

        public override string ToString()
        {
            return String.Format("Workflow Process Id = {0} Name = {1}", Id, Name);
        }
    }

    public class ProcessMatrix
    {
        public int[,] Array { get; set; }

        public int Size { get; set; }

        public void InitialFill()
        {
            Array = new int[Size, Size];

            for (int i = 0; i < Size; i++)
            {
                for (int j = 0; j < Size; j++)
                {
                    Array[i, j] = 0;
                }
            }
        }

        public void AddTransition(int row, int column)
        {
            Array[row, column]++;
        }
    }

    public class Issue
    {
        public Activity Element { get; set; }

        public string Message { get; set; }

        public override string ToString()
        {
            return String.Format("{0} <{1}> : {2}", Element.Name, Element.Type, Message);
        }
    }

    public class WorkflowProcessAnalysis
    {
        public WorkflowProcess Process { get; set; }

        public List<Issue> Issues { get; set; }

        public int Tasks { get; set; }
        public int Gateways { get; set; }
        public int StartEvents { get; set; }
        public int EndEvents { get; set; }
        public int IntermediateEvents { get; set; }
        public double CSC { get; set; }

        public WorkflowProcessAnalysis()
        {
            Issues = new List<Issue>();
        }

        public string IssuesToString()
        {
            string result = "";

            foreach (Issue issue in Issues)
            {
                result += issue.Message + ";";
            }

            return result;
        }

        public void CheckProcess()
        {
            Process.CalculateProcessMatrix();

            CheckStartEvents();
            CheckEndEvents();
            CheckIntermediateEvents();
            CheckProcessFlow();
            CheckTasks();

            CalculateCSC();
        }

        class Messages
        {
            public const string START_EVENTS = "Для процесса '{0}' не определено стартовое событие";
            public const string END_EVENTS = "Для процесса '{0}' не определено конечное событие";
            public const string INTERMEDIATE_EVENTS = "Промежуточное событие '{0}' исключено из потока процесса '{1}'";
            public const string PROCESS_FLOW = "Для разделения потока процесса '{0}' на несколько маршрутов не используется шлюз";
            public const string TARGET_TASKS = "Задача '{0}' не приводит к завершению процесса '{1}'";
            public const string SOURCE_TASKS = "Задача '{0}' не связана с остальным процессом '{1}'";
        }

        private void CalculateCSC()
        {
            double startEventsAmount = 0;
            double endEventsAmount = 0;

            foreach (Activity activity in Process.Activities)
            {
                if (activity.Type.Equals(ActivityType.StartEvent))
                {
                    startEventsAmount++;
                }

                if (activity.Type.Equals(ActivityType.EndEvent))
                {
                    endEventsAmount++;
                }
            }

            ProcessMatrix matrix = Process.Matrix;

            double intermediateEventTargets = 0;
            double intermediateEventSources = 0;
            double intermediateEventAmount = 0;

            foreach (Activity activity in Process.Activities)
            {
                if (activity.Type.Equals(ActivityType.IntermediateEvent))
                {
                    intermediateEventAmount++;
                }
            }

            foreach (Activity activity in Process.Activities)
            {
                if (activity.Type.Equals(ActivityType.IntermediateEvent))
                {
                    double intermediateEventTargetsSum = 0;
                    double intermediateEventSourcesSum = 0;

                    int index = Process.GetActivityIndexById(activity.Id);

                    for (int i = 0; i < matrix.Size; i++)
                    {
                        intermediateEventTargetsSum += matrix.Array[index, i];
                    }

                    for (int i = 0; i < matrix.Size; i++)
                    {
                        intermediateEventSourcesSum += matrix.Array[i, index];
                    }

                    intermediateEventTargets += 
                        ((intermediateEventTargetsSum > 0 ? 1 : 0) - 1) / intermediateEventAmount;
                    intermediateEventSources += 
                        ((intermediateEventSourcesSum > 0 ? 1 : 0) - 1) / intermediateEventAmount;
                }
            }

            double taskTargets = 0;
            double taskSources = 0;
            double taskAmount = 0;

            foreach (Activity activity in Process.Activities)
            {
                if (activity.Type.Equals(ActivityType.Implementation))
                {
                    taskAmount++;
                }
            }

            foreach (Activity activity in Process.Activities)
            {
                if (activity.Type.Equals(ActivityType.Implementation))
                {
                    double taskTargetsSum = 0;
                    double taskSourcesSum = 0;

                    int index = Process.GetActivityIndexById(activity.Id);

                    for (int i = 0; i < matrix.Size; i++)
                    {
                        taskTargetsSum += matrix.Array[index, i];
                    }

                    for (int i = 0; i < matrix.Size; i++)
                    {
                        taskSourcesSum += matrix.Array[i, index];
                    }

                    taskTargets += Math.Abs(1 - taskTargetsSum) > 0 ? 1 : 0;
                    taskSources += ((taskSourcesSum > 0 ? 1 : 0) - 1) / taskAmount;
                }
            }

            CSC = ((startEventsAmount > 0 ? 1 : 0) - 1) + ((endEventsAmount > 0 ? 1 : 0) - 1) +
                intermediateEventTargets + intermediateEventSources + taskSources - taskTargets;
        }

        private void CheckStartEvents()
        {
            int startEventsAmount = 0;

            foreach (Activity activity in Process.Activities)
            {
                if (activity.Type.Equals(ActivityType.StartEvent))
                {
                    startEventsAmount++;
                }
            }

            this.StartEvents = startEventsAmount;

            if (startEventsAmount == 0)
            {
                Issues.Add(new Issue()
                {
                    Element = new Activity()
                    {
                        Id = "None",
                        Name = "Не указано",
                        Type = ActivityType.StartEvent
                    },
                    Message = String.Format(Messages.START_EVENTS, Process.Name)
                });
            }
        }

        private void CheckEndEvents()
        {
            int endEventsAmount = 0;

            foreach (Activity activity in Process.Activities)
            {
                if (activity.Type.Equals(ActivityType.EndEvent))
                {
                    endEventsAmount++;
                }
            }

            this.EndEvents = endEventsAmount;

            if (endEventsAmount == 0)
            {
                Issues.Add(new Issue()
                {
                    Element = new Activity()
                    {
                        Id = "None",
                        Name = "Не указано",
                        Type = ActivityType.EndEvent
                    },
                    Message = String.Format(Messages.END_EVENTS, Process.Name)
                });
            }
        }

        private void CheckIntermediateEvents()
        {
            ProcessMatrix matrix = Process.Matrix;

            this.IntermediateEvents = 0;

            foreach (Activity activity in Process.Activities)
            {
                if (activity.Type.Equals(ActivityType.IntermediateEvent))
                {
                    this.IntermediateEvents++;

                    int index = Process.GetActivityIndexById(activity.Id);

                    int intermediateEventTargets = 0;

                    for (int i = 0; i < matrix.Size; i++)
                    {
                        intermediateEventTargets += matrix.Array[index, i];
                    }

                    int intermediateEventSources = 0;

                    for (int i = 0; i < matrix.Size; i++)
                    {
                        intermediateEventSources += matrix.Array[i, index];
                    }

                    if (intermediateEventTargets == 0 || intermediateEventSources == 0)
                    {
                        Issues.Add(new Issue()
                        {
                            Element = activity,
                            Message = String.Format(Messages.INTERMEDIATE_EVENTS, 
                                activity.Name, Process.Name)
                        });
                    }
                }
            }
        }

        private void CheckProcessFlow()
        {
            ProcessMatrix matrix = Process.Matrix;

            this.Gateways = 0;

            foreach (Activity activity in Process.Activities)
            {
                if (activity.Type.Equals(ActivityType.Route))
                {
                    this.Gateways++;
                }
            }

            foreach (Activity activity in Process.Activities)
            {
                if (activity.Type.Equals(ActivityType.Implementation))
                {
                    int index = Process.GetActivityIndexById(activity.Id);

                    int taskTargets = 0;

                    for (int i = 0; i < matrix.Size; i++)
                    {
                        taskTargets += matrix.Array[index, i];
                    }

                    if (taskTargets > 1)
                    {
                        Issues.Add(new Issue()
                        {
                            Element = activity,
                            Message = String.Format(Messages.PROCESS_FLOW, Process.Name)
                        });
                    }
                }
            }
        }

        private void CheckTasks()
        {
            ProcessMatrix matrix = Process.Matrix;

            this.Tasks = 0;

            foreach (Activity activity in Process.Activities)
            {
                if (activity.Type.Equals(ActivityType.Implementation))
                {
                    this.Tasks++;

                    int index = Process.GetActivityIndexById(activity.Id);

                    int taskTargets = 0;

                    for (int i = 0; i < matrix.Size; i++)
                    {
                        taskTargets += matrix.Array[index, i];
                    }

                    int taskSources = 0;

                    for (int i = 0; i < matrix.Size; i++)
                    {
                        taskSources += matrix.Array[i, index];
                    }

                    if (taskTargets == 0)
                    {
                        Issues.Add(new Issue()
                        {
                            Element = activity,
                            Message = String.Format(Messages.TARGET_TASKS, activity.Name, Process.Name)
                        });
                    }

                    if (taskSources == 0)
                    {
                        Issues.Add(new Issue()
                        {
                            Element = activity,
                            Message = String.Format(Messages.SOURCE_TASKS, activity.Name, Process.Name)
                        });
                    }
                }
            }
        }
    }

    public class Activity
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public ActivityType Type { get; set; }

        public override string ToString()
        {
            return String.Format("Activity <{2}> Id = {0} Name = {1}", Id, Name, Type);
        }
    }

    public class Transition
    {
        public string Id { get; set; }

        public string From { get; set; }

        public string To { get; set; }

        public override string ToString()
        {
            return String.Format("Transition Id = {0} From = {1} To = {2}", Id, From, To);
        }
    }

    public class WorkflowProcesses
    {
        public string Document { get; set; }

        public List<WorkflowProcess> Processes { get; set; }

        public void SaveAsRDFTriplesSet(string xpdlDocument)
        {
            string bpmnBaseURI = "http://process-model.org/bpmn/";
            string domainBaseURI = "http://process-model.org/domain/";

            string fileName = xpdlDocument.Split('.')[0] + ".nt";

            using (StreamWriter writer = new StreamWriter(fileName))
            {
                foreach (WorkflowProcess process in Processes)
                {
                    if (process.Activities != null)
                    {
                        string subject = null;
                        string property = null;
                        string _object = null;

                        foreach (Activity activity in process.Activities)
                        {
                            subject = "<" + domainBaseURI + process.Name.Replace(' ', '_') + ">";
                            property = "<" + bpmnBaseURI + "Orchestration>";
                            _object = "<" + domainBaseURI + activity.Name.Replace(' ', '_') + ">";

                            if (activity.Name == null || activity.Name.Length == 0)
                            {
                                _object = "<" + domainBaseURI + activity.Type + ">";
                            }

                            writer.WriteLine(String.Format("{0} {1} {2} .", subject, property, _object));
                        }

                        foreach (Transition transition in process.Transitions)
                        {
                            Activity from = process.GetActivityById(transition.From);
                            Activity to = process.GetActivityById(transition.To);

                            subject = subject = "<" + domainBaseURI + from.Name.Replace(' ', '_') + ">";

                            if (from.Name == null || from.Name.Length == 0)
                            {
                                subject = "<" + domainBaseURI + from.Type + ">";
                            }

                            property = "<" + bpmnBaseURI + "Transition>";
                            _object = "<" + domainBaseURI + to.Name.Replace(' ', '_') + ">";

                            if (to.Name == null || to.Name.Length == 0)
                            {
                                _object = "<" + domainBaseURI + to.Type + ">";
                            }

                            writer.WriteLine(String.Format("{0} {1} {2} .", subject, property, _object));
                        }
                    }
                }
            }
        }

        public void ReadDocument()
        {
            XmlDocument xmlDocument = new XmlDocument();

            xmlDocument.Load(Document);

            string xmlContents = xmlDocument.InnerXml;

            Processes = new List<WorkflowProcess>();

            XmlReader xmlReader = XmlReader.Create(new StringReader(xmlContents));

            while (xmlReader.Read())
            {
                if (xmlReader.NodeType == XmlNodeType.Element)
                {
                    if (xmlReader.LocalName.Equals("WorkflowProcess"))
                    {
                        Processes.Add(new WorkflowProcess()
                        {
                            Id = xmlReader.GetAttribute("Id"),
                            Name = xmlReader.GetAttribute("Name")
                        });
                    }

                    if (xmlReader.LocalName.Equals("Activity"))
                    {
                        if (Processes[Processes.Count - 1].Activities == null)
                        {
                            Processes[Processes.Count - 1].Activities =
                                new List<Activity>();
                        }

                        Processes[Processes.Count - 1].Activities.
                            Add(new Activity()
                            {
                                Id = xmlReader.GetAttribute("Id"),
                                Name = xmlReader.GetAttribute("Name")
                            });
                    }

                    if (xmlReader.LocalName.Equals(ActivityType.StartEvent.ToString()))
                    {
                        Processes[Processes.Count - 1].
                                Activities[Processes[Processes.Count - 1].
                                Activities.Count - 1].Type = ActivityType.StartEvent;
                    }

                    if (xmlReader.LocalName.Equals(ActivityType.Implementation.ToString()))
                    {
                        Processes[Processes.Count - 1].
                                Activities[Processes[Processes.Count - 1].
                                Activities.Count - 1].Type = ActivityType.Implementation;
                    }

                    if (xmlReader.LocalName.Equals(ActivityType.IntermediateEvent.ToString()))
                    {
                        Processes[Processes.Count - 1].
                                Activities[Processes[Processes.Count - 1].
                                Activities.Count - 1].Type = ActivityType.IntermediateEvent;
                    }

                    if (xmlReader.LocalName.Equals(ActivityType.Route.ToString()))
                    {
                        Processes[Processes.Count - 1].
                                Activities[Processes[Processes.Count - 1].
                                Activities.Count - 1].Type = ActivityType.Route;
                    }

                    if (xmlReader.LocalName.Equals(ActivityType.EndEvent.ToString()))
                    {
                        Processes[Processes.Count - 1].
                                Activities[Processes[Processes.Count - 1].
                                Activities.Count - 1].Type = ActivityType.EndEvent;
                    }

                    if (xmlReader.LocalName.Equals("Transition"))
                    {
                        if (Processes[Processes.Count - 1].Transitions == null)
                        {
                            Processes[Processes.Count - 1].Transitions =
                                new List<Transition>();
                        }

                        Processes[Processes.Count - 1].Transitions.
                            Add(new Transition()
                            {
                                Id = xmlReader.GetAttribute("Id"),
                                From = xmlReader.GetAttribute("From"),
                                To = xmlReader.GetAttribute("To")
                            });
                    }
                }
            }
        }
    }
}
