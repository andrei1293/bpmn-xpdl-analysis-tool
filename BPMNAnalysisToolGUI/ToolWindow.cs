using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using BPMNAnalysisToolCore;

namespace BPMNAnalysisToolGUI
{
    public partial class ToolWindow : Form
    {
        public ToolWindow()
        {
            InitializeComponent();
        }

        private void DragEnterHandler(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void DragDropHandler(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string filePath = ((string[])(e.Data.GetData(DataFormats.FileDrop)))[0];

                filePathBox.Text = filePath;

                processesTab.TabPages.Clear();

                WorkflowProcesses workflowProcesses = new WorkflowProcesses()
                {
                    Document = filePath
                };

                workflowProcesses.ReadDocument();

                foreach (WorkflowProcess workflowProcess in workflowProcesses.Processes)
                {
                    if (workflowProcess.Activities != null)
                    {
                        WorkflowProcessAnalysis analysis = new WorkflowProcessAnalysis()
                        {
                            Process = workflowProcess
                        };

                        analysis.CheckProcess();

                        if (analysis.Issues.Count == 0)
                        {
                            MessageBox.Show(String.Format("No issues were found in the process '{0}'",
                                workflowProcess.Name), "Analysis");
                        }
                        else
                        {
                            DataGridView resultGrid = new DataGridView();

                            resultGrid.Dock = DockStyle.Fill;
                            resultGrid.ReadOnly = true;
                            resultGrid.AllowUserToAddRows = false;

                            resultGrid.Columns.Add("Name", "Element name");
                            resultGrid.Columns.Add("Type", "Element type");
                            resultGrid.Columns.Add("Message", "Error message");

                            resultGrid.Columns["Name"].AutoSizeMode =
                                DataGridViewAutoSizeColumnMode.AllCells;
                            resultGrid.Columns["Type"].AutoSizeMode =
                                DataGridViewAutoSizeColumnMode.AllCells;
                            resultGrid.Columns["Message"].AutoSizeMode =
                                DataGridViewAutoSizeColumnMode.Fill;

                            Font defaultFont = new Font("Times New Roman", 14F, GraphicsUnit.Pixel);

                            foreach (DataGridViewColumn column in resultGrid.Columns)
                            {
                                column.DefaultCellStyle.Font = defaultFont;
                                column.HeaderCell.Style.Font = defaultFont;
                                column.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
                            }

                            resultGrid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;

                            foreach (Issue issue in analysis.Issues)
                            {
                                resultGrid.Rows.Add(new object[] 
                                {
                                    issue.Element.Name, 
                                    issue.Element.Type,
                                    issue.Message
                                });
                            }

                            TabPage processPage = new TabPage(workflowProcess.Name);

                            processPage.Controls.Add(resultGrid);

                            processesTab.TabPages.Add(processPage);
                        }
                    }
                }
            }
        }
    }
}
