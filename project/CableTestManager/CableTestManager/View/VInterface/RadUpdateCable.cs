﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Telerik.WinControls;
using CableTestManager.View.VInterface;
using CableTestManager.Business.Implements;
using WindowsFormTelerik.ControlCommon;
using System.Threading.Tasks;
using CableTestManager.Entity;
using WindowsFormTelerik.GridViewExportData;
using Telerik.WinControls.UI;
using CableTestManager.Model;
using CableTestManager.CUserManager;

namespace CableTestManager.View.VInterface
{
    public partial class RadUpdateCable : RadForm
    {
        private TCableTestLibraryManager lineStructManager;
        private string lineCableName, remark;
        private bool IsEditView;
        //是否有完成过测试
        private bool IsConductCompletedTest;
        private bool IsShortCircuitCompletedTest;
        private bool IsInsulateCompletedTest;
        private bool IsPressureCompletedTest;
        private List<CableLibParams> cableLibList = new List<CableLibParams>();
        private InterfaceInfoLibraryManager plugLibraryDetailManager = new InterfaceInfoLibraryManager();
        private const string COLUMN_ORDER = "序号";
        private const string COLUMN_START_INTER = "起始接口";
        private const string COLUMN_START_POINT = "起始接点";
        private const string COLUMN_END_INTER = "最终接口";
        private const string COLUMN_END_POINT = "最终接点";
        //private DataTable cableAddDatasource,cableDelDatasource;

        private List<CableConfig> cableAddList;
        private List<CableConfig> cableDelList;

        public RadUpdateCable(string title,string lineCableName,bool IsEdit,string remark)
        {
            InitializeComponent();
            this.Text = title;
            this.IsEditView = IsEdit;
            this.lineCableName = lineCableName;
            this.remark = remark;
        }

        private void RadUpdateCable_Load(object sender, EventArgs e)
        {
            Init();
            EventHandlers();
        }

        private void Init()
        {
            this.cableAddList = new List<CableConfig>();
            this.cableDelList = new List<CableConfig>();
            this.StartPosition = FormStartPosition.CenterParent;
            this.rdb2Method.CheckState = CheckState.Checked;
            InitDataTable();
            lineStructManager = new TCableTestLibraryManager();
            RadGridViewProperties.SetRadGridViewProperty(this.radGridView1,false,true,5);

            this.cb_startInterface.EditorControl.Columns.Add("startInterfacePoint");
            this.cb_startInterface.EditorControl.ShowColumnHeaders = false;
            this.cb_startPin.EditorControl.Columns.Add("startPin");
            this.cb_startPin.EditorControl.ShowColumnHeaders = false;
            this.cb_endInterface.EditorControl.Columns.Add("endInterfacePoint");
            this.cb_endInterface.EditorControl.ShowColumnHeaders = false;
            this.cb_endPin.EditorControl.Columns.Add("endPin");
            this.cb_endPin.EditorControl.ShowColumnHeaders = false;

            this.checkCircuit.CheckState = CheckState.Checked;
            this.checkConduction.CheckState = CheckState.Checked;
            this.checkInsulate.CheckState = CheckState.Checked;
            this.checkPressureProof.CheckState = CheckState.Checked;

            GetLineStructDetailData(lineCableName);
            CheckMeasureMethodValid();
        }

        private void InitDataTable()
        {
            //this.cableAddDatasource = new DataTable();
            //this.cableAddDatasource.Columns.Add(COLUMN_ORDER);
            //this.cableAddDatasource.Columns.Add(COLUMN_START_INTER);
            //this.cableAddDatasource.Columns.Add(COLUMN_START_POINT);
            //this.cableAddDatasource.Columns.Add(COLUMN_END_INTER);
            //this.cableAddDatasource.Columns.Add(COLUMN_END_POINT);

            //this.cableDelDatasource = this.cableAddDatasource.Clone();
        }

