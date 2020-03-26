﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Telerik.WinControls;
using Telerik.WinControls.UI;
using CableTestManager.Business;
using CableTestManager.Business.Implements;
using CableTestManager.Entity;
using WindowsFormTelerik.ControlCommon;
using CableTestManager.Model;

namespace CableTestManager.View.VProject
{
    public partial class RadProjectCreat : RadForm
    {
        private string title;
        private TProjectBasicInfo projectInfo;
        private TProjectBasicInfoManager projectInfoManager;
        private TCableTestLibraryManager lineStructLibraryDetailManager;
        public string projectName;
        private bool IsSetFixParams;
        private CableJudgeThreshold judgeThreshold;
        private bool IsOpenCustomSetTestParamUI = true;
        private bool IsEditView;
        
        public RadProjectCreat(string title,string projectName, CableJudgeThreshold cableJudgeThreshold,bool IsEdit)
        {
            InitializeComponent();
            this.title = title;
            this.projectName = projectName;
            this.StartPosition = FormStartPosition.CenterParent;
            this.judgeThreshold = cableJudgeThreshold;
            this.IsEditView = IsEdit;
            Init();
            EventHandlers();
        }

        private void Init()
        {
            if (this.title != "")
                this.Text = title;
            projectInfo = new TProjectBasicInfo();
            projectInfoManager = new TProjectBasicInfoManager();
            lineStructLibraryDetailManager = new TCableTestLibraryManager();
            RadGridViewProperties.SetRadGridViewProperty(this.radGridView1,false,true,5);
            this.checkIsGroup.CheckState = CheckState.Unchecked;
            this.btn_groupParams.Visible = false;
            QueryPlugLineStructInfo();
            UpdateProjectInfo();
        }

        private void EventHandlers()
        {
            this.btn_groupParams.Click += Btn_groupParams_Click;
            this.radGridView1.CellClick += RadGridView1_CellClick;
            this.btnApply.Click += BtnApply_Click;
            this.btnClose.Click += BtnClose_Click;
            this.rtbCableCondition.TextChanged += RtbCableCondition_TextChanged;
            this.rbt_customTestParams.CheckStateChanged += Rbt_customTestParams_CheckStateChanged;
            this.rbt_defaultTestParams.CheckStateChanged += Rbt_defaultTestParams_CheckStateChanged;
            this.checkIsGroup.CheckStateChanged += CheckIsGroup_CheckStateChanged;
        }

        private void CheckIsGroup_CheckStateChanged(object sender, EventArgs e)
        {
            if (this.checkIsGroup.Checked)
                this.btn_groupParams.Visible = true;
            else
                this.btn_groupParams.Visible = false;
        }

        private void RtbCableCondition_TextChanged(object sender, EventArgs e)
        {
            QueryPlugLineStructInfo();
        }

        private void Btn_groupParams_Click(object sender, EventArgs e)
        {
            var curLineCable = this.rtbCurrentTestCable.Text.Trim();
            if (curLineCable == "")
            {
                MessageBox.Show("未选择测试线束！","提示",MessageBoxButtons.OK,MessageBoxIcon.Warning);
                return;
            }
            GroupTestStandardParams groupTestStandardParams = new GroupTestStandardParams(curLineCable,this.judgeThreshold);
            groupTestStandardParams.ShowDialog(); 
        }

        private void Rbt_defaultTestParams_CheckStateChanged(object sender, EventArgs e)
        {
            if (this.rbt_defaultTestParams.Checked)
            {
                if (this.rbt_customTestParams.Checked)
                {
                    this.rbt_customTestParams.CheckState = CheckState.Unchecked;
                }
            }
        }

        private void Rbt_customTestParams_CheckStateChanged(object sender, EventArgs e)
        {
            if (this.rbt_customTestParams.Checked)
            {
                if (this.rbt_defaultTestParams.Checked)
                {
                    this.rbt_defaultTestParams.CheckState = CheckState.Unchecked;
                }
                if (this.IsOpenCustomSetTestParamUI)
                {
                    OpenFixParamsConfig();
                }
            }
        }

        private void OpenFixParamsConfig()
        {
            SetFixedTestParams setFixedTestParams = new SetFixedTestParams(projectInfo,IsEditView);
            if (setFixedTestParams.ShowDialog() == DialogResult.OK)
            {
                IsSetFixParams = true;
            }
            else
            {
                IsSetFixParams = false;
            }
        }

        private void BtnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void BtnApply_Click(object sender, EventArgs e)
        {
            CreateOrUpdateProject();
        }

