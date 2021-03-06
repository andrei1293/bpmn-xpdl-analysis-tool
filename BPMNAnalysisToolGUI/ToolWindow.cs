﻿using System;
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
        private Dictionary<string, string> localeActivity = new Dictionary<string, string>()
        {
            { "StartEvent", "Стартовое событие" },
            { "Implementation", "Задача/подпроцесс" },
            { "IntermediateEvent", "Промежуточное событие" },
            { "Route", "Шлюз" },
            { "EndEvent", "Конечное событие" }
        };

        private int Tasks = 0;
        private int Gateways = 0;
        private int Start = 0;
        private int Intermediate = 0;
        private int End = 0;
        private int Shortcomings = 0;
        private double Coefficient = 0;

        public ToolWindow()
        {
            InitializeComponent();
        }

        private void ClearMetrics()
        {
            Tasks = 0;
            Gateways = 0;
            Start = 0;
            Intermediate = 0;
            End = 0;
            Shortcomings = 0;
            Coefficient = 0;
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
                workflowProcesses.SaveAsRDFTriplesSet(filePath);

                foreach (WorkflowProcess workflowProcess in workflowProcesses.Processes)
                {
                    if (workflowProcess.Activities != null)
                    {
                        WorkflowProcessAnalysis analysis = new WorkflowProcessAnalysis()
                        {
                            Process = workflowProcess
                        };

                        analysis.CheckProcess();

                        Tasks += analysis.Tasks;
                        Gateways += analysis.Gateways;
                        Start += analysis.StartEvents;
                        Intermediate += analysis.IntermediateEvents;
                        End += analysis.EndEvents;
                        Coefficient += analysis.CalcIntegratedCSC();
                        Shortcomings += analysis.Issues.Count;

                        DataGridView resultGrid = new DataGridView();

                        resultGrid.Dock = DockStyle.Fill;
                        resultGrid.ReadOnly = true;
                        resultGrid.AllowUserToAddRows = false;

                        resultGrid.Columns.Add("Name", "Имя элемента");
                        resultGrid.Columns.Add("Type", "Тип элемента");
                        resultGrid.Columns.Add("Message", "Описание ошибки");

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
                                    localeActivity[issue.Element.Type.ToString()],
                                    issue.Message
                                });
                        }

                        TabPage processPage = new TabPage(workflowProcess.Name + " (CSC = " +
                                Math.Round(analysis.CSC, 4) + ")");

                        processesTab.TabPages.Add(processPage);

                        processPage.Controls.Add(resultGrid);
                    }
                }

                tasksField.Text = Tasks.ToString();
                gatewaysField.Text = Gateways.ToString();
                startField.Text = Start.ToString();
                intermediateField.Text = Intermediate.ToString();
                endField.Text = End.ToString();
                shortcomingsField.Text = Shortcomings.ToString();
                coefficientField.Text = Math.Round(Coefficient, 4).ToString();

                ClearMetrics();
            }
        }
    }
}