        private void EventHandlers()
        {
            this.btnCableConfig.Click += BtnCableConfig_Click;
            this.cb_startInterface.SelectedIndexChanged += Cb_startInterface_SelectedIndexChanged;
            this.cb_endInterface.SelectedIndexChanged += Cb_endInterface_SelectedIndexChanged;
            this.btnClose.Click += BtnClose_Click;
            this.btnSubmit.Click += BtnSubmit_Click;

            this.tool_addSignal.Click += Tool_addSignal_Click;
            this.tool_batchAdd.Click += Tool_batchAdd_Click;
            this.tool_delete.Click += Tool_delete_Click;
            this.tool_deleteAll.Click += Tool_deleteAll_Click;
            //this.tool_export.Click += Tool_export_Click;
            this.rdb2Method.CheckStateChanged += Rdb2Method_CheckStateChanged;
            this.rdb4Method.CheckStateChanged += Rdb4Method_CheckStateChanged;
            this.checkCircuit.CheckStateChanged += CheckCircuit_CheckStateChanged;
            this.checkInsulate.CheckStateChanged += CheckInsulate_CheckStateChanged;
            this.checkPressureProof.CheckStateChanged += CheckPressureProof_CheckStateChanged;
            this.checkConduction.CheckStateChanged += CheckConduction_CheckStateChanged;
        }

        private void CheckConduction_CheckStateChanged(object sender, EventArgs e)
        {
            if (this.checkConduction.CheckState == CheckState.Unchecked)
            {
                if (this.checkCircuit.Checked || this.checkInsulate.Checked || this.checkPressureProof.Checked)
                {
                    this.checkConduction.CheckState = CheckState.Checked;
                }
            }
        }

        private void CheckPressureProof_CheckStateChanged(object sender, EventArgs e)
        {
            if (this.checkPressureProof.Checked)
            {
                if (!this.IsShortCircuitCompletedTest)
                {
                    this.checkCircuit.CheckState = CheckState.Checked;
                }
            }
        }

        private void CheckInsulate_CheckStateChanged(object sender, EventArgs e)
        {
            if (this.checkInsulate.Checked)
            {
                if (!this.IsShortCircuitCompletedTest)
                {
                    this.checkCircuit.CheckState = CheckState.Checked;
                }
            }
        }

        private void CheckCircuit_CheckStateChanged(object sender, EventArgs e)
        {
            if (this.checkCircuit.Checked)
            {
                if (!this.IsConductCompletedTest)
                {
                    this.checkConduction.CheckState = CheckState.Checked;
                }
            }
            else
            {
                if (this.checkInsulate.Checked || this.checkPressureProof.Checked)
                {
                    this.checkCircuit.CheckState = CheckState.Checked;
                }
            }
        }

        private void Rdb4Method_CheckStateChanged(object sender, EventArgs e)
        {
            //CheckMeasureMethodValid();
        }

        private void Rdb2Method_CheckStateChanged(object sender, EventArgs e)
        {
            CheckMeasureMethodValid();
        }

        private void BtnSubmit_Click(object sender, EventArgs e)
        {
            SubmitData();
        }

        private void BtnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void Tool_export_Click(object sender, EventArgs e)
        {
            GridViewExport.ExportGridViewData(GridViewExport.ExportFormat.EXCEL, this.radGridView1);
        }

        private void Tool_deleteAll_Click(object sender, EventArgs e)
        {
            if (this.radGridView1.RowCount < 1)
                return;
            //if (IsInterUsed(this.lineCableName))
            //{
            //    MessageBox.Show($"线束{this.lineCableName}已被项目使用,删除失败！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            //    return;
            //}
            for (int i = this.radGridView1.RowCount - 1; i >= 0; i--)
            {
                AddDelCableInfo(i);
                this.radGridView1.Rows[i].Delete();
            }
        }

        private void Tool_delete_Click(object sender, EventArgs e)
        {
            int cIndex = this.radGridView1.CurrentRow.Index;
            if (cIndex < 0)
                return;
            //if (IsInterUsed(this.lineCableName))
            //{
            //    MessageBox.Show($"线束{this.lineCableName}已被项目使用,删除失败！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            //    return;
            //}
            AddDelCableInfo(cIndex);
            this.radGridView1.Rows[cIndex].Delete();
        }