        private void RadGridView1_CellClick(object sender, GridViewCellEventArgs e)
        {
            if (this.radGridView1.RowCount < 1)
                return;
            var cableName = this.radGridView1.CurrentRow.Cells[1].Value.ToString();
            this.rtbCurrentTestCable.Text = cableName;
        }

        private void UpdateProjectInfo()
        {
            if (this.projectName == "")
                return;
            this.rtbProjectName.Text = this.projectName;
            this.rtbCurrentTestCable.Text = this.projectName;
            var dt = projectInfoManager.GetDataSetByWhere($"where ProjectName='{this.projectName}'").Tables[0];
            if (dt.Rows.Count < 1)
                return;
            foreach (DataRow dr in dt.Rows)
            {
                this.rtbBatchNo.Text = dr["BatchNumber"].ToString();
                this.rtbProjectRemark.Text = dr["Remark"].ToString();
                var isUseDefaultParams = dr["IsUseSelfDefineParams"].ToString();
                if (isUseDefaultParams == "0")
                    this.rbt_defaultTestParams.CheckState = CheckState.Checked;
                else if (isUseDefaultParams == "1")
                {
                    this.rbt_customTestParams.CheckState = CheckState.Checked;
                    this.IsOpenCustomSetTestParamUI = false;
                }
                var IsCommon = dr["IsCommonProject"].ToString();
                if (IsCommon == "0")
                    this.checkIsCommonPro.Checked = false;
                else if (IsCommon == "1")
                {
                    this.checkIsCommonPro.Checked = true;
                }
                var IsGroup = dr["IsGroupTestProject"].ToString();
                if (IsGroup == "0")
                {
                    this.checkIsGroup.Checked = false;
                    this.btn_groupParams.Visible = false;
                }
                else if (IsGroup == "1")
                {
                    this.checkIsGroup.Checked = true;
                    this.btn_groupParams.Visible = true;
                }
                this.rtbCurrentTestCable.Text = dr["TestCableName"].ToString();
                this.rtbCableCondition.Text = this.rtbCurrentTestCable.Text;
                projectInfo.ConductTestThreshold = double.Parse(dr["ConductTestThreshold"].ToString());
                projectInfo.ConductTestVoltage = double.Parse(dr["ConductTestVoltage"].ToString());
                projectInfo.ConductTestCurrentElect = double.Parse(dr["ConductTestCurrentElect"].ToString());
                projectInfo.InsulateTestThreshold = double.Parse(dr["InsulateTestThreshold"].ToString());
                projectInfo.InsulateTestVoltage = double.Parse(dr["InsulateTestVoltage"].ToString());
                projectInfo.InsulateTestRaiseTime = double.Parse(dr["InsulateTestRaiseTime"].ToString());
                projectInfo.InsulateTestHoldTime = double.Parse(dr["InsulateTestHoldTime"].ToString());
                projectInfo.InsulateHigthOrLowElect = double.Parse(dr["InsulateHigthOrLowElect"].ToString());
                projectInfo.VoltageWithStandardThreshold = double.Parse(dr["VoltageWithStandardThreshold"].ToString());
                projectInfo.VoltageWithStandardHoldTime = double.Parse(dr["VoltageWithStandardHoldTime"].ToString());
                projectInfo.VoltageWithStandardVoltage = double.Parse(dr["VoltageWithStandardVoltage"].ToString());
            }
            QueryPlugLineStructInfo();
        }

        private void QueryPlugLineStructInfo()
        {
            RadGridViewProperties.ClearGridView(this.radGridView1);
            var selectSQL = "";
            if (this.rtbCableCondition.Text.Trim() != "")
                selectSQL = $"where CableName like '%{this.rtbCableCondition.Text.Trim()}%'";
            var data = lineStructLibraryDetailManager.GetDataSetByFieldsAndWhere("distinct CableName", selectSQL).Tables[0];
            if (data.Rows.Count < 1)
                return;
            int iRow = 0;
            foreach (DataRow dr in data.Rows)
            {
                var lineStructName = dr["CableName"].ToString();
                if (IsExistLineStruct(lineStructName))
                    continue;
                this.radGridView1.Rows.AddNew();
                var resultString = GetInterfaceInfoByCableName(lineStructName);
                this.radGridView1.Rows[iRow].Cells[0].Value = iRow + 1;
                this.radGridView1.Rows[iRow].Cells[1].Value = lineStructName;
                this.radGridView1.Rows[iRow].Cells[2].Value = resultString;
                this.radGridView1.Rows[iRow].Cells[3].Value = lineStructLibraryDetailManager.GetRowCountByWhere($"where CableName='{lineStructName}'");
                //this.radGridView1.Rows[iRow].Cells[4].Value = dr["Remark"].ToString();
                iRow++;
            }
        }