        private void AddDelCableInfo(int rowIndex)
        {
            var startInterName = this.radGridView1.Rows[rowIndex].Cells[1].Value.ToString();
            var startContact = this.radGridView1.Rows[rowIndex].Cells[2].Value.ToString();
            var endInterName = this.radGridView1.Rows[rowIndex].Cells[3].Value.ToString();
            var endContact = this.radGridView1.Rows[rowIndex].Cells[4].Value.ToString();

            CableConfig cableConfig = new CableConfig();
            cableConfig.StartInterface = startInterName;
            cableConfig.StartInterPoint = startContact;
            cableConfig.EndInterface = endInterName;
            cableConfig.EndInterPoint = endContact;
            this.cableDelList.Add(cableConfig);

            var curExistList = this.cableAddList.Find(m => m.StartInterface == startInterName && m.StartInterPoint == startContact && m.EndInterface == endInterName && m.EndInterPoint == endContact);
            if (curExistList != null)
            {
                this.cableAddList.Remove(curExistList);
            }

            //DataRow dataRow = this.cableDelDatasource.NewRow();
            //dataRow[COLUMN_START_INTER] = startInterName;
            //dataRow[COLUMN_START_POINT] = startContact;
            //dataRow[COLUMN_END_INTER] = endInterName;
            //dataRow[COLUMN_END_POINT] = endContact;
            //this.cableDelDatasource.Rows.Add(dataRow);
        }

        private bool IsInterUsed(string cableName)
        {
            TProjectBasicInfoManager libraryManager = new TProjectBasicInfoManager();
            var data = libraryManager.GetDataSetByFieldsAndWhere("DISTINCT ProjectName", $"where TestCableName = '{cableName}'").Tables[0];
            if (data.Rows.Count > 0)
            {
                return true;
            }
            return false;
        }

        private void Tool_batchAdd_Click(object sender, EventArgs e)
        {
            BatchAddConnect();
        }

        private void Tool_addSignal_Click(object sender, EventArgs e)
        {
            AddSignalConnect(this.cb_startPin.Text,this.cb_endPin.Text);
        }

        private void Cb_endInterface_SelectedIndexChanged(object sender, EventArgs e)
        {
            BindContactDataByInterface(this.cb_endInterface.Text, this.cb_endPin);
        }

        private void Cb_startInterface_SelectedIndexChanged(object sender, EventArgs e)
        {
            BindContactDataByInterface(this.cb_startInterface.Text, this.cb_startPin);
        }

        private void BtnCableConfig_Click(object sender, EventArgs e)
        {
            var cableName = this.rtbCableName.Text;
            if (cableName == "")
            {
                MessageBox.Show("线束代号不能为空！","Warning",MessageBoxButtons.OK,MessageBoxIcon.Warning);
                return;
            }
            if (!this.IsEditView)
            {
                if (IsCableNoExist(cableName))
                {
                    MessageBox.Show("线束代号已存在，请重新命名！", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
            RadCableLibraryManager radCableLibraryManager = new RadCableLibraryManager(cableName);
            if (radCableLibraryManager.ShowDialog() == DialogResult.OK)
            {
                var plugLibraryList = RadCableLibraryManager.interLibraryDetailList;
                this.cb_endInterface.EditorControl.Rows.Clear();
                this.cb_startInterface.EditorControl.Rows.Clear();
                this.cb_startPin.EditorControl.Rows.Clear();
                this.cb_endPin.EditorControl.Rows.Clear();
                
                foreach (var library in plugLibraryList)
                {
                    this.cb_startInterface.EditorControl.Rows.Add(library.InterfaceNo);
                    InitCableParams(library.InterfaceNo);
                }
                if (this.cb_startInterface.EditorControl.RowCount > 0)
                {
                    this.cb_startInterface.SelectedIndex = 0;
                }
                foreach (var library in plugLibraryList)
                {
                    this.cb_endInterface.EditorControl.Rows.Add(library.InterfaceNo);
                }
                if (this.cb_endInterface.EditorControl.Rows.Count > 0)
                {
                    //this.cb_endInterface.SelectedIndex = 0;
                    //BindContactDataByInterface(this.cb_endInterface.Text, this.cb_endPin);
                }
                if (this.cb_startInterface.EditorControl.Rows.Count > 0)
                {
                    //this.cb_startInterface.SelectedIndex = 0;
                    //BindContactDataByInterface(this.cb_startInterface.Text, this.cb_startPin);
                }
                this.cb_startInterface.EditorControl.ShowColumnHeaders = false;
                this.cb_startInterface.EditorControl.AutoSizeColumnsMode = GridViewAutoSizeColumnsMode.Fill;
                this.cb_endInterface.EditorControl.ShowColumnHeaders = false;
                this.cb_endInterface.EditorControl.AutoSizeColumnsMode =  GridViewAutoSizeColumnsMode.Fill;
            }
        }

        private void InitCableParams(string plugno)
        {
            int methodIndex = 2;
            if (this.rdb2Method.IsChecked)
                methodIndex = 2;
            else if (this.rdb4Method.IsChecked)
                methodIndex = 4;
            var resultList = ContactPointReSort(plugno, methodIndex);
            if (resultList.Count == 0)
                return;
            int i = 0;
            foreach (var point in resultList)
            {
                CableLibParams cableLibParams = new CableLibParams();
                cableLibParams.InterfaceName = plugno;
                cableLibParams.InterContactPoint = (i + 1).ToString();
                cableLibParams.DevInterPoint = point.ToString();
                var cableList = this.cableLibList.Find(obj => obj.InterfaceName == plugno && obj.InterContactPoint == point.ToString()); ;
                if (cableList == null)
                {
                    this.cableLibList.Add(cableLibParams);
                }
                i++;
            }
        }

        private bool IsCableNoExist(string cableName)
        {
            var count = lineStructManager.GetRowCountByWhere($"where CableName = '{cableName}'");
            if (count > 0)
                return true;
            return false;
        }

        private void BindContactDataByInterface(string plugno, RadMultiColumnComboBox cbPin)
        {
            //此处省略排序规则：字符串混合排序
            this.Invoke(new Action(() =>
            {
            var methodIndex = 2;
            if (this.rdb2Method.IsChecked)
                methodIndex = 2;
            else if (this.rdb4Method.IsChecked)
                methodIndex = 4;
                cbPin.EditorControl.Rows.Clear();
                var resultList = ContactPointReSort(plugno, methodIndex);
                if (resultList.Count == 0)
                    return;
                this.cb_startPin.EditorControl.BeginEdit();
                int iStart = 0;
                foreach (var point in resultList)
                {
                    cbPin.EditorControl.Rows.Add(iStart + 1);
                    iStart++;
                }
                cbPin.EditorControl.EndEdit();
                cbPin.SelectedIndex = 0;
                cbPin.EditorControl.ShowColumnHeaders = false;
                cbPin.EditorControl.ClearSelection();
                cbPin.EditorControl.AutoSizeColumnsMode = GridViewAutoSizeColumnsMode.Fill;
            }));
        }

        private List<int> ContactPointReSort(string plugno,int methodIndex)
        {
            List<int> list = new List<int>();
            var selectSQL = $"where InterfaceNo='{plugno}' and MeasureMethod = '{methodIndex}'";
            var dt = plugLibraryDetailManager.GetDataSetByFieldsAndWhere("SwitchStandStitchNo", selectSQL).Tables[0];
            if (dt.Rows.Count < 1)
                return list;

            foreach (DataRow dr in dt.Rows)
            {
                int value = 0;
                var v = dr[0].ToString();
                if (methodIndex == 2)
                {
                    if (int.TryParse(v, out value))
                    {
                        if(!list.Contains(value))
                            list.Add(value);
                    }
                }
                else if (methodIndex == 4)
                {
                    if (v.Contains(","))
                    {
                        var v1 = v.Substring(0,v.IndexOf(','));
                        if (int.TryParse(v1, out value))
                        {
                            if(!list.Contains(value))
                                list.Add(value);
                        }
                        var v2 = v.Substring(v.IndexOf(',') + 1);
                        if (int.TryParse(v2, out value))
                        { 
                            if(!list.Contains(value))
                                list.Add(value);
                        }
                    }
                }
            }
            list.Sort();
            return list;
        }

        private void CheckMeasureMethodValid()
        {
            #region close
            //var plugLibraryList = RadCableLibraryManager.interLibraryDetailList;
            //var startPlugLibrary = plugLibraryList.Find(plug => plug.InterfaceNo == this.cb_startInterface.Text);
            //var endPlugLibrary = plugLibraryList.Find(plug => plug.InterfaceNo == this.cb_endInterface.Text);
            //if (this.rdb2Method.CheckState == CheckState.Checked)
            //{
            //    if (this.cb_startInterface.Text == this.cb_endInterface.Text)
            //    {
            //        if (startPlugLibrary.MeasureMethod != "1")
            //        {
            //            //MessageBox.Show($"接口{this.cb_startInterface.Text}未定义为二线法，请重新选择！", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            //        }
            //    }
            //    else
            //    {
            //        if (startPlugLibrary.MeasureMethod != "2" && endPlugLibrary.MeasureMethod != "2")
            //        {
            //            //MessageBox.Show($"接口{this.cb_startInterface.Text}与接口{cb_endInterface.Text}均未定义为二线法，请重新选择！", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            //        }
            //        else if (startPlugLibrary.MeasureMethod != "2" && endPlugLibrary.MeasureMethod == "2")
            //        {
            //            //MessageBox.Show($"接口{this.cb_startInterface.Text}未定义为二线法，请重新选择！", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            //        }
            //        else if (startPlugLibrary.MeasureMethod == "2" && endPlugLibrary.MeasureMethod != "2")
            //        {
            //            //MessageBox.Show($"接口{this.cb_endInterface.Text}未定义为二线法，请重新选择！", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            //        }
            //    }
            //}
            //else if (this.rdb4Method.CheckState == CheckState.Checked)
            //{
            //    if (this.cb_startInterface.Text == this.cb_endInterface.Text)
            //    {
            //        if (startPlugLibrary.MeasureMethod != "4")
            //        {
            //            //MessageBox.Show($"接口{this.cb_startInterface.Text}未定义为四线法，请重新选择！", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            //        }
            //    }
            //    else
            //    {
            //        if (startPlugLibrary.MeasureMethod != "4" && endPlugLibrary.MeasureMethod != "4")
            //        {
            //            //MessageBox.Show($"接口{this.cb_startInterface.Text}与接口{cb_endInterface.Text}均未定义为四线法，请重新选择！", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            //        }
            //        else if (startPlugLibrary.MeasureMethod != "4" && endPlugLibrary.MeasureMethod == "4")
            //        {
            //            //MessageBox.Show($"接口{this.cb_startInterface.Text}未定义为四线法，请重新选择！", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            //        }
            //        else if (startPlugLibrary.MeasureMethod == "4" && endPlugLibrary.MeasureMethod != "4")
            //        {
            //            //MessageBox.Show($"接口{this.cb_endInterface.Text}未定义为四线法，请重新选择！", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            //        }
            //    }
            //}
            #endregion

            BindContactDataByInterface(this.cb_endInterface.Text, this.cb_endPin);
            BindContactDataByInterface(this.cb_startInterface.Text, this.cb_startPin);
        }

        /*
         * 添加规则
         * 【二线法】
         * 1）起点接口与终点接口为同一个接口：
         *  起点接点为起始点，长度为终点接点减去起点接点
         * 
         */ 
        private void AddSignalConnect(string startContact,string endContact)
        {
            if (!IsInterfaceCfgValid())
                return;
            //判断是否满足添加条件
            if (IsExistConnect(startContact, endContact))
            {
                MessageBox.Show($"已存在连接关系起点接点{startContact}与终点接点{endContact}", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            //add to radgridview
            this.radGridView1.Rows.AddNew();
            var rowCount = this.radGridView1.RowCount;
            this.radGridView1.Rows[rowCount - 1].Cells[0].Value = rowCount;
            this.radGridView1.Rows[rowCount - 1].Cells[1].Value = this.cb_startInterface.Text;
            this.radGridView1.Rows[rowCount - 1].Cells[2].Value = startContact;
            this.radGridView1.Rows[rowCount - 1].Cells[3].Value = this.cb_endInterface.Text;
            this.radGridView1.Rows[rowCount - 1].Cells[4].Value = endContact;

            CableConfig cableConfig = new CableConfig();
            cableConfig.StartInterface = this.cb_startInterface.Text;
            cableConfig.StartInterPoint = startContact;
            cableConfig.EndInterface = this.cb_endInterface.Text;
            cableConfig.EndInterPoint = endContact;
            this.cableAddList.Add(cableConfig);

            //DataRow dataRow = this.cableAddDatasource.NewRow();
            //dataRow[COLUMN_ORDER] = rowCount;
            //dataRow[COLUMN_START_INTER] = this.cb_startInterface.Text;
            //dataRow[COLUMN_START_POINT] = startContact;
            //dataRow[COLUMN_END_INTER] = this.cb_endInterface.Text;
            //dataRow[COLUMN_END_POINT] = endContact;
            //this.cableAddDatasource.Rows.Add(dataRow);
        }

        private void BatchAddConnect()
        {
            if (!IsInterfaceCfgValid())
                return;

            //当前能建立连接关系的最大范围
            List<string> startContactList = new List<string>();
            List<string> endContactList = new List<string>();
            int contactMax = 0;
            if (this.cb_startInterface.Text == this.cb_endInterface.Text)
            {
                if (this.cb_startPin.Text != this.cb_endPin.Text)
                {
                    for (int i = this.cb_startPin.SelectedIndex; i < this.cb_endPin.SelectedIndex; i++)
                    {
                        startContactList.Add(this.cb_startPin.EditorControl.Rows[i].Cells[0].Value.ToString());
                    }
                    for (int i = this.cb_endPin.SelectedIndex; i < this.cb_endPin.EditorControl.Rows.Count; i++)
                    {
                        endContactList.Add(this.cb_endPin.EditorControl.Rows[i].Cells[0].Value.ToString());
                    }
                }
            }
            else
            {
                for (int i = this.cb_startPin.SelectedIndex; i < this.cb_startPin.EditorControl.Rows.Count; i++)
                {
                    startContactList.Add(this.cb_startPin.EditorControl.Rows[i].Cells[0].Value.ToString());
                }
                for (int i = this.cb_endPin.SelectedIndex; i < this.cb_endPin.EditorControl.Rows.Count; i++)
                {
                    endContactList.Add(this.cb_endPin.EditorControl.Rows[i].Cells[0].Value.ToString());
                }
            }

            if (startContactList.Count <= endContactList.Count)
                contactMax = startContactList.Count;
            else
                contactMax = endContactList.Count;

            BatchAddCableSet batchAddCableSet = new BatchAddCableSet(contactMax,0);
            if (batchAddCableSet.ShowDialog() == DialogResult.OK)
            {
                var batchAddCount = BatchAddCableSet.currentValue;
                for (int i = 0; i < batchAddCount; i++)
                {
                    AddSignalConnect(startContactList[i],endContactList[i]);
                }
            }
        }


        private bool IsInterfaceCfgValid()
        {
            if (this.rtbCableName.Text.Trim() == "")
            {
                MessageBox.Show("线束代号不能为空！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            if (this.cb_startInterface.Text.Trim() == "")
            {
                MessageBox.Show("未设置起点接口！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            if (this.cb_endInterface.Text.Trim() == "")
            {
                MessageBox.Show("未设置终点接口！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            if (this.cb_startPin.Text.Trim() == "")
            {
                MessageBox.Show("未设置起点接点！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            if (this.cb_endPin.Text.Trim() == "")
            {
                MessageBox.Show("未设置终点接点！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            return true;
        }

        private bool IsExistConnect(string startContact,string endContact)
        {
            if (this.radGridView1.RowCount < 1)
                return false;
            foreach (var rowInfo in this.radGridView1.Rows)
            {
                if (rowInfo.Cells[2].Value.ToString() == startContact && rowInfo.Cells[4].Value.ToString() == endContact)
                {
                    return true;
                }
            }
            return false;
        }

        private void SubmitData()
        {
            if (this.radGridView1.Rows.Count < 1)
                return;

            if (this.IsEditView)
            {
                if (UpdateCableInfo() + AddCableInfo() + UpdateCableName() > 0)
                {
                    MessageBox.Show("更新成功！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            else
            {
                if (this.rtbCableName.Text.Trim() == "")
                {
                    MessageBox.Show("线束代号不能为空！", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (AddCableInfo() > 0)
                {
                    MessageBox.Show("更新成功！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            this.Close();
            this.DialogResult = DialogResult.OK;
        }

        private int UpdateCableName()
        {
            if (this.IsEditView)
            {
                if (this.lineCableName.Trim() != this.rtbCableName.Text.Trim() || this.remark != this.rtb_remark.Text.Trim())//名称已修改
                {
                    var row = this.lineStructManager.UpdateFields($"CableName = '{this.rtbCableName.Text.Trim()}', Remark = '{this.rtb_remark.Text.Trim()}'", $"where CableName = '{this.lineCableName}'");
                    return row;
                }
            }
            return 0;
        }

        private int AddCableInfo()
        {
            int rowCount = 0;
            List<TCableTestLibrary> cableLibList = new List<TCableTestLibrary>();
            foreach (var rowInfo in this.cableAddList)
            {
                var startInterface = rowInfo.StartInterface;
                var startContactPoint = rowInfo.StartInterPoint;
                var endInterface = rowInfo.EndInterface;
                var endContactPoint = rowInfo.EndInterPoint;

                TCableTestLibrary lineStructLibraryDetail = new TCableTestLibrary();
                lineStructLibraryDetail.ID = CableTestManager.Common.TablePrimaryKey.InsertCableLibPID() + rowCount;
                lineStructLibraryDetail.CableName = this.rtbCableName.Text.Trim();
                lineStructLibraryDetail.Remark = this.rtb_remark.Text;
                lineStructLibraryDetail.StartInterface = startInterface;
                lineStructLibraryDetail.StartContactPoint = startContactPoint;
                var cStartObj = this.cableLibList.Find(obj => obj.InterfaceName == startInterface && obj.InterContactPoint == startContactPoint);
                if (cStartObj != null)
                {
                    lineStructLibraryDetail.StartDevPoint = cStartObj.DevInterPoint;
                }
                lineStructLibraryDetail.EndInterface = endInterface;
                lineStructLibraryDetail.EndContactPoint = endContactPoint;
                var cEndObj = this.cableLibList.Find(obj => obj.InterfaceName == endInterface && obj.InterContactPoint == endContactPoint);
                if (cEndObj != null)
                {
                    lineStructLibraryDetail.EndDevPoint = cEndObj.DevInterPoint;
                }
                lineStructLibraryDetail.MeasureMethod = QueryMeasuringMethod(startInterface).ToString();
                lineStructLibraryDetail.UpdateDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                lineStructLibraryDetail.Operator = LocalLogin.currentUserName;
                cableLibList.Add(lineStructLibraryDetail);
                rowCount++;
            }
            var row = lineStructManager.Insert(cableLibList);
            this.cableAddList.Clear();
            return row;
        }

        private int UpdateCableInfo()
        {
            int rowCount = 0;
            foreach (var rowInfo in this.cableDelList)
            {
                var startInterface = rowInfo.StartInterface;
                var startContactPoint = rowInfo.StartInterPoint;
                var endInterface = rowInfo.EndInterface;
                var endContactPoint = rowInfo.EndInterPoint;

                TCableTestLibrary lineStructLibraryDetail = new TCableTestLibrary();
                lineStructLibraryDetail.ID = GetCablePrimary(this.rtbCableName.Text.Trim(), startInterface, startContactPoint, endInterface, endContactPoint);
                rowCount += this.lineStructManager.Delete(lineStructLibraryDetail.ID);
            }
            this.cableDelList.Clear();
            return rowCount;
        }

        private int QueryMeasuringMethod(string PlugNo)
        {
            int method;
            InterfaceInfoLibraryManager plugLibraryDetailManager = new InterfaceInfoLibraryManager();
            var dt = plugLibraryDetailManager.GetDataSetByFieldsAndWhere("distinct MeasureMethod", $"where InterfaceNo='{PlugNo}'").Tables[0];
            if (dt.Rows.Count < 1)
                return 0;
            int.TryParse(dt.Rows[0][0].ToString().Trim(),out method);
            return method;
        }

        private void GetLineStructDetailData(string plugCableName)
        {
            if (plugCableName == "")
                return;
            TCableTestLibraryManager lineStructManager = new TCableTestLibraryManager();
            var data = lineStructManager.GetDataSetByWhere($"where CableName = '{plugCableName}'").Tables[0];
            if (data.Rows.Count < 1)
                return;
            List<string> interfaceList = new List<string>();
            List<int> interfacePointList = new List<int>();
            this.Invoke(new Action(() =>
            {
                this.rtbCableName.Text = this.lineCableName;
                this.rtb_remark.Text = data.Rows[0]["Remark"].ToString();
                var dt = lineStructManager.GetDataSetByFieldsAndWhere("DISTINCT StartInterface,EndInterface,StartContactPoint,EndContactPoint", $"where CableName = '{this.lineCableName}'").Tables[0];
                if (dt.Rows.Count > 0)
                {
                    this.cb_startInterface.EditorControl.Rows.Clear();
                    this.cb_endInterface.EditorControl.Rows.Clear();
                    this.cb_startPin.EditorControl.Rows.Clear();
                    this.cb_endPin.EditorControl.Rows.Clear();

                    foreach (DataRow dr in dt.Rows)
                    {
                        var startInterface = dr["StartInterface"].ToString();
                        var endInterface = dr["EndInterface"].ToString();
                        var startContactPoint = int.Parse(dr["StartContactPoint"].ToString());
                        var endContactPoint = int.Parse(dr["EndContactPoint"].ToString());

                        if (!interfaceList.Contains(startInterface))
                            interfaceList.Add(startInterface);
                        if (!interfaceList.Contains(endInterface))
                            interfaceList.Add(endInterface);
                        if (!interfacePointList.Contains(startContactPoint))
                            interfacePointList.Add(startContactPoint);
                        if (!interfacePointList.Contains(endContactPoint))
                            interfacePointList.Add(endContactPoint);
                    }
                    interfacePointList.Sort();

                    int iStart = 1;
                    foreach (var pointString in interfacePointList)
                    {
                        if (!IsExistCombox(this.cb_startPin, iStart.ToString()))
                            this.cb_startPin.EditorControl.Rows.Add(iStart);
                        if (!IsExistCombox(this.cb_endPin, iStart.ToString()))
                            this.cb_endPin.EditorControl.Rows.Add(iStart);
                        iStart++;
                    }
                    this.cb_startPin.EditorControl.ShowColumnHeaders = false;
                    this.cb_startPin.EditorControl.AllowAutoSizeColumns = true;
                    this.cb_endPin.EditorControl.ShowColumnHeaders = false;
                    this.cb_endPin.EditorControl.AllowAutoSizeColumns = true;

                    foreach (var interString in interfaceList)
                    {
                        if (!IsExistInterfaceCombox(interString))
                            this.cb_startInterface.EditorControl.Rows.Add(interString);
                        if (!IsExistEndInterfaceCombox(interString))
                            this.cb_endInterface.EditorControl.Rows.Add(interString);
                        InitCableParams(interString);
                    }
                    this.cb_startInterface.EditorControl.ShowColumnHeaders = false;
                    this.cb_startInterface.EditorControl.AutoSizeColumnsMode = GridViewAutoSizeColumnsMode.Fill;
                    this.cb_endInterface.EditorControl.ShowColumnHeaders = false;
                    this.cb_endInterface.EditorControl.AutoSizeColumnsMode =  GridViewAutoSizeColumnsMode.Fill;
                }
                int iRow = 0;
                foreach (DataRow dr in data.Rows)
                {
                    this.radGridView1.Rows.AddNew();
                    this.radGridView1.Rows[iRow].Cells[0].Value = iRow + 1;
                    this.radGridView1.Rows[iRow].Cells[1].Value = dr["StartInterface"].ToString();
                    this.radGridView1.Rows[iRow].Cells[2].Value = dr["StartContactPoint"].ToString();
                    this.radGridView1.Rows[iRow].Cells[3].Value = dr["EndInterface"].ToString();
                    this.radGridView1.Rows[iRow].Cells[4].Value = dr["EndContactPoint"].ToString();
                    iRow++;
                }
            }));
        }

        private bool IsExistCombox(RadMultiColumnComboBox interfaceCombox,string inputValue)
        {
            if (interfaceCombox.EditorControl.RowCount < 1)
                return false;
            foreach (var rowInfo in interfaceCombox.EditorControl.Rows)
            {
                var value = rowInfo.Cells[0].Value.ToString();
                if (value == inputValue)
                    return true;
            }
            return false;
        }

        private bool IsExistInterfaceCombox(string inputValue)
        {
            if (this.cb_startInterface.EditorControl.RowCount < 1)
                return false;
            foreach (var rowInfo in this.cb_startInterface.EditorControl.Rows)
            {
                var value = rowInfo.Cells[0].Value.ToString();
                if (value == inputValue)
                    return true;
            }
            return false;
        }

        private bool IsExistEndInterfaceCombox(string inputValue)
        {
            if (this.cb_endInterface.EditorControl.RowCount < 1)
                return false;
            foreach (var rowInfo in this.cb_endInterface.EditorControl.Rows)
            {
                var value = rowInfo.Cells[0].Value.ToString();
                if (value == inputValue)
                    return true;
            }
            return false;
        }

        private bool IsExistLineStructRecord(string primaryID)
        {
            TCableTestLibraryManager lineStructManager = new TCableTestLibraryManager();
            var lineStructRecord = lineStructManager.GetById(primaryID);
            if (lineStructManager == null)
                return false;
            return true;
        }

        private int GetCablePrimary(string LineStructName, string StartInterface, string StartContactPoint,string EndInterface,string EndContactPoint)
        {
            int ID;
            var dt = lineStructManager.GetDataSetByWhere($"where CableName='{LineStructName}' and StartInterface='{StartInterface}' " +
                $"and StartContactPoint='{StartContactPoint}' and EndInterface = '{EndInterface}' and EndContactPoint= '{EndContactPoint}'").Tables[0];
            if (dt.Rows.Count > 0)
            {
                int.TryParse(dt.Rows[0]["ID"].ToString(), out ID);
                return ID;
            }
            return 0;
        }
    }
}