        private string GetInterfaceInfoByCableName(string cableName)
        {
            var data = lineStructLibraryDetailManager.GetDataSetByFieldsAndWhere("distinct StartInterface,EndInterface", $"where CableName='{cableName}'").Tables[0];
            var resultString = "";
            foreach (DataRow dr in data.Rows)
            {
                if (!resultString.Contains(dr[0].ToString()))
                    resultString += dr[0].ToString() + ",";
                if (!resultString.Contains(dr[1].ToString()))
                    resultString += dr[1].ToString() + ",";
            }
            return resultString.Substring(0, resultString.Length - 1);
        }

        private bool IsExistLineStruct(string cableName)
        {
            foreach (var rowInfo in this.radGridView1.Rows)
            {
                if (rowInfo.Cells[1].Value != null)
                {
                    if (rowInfo.Cells[1].Value.ToString() == cableName)
                        return true;
                }
            }
            return false;
        }

        private void CreateOrUpdateProject()
        {
            if (this.rtbProjectName.Text.Trim() == "")
            {
                MessageBox.Show("工程名称不能为空！","提示",MessageBoxButtons.OK);
                return;
            }
            if (this.rtbBatchNo.Text.Trim() == "")
            {
                MessageBox.Show("批次号不能为空！", "提示", MessageBoxButtons.OK);
                return;
            }
            if (this.rtbCurrentTestCable.Text.Trim() == "")
            {
                MessageBox.Show("请选择被测线束！", "提示", MessageBoxButtons.OK);
                return;
            }
            //add info 
            projectInfo.ID = CableTestManager.Common.TablePrimaryKey.InsertProjectBInfoPID();
            projectInfo.ProjectName = this.rtbProjectName.Text.Trim();
            this.projectName = projectInfo.ProjectName;
            projectInfo.BatchNumber = this.rtbBatchNo.Text.Trim();
            projectInfo.Remark = this.rtbProjectRemark.Text.Trim();
            projectInfo.TestCableName = this.rtbCurrentTestCable.Text;
            if (this.checkIsCommonPro.Checked)
                projectInfo.IsCommonProject = 1;
            else
                projectInfo.IsCommonProject = 0;
            if (this.checkIsGroup.Checked)
                projectInfo.IsGroupTestProject = 1;
            else
                projectInfo.IsGroupTestProject = 0;

            if (this.rbt_defaultTestParams.IsChecked)
            {
                projectInfo.IsUseSelfDefineParams = 0;
                projectInfo.ConductTestThreshold = 2;
                projectInfo.ConductTestVoltage = 12;
                projectInfo.ConductTestCurrentElect = 2;

                projectInfo.InsulateTestThreshold = 20;
                projectInfo.InsulateTestHoldTime = 1;
                projectInfo.InsulateTestVoltage = 100;
                projectInfo.InsulateTestRaiseTime = 2;
                projectInfo.InsulateHigthOrLowElect = 1;

                projectInfo.VoltageWithStandardThreshold = 2;
                projectInfo.VoltageWithStandardHoldTime = 1;
                projectInfo.VoltageWithStandardVoltage = 100;
            }
            else
                projectInfo.IsUseSelfDefineParams = 1;

            var iRow = 0;
            var originCount = projectInfoManager.GetRowCount();
            if (!IsExistProject(projectInfo.ProjectName))
            {
                iRow = projectInfoManager.Insert(projectInfo);
                iRow = iRow - originCount;
            }
            else
            {
                //update
                projectInfo.ID = GetProjectInfoID(projectInfo.ProjectName);
                iRow = projectInfoManager.Update(projectInfo);
                if (iRow > 0)
                {
                    MessageBox.Show("更新成功！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("更新失败！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }

            this.Close();
            this.DialogResult = DialogResult.OK;
        }

        private bool IsExistProject(string projectName)
        {
            var ds = projectInfoManager.GetDataSetByWhere($"where ProjectName='{projectName}'").Tables[0];
            if (ds.Rows.Count < 1)
                return false;
            return true;
        }

        private int GetProjectInfoID(string projectName)
        {
            var dt = projectInfoManager.GetDataSetByFieldsAndWhere("ID",$"where ProjectName='{projectName}'").Tables[0];
            if (dt.Rows.Count > 0)
                return int.Parse(dt.Rows[0][0].ToString());
            return 0;
        }
    }
}