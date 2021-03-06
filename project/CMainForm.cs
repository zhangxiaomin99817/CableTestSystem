﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Telerik.WinControls;
using Telerik.WinControls.UI;
using CableTestManager.DeviceUtils;
using CableTestManager.Business;
using CableTestManager.Business.Implements;
using CableTestManager.Entity;
using CableTestManager.View.VInterface;
using CableTestManager.View.VProject;
using CableTestManager.View.VSelfStydy;
using CableTestManager.View.VEquipment;
using WindowsFormTelerik.ControlCommon;
using CableTestManager.CUserManager;
using CableTestManager.ClientSocket;
using CommonUtils.Logger;
using CommonUtils.FileHelper;
using CommonUtil.CUserManager;
using CommonUtils.ByteHelper;
using System.IO;
using CableTestManager.Properties;
using CableTestManager.Model;
using CableTestManager.ClientSocket.AppBase;
using CableTestManager.Common;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;

namespace CableTestManager.View
{
    public partial class CMainForm : Telerik.WinControls.UI.RadForm
    {
        private KLineTestProcessor gLineTestProcessor;
        private TProjectBasicInfoManager projectInfoManager;
        private THistoryDataDetailManager historyDataDetailManager;
        private THistoryDataBasicManager historyDataInfoManager;
        private TCableTestLibraryManager lineStructLibraryDetailManager;
        private CableJudgeThreshold cableJudgeThreshold;
        private TProjectBasicInfo projectInfo;
        private System.Timers.Timer sysTimer;
        private System.Timers.Timer refreshDataTimer;

        private bool IsConnectServer;
        private string serviceURL = "";
        private int servicePort;
        private string currentTestProject;
        
        //当前测试项目的测试项的异常数
        private int conductTestExceptCount;
        private int shortCircuitTestExceptCount;
        private int insulateTestExceptCount;
        private int voltageWithStandardTestExceptCount;
        
        //测试项阈值：判断是否合格的标准值，打开当前项目时更新测试标准
        private double conductPassThreshold;
        private double shortCircuitPassThreshold;
        private double insulatePassThreshold;
        private double voltageWithStandardPassThreshold;

        private double environmentTemperature;
        private double environmentAmbientHumidity;
        private string deviceTypeNo;
        /// <summary>
        /// 项目测试序列，每次启动测试便生成一个新序列
        /// </summary>
        private string projectTestNumber;

        /// <summary>
        /// 项目测试序列长度，超过最大长度会自动延长
        /// </summary>
        private int projectTestNumberLen = 8;
        private string startTestDate;
        private string endTestDate;

        #region 测试项与逻辑
        /*
         * 导通测试完成后，开放短路测试
         * 短路测试完成后，开放绝缘测试与耐压测试
         * 一键测试：可配置一键测试的测试项
         * 
         *需要读取各项测试的测试状态：是否完成过测试
         * 每个测试序列/测试线束，每次完成测试后存储测试状态，若此测试线束发生修改后是否更新测试状态为-未测试
         * 
         */

        /// <summary>
        /// 是否完成导通测试（为当前，不同步过去是否完成测试）
        /// </summary>
        private bool IsPassConductionTest;

        /// <summary>
        /// 是否完成短路测试
        /// </summary>
        private bool IsPassShortCircuitTest;

        /// <summary>
        /// 是否完成绝缘测试
        /// </summary>
        private bool IsPassInsulateTest;

        /// <summary>
        /// 是否完成耐压测试
        /// </summary>
        private bool IsPassVoltageWithStandardTest;

        /// <summary>
        /// 是否完成自学习测试
        /// </summary>
        private bool IsPassSelfStudy;

        #endregion

        #region 一键测试变量
        /// <summary>
        /// 是否启动一键测试
        /// </summary>
        private bool IsStartOneClickTest;

        /// <summary>
        /// 是否启动导通测试
        /// </summary>
        private bool IsStartConductTest;

        /// <summary>
        /// 是否启动短路测试
        /// </summary>
        private bool IsStartShortCircuitTest;

        /// <summary>
        /// 是否启动绝缘测试
        /// </summary>
        private bool IsStartInsulateTest;

        /// <summary>
        /// 是否启动耐压测试
        /// </summary>
        private bool IsStartVoltageWithStandardTest;
        #endregion

        #region 测试项最新完成状态，每次测试有完成即更新测试状态
        private bool IsConductLatestCompleteStatus;
        private bool IsShortCircuitLatestCompleteStatus;
        private bool IsInsulateLatestCompleteStatus;
        private bool IsVoltageWithStandardLatestCompleteStatus;
        #endregion

        #region INI CONFIG FILE FOR FIXED TESTER STANDARD DEFAULT PARAMS

        private string CONDUCTION_THRESHOLD = "100";
        private string IS_CONDUCTION_JUDGE_THAN_THRESHOLD = "0";//小于或等于标准值
        
        private string INSULATE_THRESHOLD = "20";
        private string IS_INSULATE_JUDGE_THAN_THRESHOLD = "1";
        private string INSULATE_VOLTAGE = "100";
        private string INSULATE_HOLD_TIME = "1";

        private string PRESSURE_PROOF_THRESHOLD = "2";
        private string IS_PRESSURE_JUDGE_THAN_THRESHOLD = "0";
        private string PRESSURE_PROOF_VOLTAGE = "100";
        private string PRESSURE_PROOF_HOLD_TIME = "1";

        private const string CONFIG_DIRECTORY_NAME = "CONFIG";
        private const string CONFIG_SECTION_NAME = "FIX_STANDARD_PARAMS";
        private const string CONFIG_SECTION_TEST_NAME = "TEST_INFO";
        private const string PROJECT_TEST_NUMBER_LEN_KEY = "test_number_length";
        private const string CONDUCTION_THRESHOLD_KEY = "conduction_threshold";
        private const string IS_CONDUCTION_JUDGE_THAN_THRESHOLD_KEY = "conduction_judge_than_threshold";
        private const string INSULATE_THRESHOLD_KEY = "insulate_threshold";
        private const string IS_INSULATE_JUDGE_THAN_THRESHOLD_KEY = "insulate_judge_than_threshold";
        private const string INSULATE_VOLTAGE_KEY = "insulate_voltage";
        private const string INSULATE_HOLD_TIME_KEY = "insulate_hold_time";
        private const string PRESSURE_PROOF_THRESHOLD_KEY = "pressure_proof_threshold";
        private const string IS_PRESSURE_JUDGE_THAN_THRESHOLD_KEY = "is_pressure_judge_than_threshold";
        private const string PRESSURE_PROOF_VOLTAGE_KEY = "pressure_proof_voltage";
        private const string PRESSURE_PROOF_HOLD_TIME_KEY = "pressure_proof_hold_time";
        #endregion

        #region 常量
        public const string CableTestSystemName = "线束测试系统";

        private string configPath;
        private const string DEVICE_CONFIG_FILE_NAME = "DeviceConfig.ini";
        private const string DEVICE_CONFIG_SECTION = "service";
        private const string DEVICE_CONFIG_IS_AUTO_KEY = "IsAutoConnect";
        private const string DEVICE_CONFIG_SERVER_URL_KEY = "serviceURL";
        private const string DEVICE_CONFIG_SERVER_PORT_KEY = "servicePort";

        private const string PROJECT_INFO = "工程信息";
        private const string PROJECT_OPEN_TEST = "打开测试";
        private const string PROJECT_CLOSE_TEST = "关闭测试";
        private const string PROJECT_EDIT = "编辑项目";
        private const string WORK_WEAR = "查看工装";
        private const string TEST_CABLE = "被测线束";
        private const string CABLE_DETAIL = "查看线束";
        private const string DEVICE_INFO = "设备信息";
        private const string DEVICE_DETAIL = "查看设备";

        private const string headHexString = "FF FF";
        private const string conductionTestFunCode = "F2 AA";
        private const string shortCircuitTestFunCode = "F3 AA";
        private const string insulateTestFunCode = "F4 AA";
        private const string voltageWithStandardFunCode = "F5 AA";
        private const string selfStudyTestFunCode = "F1 AA";
        #endregion

        private DoubleBufferListView listViewSelfStudy;
        private double selfStudyThreshold;//自学习设定的标准值
        private List<ListViewItem> selfStudyListView;
        private List<ListViewItem> cableTestListView;
        private List<SelfStudyParams> studyParamList;
        private List<CableTestParams> cableTestPramsList;

        public CMainForm()
        {
            InitializeComponent();
        }

        private void CMainForm_Load(object sender, EventArgs e)
        {
            
        }

        public void Init()
        {
            this.lbx_curLoginUser.Text = "";
            this.lbx_conDevStatus.Text = "未连接";
            this.lbx_serviceURL.Text = "";
            this.lbx_servicePort.Text = "";
            this.lbx_monitorDate.Text = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            //this.StartPosition = FormStartPosition.CenterScreen;
            //this.WindowState = FormWindowState.Maximized;
            //this.radDock1.DocumentTabsVisible = false;
            this.radDock1.RemoveAllDocumentWindows();
            //this.radDock1.AddDocument(this.documentWindow1);
            InitDatable(false);

            this.listViewSelfStudy = new DoubleBufferListView();
            this.panelView.Controls.Add(this.listViewSelfStudy);
            this.listViewSelfStudy.Dock = DockStyle.Fill;
            this.selfStudyListView = new List<ListViewItem>();
            this.cableTestListView = new List<ListViewItem>();
            this.studyParamList = new List<SelfStudyParams>();
            this.cableTestPramsList = new List<CableTestParams>();
            this.projectInfoManager = new TProjectBasicInfoManager();
            this.historyDataInfoManager = new THistoryDataBasicManager();
            this.historyDataDetailManager = new THistoryDataDetailManager();
            lineStructLibraryDetailManager = new TCableTestLibraryManager();
            refreshDataTimer = new System.Timers.Timer();
            refreshDataTimer.Interval = 500;
            refreshDataTimer.AutoReset = true;

            projectInfo = new TProjectBasicInfo();
            sysTimer = new System.Timers.Timer();
            sysTimer.Start();

            #region update control status
            this.menu_InsulationTest.Enabled = true;
            this.menu_shortCircuitTest.Enabled = true;
            this.menu_VoltageWithStandTest.Enabled = true;
            #endregion

            configPath = AppDomain.CurrentDomain.BaseDirectory + "config\\";
            if (!Directory.Exists(configPath))
                Directory.CreateDirectory(configPath);
            this.lbx_curLoginUser.Text = LocalLogin.CurrentUserName;
            InitDefaultConfig();
            UpdateTreeView();
            EventHandles();
        }

        private void InitDatable(bool IsSelfStudy)
        {
            if (IsSelfStudy)
            {
                if (this.listViewSelfStudy.Columns.Count > 0)
                    return;
                this.listViewSelfStudy.Columns.Add("序号");
                this.listViewSelfStudy.Columns.Add("起点接点");
                this.listViewSelfStudy.Columns.Add("终点接点");
                this.listViewSelfStudy.Columns.Add("测量值(Ω)");
                this.listViewSelfStudy.Columns.Add("测量结果");

                this.listViewSelfStudy.GridLines = false;
                this.listViewSelfStudy.FullRowSelect = true;
                this.listViewSelfStudy.View = System.Windows.Forms.View.Details;
                this.listViewSelfStudy.Scrollable = true;
                this.listViewSelfStudy.MultiSelect = false;
                this.listViewSelfStudy.HeaderStyle = ColumnHeaderStyle.Clickable;

                SetListViewSize();
                this.listViewSelfStudy.Font = new Font("宋体", 14f, FontStyle.Regular);

                this.listViewSelfStudy.VirtualMode = true;
            }
            else
            {
                this.listViewCableTest.Columns.Add("序号");
                this.listViewCableTest.Columns.Add("起点接口");
                this.listViewCableTest.Columns.Add("起点接点");
                this.listViewCableTest.Columns.Add("终点接口");
                this.listViewCableTest.Columns.Add("终点接点");
                this.listViewCableTest.Columns.Add("导通电阻(Ω)");
                this.listViewCableTest.Columns.Add("导通测试结果");
                this.listViewCableTest.Columns.Add("短路测试(Ω)");
                this.listViewCableTest.Columns.Add("短路测试结果");
                this.listViewCableTest.Columns.Add("绝缘电阻(Ω)");
                this.listViewCableTest.Columns.Add("绝缘测试结果");
                this.listViewCableTest.Columns.Add("耐压电流(Ω)");
                this.listViewCableTest.Columns.Add("耐压测试结果");
                this.listViewCableTest.Columns.Add("testMethod");

                this.listViewCableTest.GridLines = false;
                this.listViewCableTest.FullRowSelect = true;
                this.listViewCableTest.View = System.Windows.Forms.View.Details;
                this.listViewCableTest.Scrollable = true;
                this.listViewCableTest.BackColor = Color.Azure;
                this.listViewCableTest.MultiSelect = false;
                this.listViewCableTest.HeaderStyle = ColumnHeaderStyle.Clickable;
                this.listViewCableTest.GridLines = true;
                this.listViewCableTest.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
                this.listViewCableTest.Columns[11].Width = 0;
                this.listViewCableTest.Columns[12].Width = 0;
                this.listViewCableTest.Columns[13].Width = 0;

                this.listViewCableTest.Font = new Font("宋体", 14f, FontStyle.Regular);

                this.listViewCableTest.VirtualMode = false;
            }
        }

        private void SetListViewSize()
        {
            if (this.listViewSelfStudy == null)
                return;
            if (this.listViewSelfStudy.Columns.Count <= 0)
                return;
            this.listViewSelfStudy.Columns[0].Width = this.listViewSelfStudy.Width / 5;
            this.listViewSelfStudy.Columns[1].Width = this.listViewSelfStudy.Width / 5;
            this.listViewSelfStudy.Columns[2].Width = this.listViewSelfStudy.Width / 5;
            this.listViewSelfStudy.Columns[3].Width = this.listViewSelfStudy.Width / 5;
            this.listViewSelfStudy.Columns[4].Width = this.listViewSelfStudy.Width / 5;
        }

        private void EventHandles()
        {
            #region tool event
            this.tool_InterfaceLibrary.Click += Tool_InterfaceLibrary_Click;
            this.tool_HarnessLibrary.Click += Tool_HarnessLibrary_Click;
            this.tool_NewProject.Click += Tool_NewProject_Click;
            this.tool_OpenProject.Click += Tool_OpenProject_Click;
            this.tool_HistoryData.Click += Tool_HistoryData_Click;
            this.tool_ConnectDevice.Click += Tool_ConnectDevice_Click;
            this.tool_SelfStudy.Click += Tool_SelfStudy_Click;
            this.tool_Probe.Click += Tool_Probe_Click;
            this.tool_testEnvironment.Click += Tool_testEnvironment_Click;
            #endregion

            #region menu event
            this.menu_connectServer.Click += Menu_connectServer_Click;
            this.menu_disConnect.Click += Menu_disConnect_Click;
            this.menu_SaveData.Click += Menu_SaveData_Click;
            this.menu_connectorLibrary.Click += Menu_connectorLibrary_Click;
            this.menu_interfaceLibrary.Click += Menu_interfaceLibrary_Click;
            this.menu_cableLibrary.Click += Menu_cableLibrary_Click;
            this.menu_switchStandMapLib.Click += Menu_switchStandMapLib_Click;
            this.menu_switchWorkWearLib.Click += Menu_switchWorkWearLib_Click;
            this.menu_modifyPassword.Click += Menu_modifyPassword_Click;
            this.menu_operateLog.Click += Menu_operateLog_Click;
            this.menu_userManager.Click += Menu_userManager_Click;
            this.menu_connectCfg.Click += Menu_connectCfg_Click;
            this.menu_ConductionTest.Click += Menu_ConductionTest_Click;
            this.menu_shortCircuitTest.Click += Menu_shortCircuitTest_Click;
            this.menu_InsulationTest.Click += Menu_InsulationTest_Click;
            this.menu_VoltageWithStandTest.Click += Menu_VoltageWithStandTest_Click;
            this.menu_OneKeyTest.Click += Menu_OneKeyTest_Click;
            this.menu_devCalibration.Click += Menu_devCalibration_Click;
            this.menu_devDebugAssitant.Click += Menu_devDebugAssitant_Click;
            this.menu_devSelfCheck.Click += Menu_devSelfCheck_Click;
            this.menu_faultCode.Click += Menu_faultCode_Click;
            this.menu_historyData.Click += Menu_historyData_Click;
            #endregion

            this.FormClosed += CMainForm_FormClosed;
            this.radTreeView1.NodeMouseDoubleClick += RadTreeView1_NodeMouseDoubleClick;
            this.sysTimer.Elapsed += SysTimer_Elapsed;
            this.SizeChanged += CMainForm_SizeChanged;
            this.refreshDataTimer.Elapsed += RefreshDataTimer_Elapsed;

            this.listViewSelfStudy.RetrieveVirtualItem += ListViewSelfStudy_RetrieveVirtualItem;
            this.listViewCableTest.RetrieveVirtualItem += ListViewCableTest_RetrieveVirtualItem;

            SuperEasyClient.NoticeConnectEvent += SuperEasyClient_NoticeConnectEvent;
            SuperEasyClient.NoticeMessageEvent += SuperEasyClient_NoticeMessageEvent;
        }

        private void RefreshDataTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            UpdateTestSelfStudyGridView();
            UpdateTestResultByTestPoint();
        }

        private void ListViewCableTest_RetrieveVirtualItem(object sender, RetrieveVirtualItemEventArgs e)
        {
            if (this.cableTestListView == null || this.cableTestListView.Count == 0)
            {
                return;
            }
            ListViewItem lv = this.cableTestListView[e.ItemIndex];
            e.Item = lv;
        }

        private void ListViewSelfStudy_RetrieveVirtualItem(object sender, RetrieveVirtualItemEventArgs e)
        {
            if (this.selfStudyListView == null || this.selfStudyListView.Count == 0)
            {
                return;
            }
            ListViewItem lv = this.selfStudyListView[e.ItemIndex];
            e.Item = lv;
        }

       

        private void CMainForm_SizeChanged(object sender, EventArgs e)
        {
            SetListViewSize();
            this.listViewCableTest.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
            this.listViewCableTest.Columns[11].Width = 0;
            this.listViewCableTest.Columns[12].Width = 0;
            this.listViewCableTest.Columns[13].Width = 0;
        }

        private void Tool_testEnvironment_Click(object sender, EventArgs e)
        {
            OpenEnvironmentParamsSet();
        }

        private bool OpenEnvironmentParamsSet()
        {
            FrmEnvironmentParams environmentParaSet = new FrmEnvironmentParams();
            if (environmentParaSet.ShowDialog() == DialogResult.OK)
            {
                this.environmentTemperature = environmentParaSet.temperature;
                this.environmentAmbientHumidity = environmentParaSet.ambientHumidity;
                return true;
            }
            return false;
        }

        #region system event
        private void SysTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            this.lbx_monitorDate.Text = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        private bool IsConnectSuccess()
        {
            if (!this.IsConnectServer)
            {
                MessageBox.Show("设备未连接！", "提示", MessageBoxButtons.OK);
                return false;
            }
            return true;
        }

        private void CMainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            SaveDeviceConfig();
        }

        private void SuperEasyClient_NoticeMessageEvent(ClientSocket.AppBase.MyPackageInfo packageInfo)
        {
            //LogHelper.Log.Info($"接收到消息：头= {BitConverter.ToString(packageInfo.Header)} 内容="+BitConverter.ToString(packageInfo.Data));
            //测试结果根据阈值判断是否合格
            //导通测试：小于阈值为合格
            //短路测试：大于阈值为合格
            //绝缘测试：大于阈值为合格
            //耐压测试：小于阈值为合格
              
            //接收消息
            var dataLength = packageInfo.Data.Length;
            //导通测试成功后，更新UI，并开始测试下一个
            //收到f2 cc
            if (packageInfo.Header.Length < 4 || packageInfo.Data.Length < 4)
                return;
            if (packageInfo.Data[0] != 0xff || packageInfo.Data[1] != 0xff)
            {
                return;
            }
            //返回数据格式为功能码 + 方法类型 + 接点内容 + 测试结果
            if (packageInfo.Data[4] == 0xf1 && packageInfo.Data[5] == 0xbb)//自学习
            {
                //FF-FF-00-0C-F1-BB-00-01-00-02-01-01-00-00-1A-C9
                //min/max/setthreshold
                var startInterPoint = ((int)packageInfo.Data[6]).ToString().PadLeft(2, '0') + ((int)packageInfo.Data[7]).ToString().PadLeft(2, '0');
                var endInterPoint = ((int)packageInfo.Data[8]).ToString().PadLeft(2, '0') + ((int)packageInfo.Data[9]).ToString().PadLeft(2, '0');
                var baseNum = ((int)packageInfo.Data[10]).ToString().PadLeft(2, '0');
                var power = (int)packageInfo.Data[11];
                byte[] buffer = new byte[4];
                Array.Copy(packageInfo.Data, 12, buffer, 0, 4);
                var resistance = Convert.ToInt32(BitConverter.ToString(buffer).Replace("-",""),16) / 65536.0;
                double factor = 0;
                if (baseNum == "00")
                {
                    double.TryParse(Convert.ToString((int)packageInfo.Data[11], 16).Substring(0, 1), out factor);
                }
                else if (baseNum == "01")
                {
                    factor = 10 * Math.Pow(10, power);
                }
                var calResult = resistance * factor;
                var selfStudyResult = "";
                if (calResult < this.selfStudyThreshold)
                    selfStudyResult = "导通";
                else
                    selfStudyResult = "不导通";
                SelfStudyParams studyParams = new SelfStudyParams();
                studyParams.StartPoint = startInterPoint;
                studyParams.EndPoint = endInterPoint;
                studyParams.TestResultVal = calResult.ToString("f2");
                studyParams.TestReulst = selfStudyResult;
                this.studyParamList.Add(studyParams);
                this.refreshDataTimer.Enabled = true;
            }
            else if (packageInfo.Data[4] == 0xf1 && packageInfo.Data[5] == 0xcc)//自学习结束
            {
                this.refreshDataTimer.Enabled = false;
            }
            else if (packageInfo.Data[4] == 0xf2 && packageInfo.Data[5] == 0xbb)//导通测试
            {//02-00-01-00-05-00-00-00-02
                var startInterPoint = ((int)packageInfo.Data[6]).ToString().PadLeft(2,'0') + ((int)packageInfo.Data[7]).ToString().PadLeft(2, '0');
                var endInterPoint = ((int)packageInfo.Data[8]).ToString().PadLeft(2, '0') + ((int)packageInfo.Data[9]).ToString().PadLeft(2, '0');
                var baseNum = ((int)packageInfo.Data[10]).ToString().PadLeft(2, '0');
                var power = (int)packageInfo.Data[11];
                byte[] buffer = new byte[4];
                Array.Copy(packageInfo.Data, 12, buffer, 0, 4);
                var resistance = Convert.ToInt32(BitConverter.ToString(buffer).Replace("-", ""), 16) / 65536.0;
                var factor = 10 * Math.Pow(10, power);
                var calResult = resistance * factor;
                var conductResult = "";
                if (calResult <= this.conductPassThreshold)
                    conductResult = "导通";
                else
                    conductResult = "不导通";
                CableTestParams cableTestParams = new CableTestParams();
                cableTestParams.StartPoint = startInterPoint;
                cableTestParams.EndPoint = endInterPoint;
                cableTestParams.TestResultVal = calResult.ToString("f2");
                cableTestParams.TestReulst = conductResult;
                cableTestParams.StartIndex = 5;
                this.cableTestPramsList.Add(cableTestParams);
                this.refreshDataTimer.Enabled = true;
            }
            else if (packageInfo.Data[4] == 0xf2 && packageInfo.Data[5] == 0xcc)//导通测试结束
            {
                //this.refreshDataTimer.Enabled = false;
                this.IsPassConductionTest = true;
                this.IsConductLatestCompleteStatus = true;//更新导通测试最新测试状态
                //保存测试状态与结果数据
                //更新UI最终测试结果
                //检查是否进行下一项测试
                if (this.IsStartOneClickTest)
                {
                    //启动选择的测试项启动测试
                    if (this.IsStartShortCircuitTest)
                        StartCircuitTest();
                    else
                    {
                        if (this.IsStartInsulateTest)
                            StartInsulateTest();
                        else
                        {
                            if (this.IsStartVoltageWithStandardTest)
                                StartVoltageWithStandardTest();
                            else
                            {
                                //一键测试结束，更新控件显示
                                RefreshTestItemControl();
                            }
                        }
                    }
                }
                else
                {
                    this.Invoke(new Action(()=>
                    {
                        this.menu_ConductionTest.Enabled = true;
                        this.lbx_testStatus.Text = "导通测试已完成";
                        this.lbx_testStatus.ForeColor = Color.Red;
                        this.endTestDate = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    }));
                }
            }
            else if (packageInfo.Data[4] == 0xf3 && packageInfo.Data[5] == 0xbb)//短路测试测试
            {
                var startInterPoint = ((int)packageInfo.Data[6]).ToString().PadLeft(2, '0') + ((int)packageInfo.Data[7]).ToString().PadLeft(2, '0');
                var endInterPoint = ((int)packageInfo.Data[8]).ToString().PadLeft(2, '0') + ((int)packageInfo.Data[9]).ToString().PadLeft(2, '0');
                var baseNum = ((int)packageInfo.Data[10]).ToString().PadLeft(2, '0');
                var power = (int)packageInfo.Data[11];
                byte[] buffer = new byte[4];
                Array.Copy(packageInfo.Data, 12, buffer, 0, 4);
                var resistance = Convert.ToInt32(BitConverter.ToString(buffer).Replace("-", ""), 16) / 65536.0;
                var factor = 10 * Math.Pow(10, power);
                var calResult = resistance * factor;
                var circuitResult = "";
                if (calResult <= this.shortCircuitPassThreshold)
                    circuitResult = "导通";
                else
                    circuitResult = "不导通";
                CableTestParams cableTestParams = new CableTestParams();
                cableTestParams.StartPoint = startInterPoint;
                cableTestParams.EndPoint = endInterPoint;
                cableTestParams.TestResultVal = calResult.ToString("f2");
                cableTestParams.TestReulst = circuitResult;
                cableTestParams.StartIndex = 7;
                this.cableTestPramsList.Add(cableTestParams);
                //this.refreshDataTimer.Enabled = true;
            }
            else if (packageInfo.Data[4] == 0xf3 && packageInfo.Data[5] == 0xcc)//短路测试结束
            {
                this.IsPassShortCircuitTest = true;
                this.IsShortCircuitLatestCompleteStatus = true;
                if (this.IsStartOneClickTest)
                {
                    if (this.IsStartInsulateTest)
                        StartInsulateTest();
                    else if (this.IsStartVoltageWithStandardTest)
                        StartVoltageWithStandardTest();
                    else
                    {
                        //一键测试结束，更新控件显示
                        RefreshTestItemControl();
                    }
                }
                else
                {
                    this.Invoke(new Action(() =>
                    {
                        this.menu_shortCircuitTest.Enabled = true;
                        this.lbx_testStatus.Text = "短路测试已完成";
                        this.lbx_testStatus.ForeColor = Color.Red;
                        this.endTestDate = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    }));
                }
            }
            else if (packageInfo.Data[4] == 0xf4 && packageInfo.Data[5] == 0xbb)//绝缘测试
            {
                var startInterPoint = ((int)packageInfo.Data[6]).ToString().PadLeft(2, '0') + ((int)packageInfo.Data[7]).ToString().PadLeft(2, '0');
                var endInterPoint = ((int)packageInfo.Data[8]).ToString().PadLeft(2, '0') + ((int)packageInfo.Data[9]).ToString().PadLeft(2, '0');
                var baseNum = ((int)packageInfo.Data[10]).ToString().PadLeft(2, '0');
                var power = (int)packageInfo.Data[11];
                byte[] buffer = new byte[4];
                Array.Copy(packageInfo.Data, 12, buffer, 0, 4);
                var resistance = Convert.ToInt32(BitConverter.ToString(buffer).Replace("-", ""), 16) / 65536.0;
                var factor = 10 * Math.Pow(10, power);
                var calResult = resistance * factor;
                var insulateResult = "";
                if (calResult <= this.insulatePassThreshold)
                    insulateResult = "导通";
                else
                    insulateResult = "不导通";
                CableTestParams cableTestParams = new CableTestParams();
                cableTestParams.StartPoint = startInterPoint;
                cableTestParams.EndPoint = endInterPoint;
                cableTestParams.TestResultVal = calResult.ToString("f2");
                cableTestParams.TestReulst = insulateResult;
                cableTestParams.StartIndex = 9;
                this.cableTestPramsList.Add(cableTestParams);
                //this.refreshDataTimer.Enabled = true;
            }
            else if (packageInfo.Data[4] == 0xf4 && packageInfo.Data[5] == 0xcc)//绝缘测试结束
            {
                this.IsPassInsulateTest = true;
                this.IsInsulateLatestCompleteStatus = true;
                if (this.IsStartOneClickTest)
                {
                    if (this.IsStartVoltageWithStandardTest)
                        StartVoltageWithStandardTest();
                    else
                    {
                        //一键测试结束，更新控件显示
                        RefreshTestItemControl();
                    }
                }
                else
                {
                    this.Invoke(new Action(()=>
                    {
                        this.lbx_testStatus.Text = "绝缘测试已完成";
                        this.lbx_testStatus.ForeColor = Color.Red;
                        this.menu_InsulationTest.Enabled = true;
                        this.endTestDate = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    }));
                }
            }
            else if (packageInfo.Data[4] == 0xf5 && packageInfo.Data[5] == 0xbb)//耐压测试测试
            {
                var startInterPoint = ((int)packageInfo.Data[6]).ToString().PadLeft(2, '0') + ((int)packageInfo.Data[7]).ToString().PadLeft(2, '0');
                var endInterPoint = ((int)packageInfo.Data[8]).ToString().PadLeft(2, '0') + ((int)packageInfo.Data[9]).ToString().PadLeft(2, '0');
                var baseNum = ((int)packageInfo.Data[10]).ToString().PadLeft(2, '0');
                var power = (int)packageInfo.Data[11];
                byte[] buffer = new byte[4];
                Array.Copy(packageInfo.Data, 12, buffer, 0, 4);
                var resistance = Convert.ToInt32(BitConverter.ToString(buffer).Replace("-", ""), 16) / 65536.0;
                var factor = 10 * Math.Pow(10, power);
                var calResult = resistance * factor;
                var voltageResult = "";
                if (calResult <= this.insulatePassThreshold)
                    voltageResult = "导通";
                else
                    voltageResult = "不导通";
                CableTestParams cableTestParams = new CableTestParams();
                cableTestParams.StartPoint = startInterPoint;
                cableTestParams.EndPoint = endInterPoint;
                cableTestParams.TestResultVal = calResult.ToString("f2");
                cableTestParams.TestReulst = voltageResult;
                cableTestParams.StartIndex = 11;
                this.cableTestPramsList.Add(cableTestParams);
                //this.refreshDataTimer.Enabled = true;
            }
            else if (packageInfo.Data[4] == 0xf5 && packageInfo.Data[5] == 0xcc)//耐压测试测试结束
            {
                this.IsPassVoltageWithStandardTest = true;
                this.IsVoltageWithStandardLatestCompleteStatus = true;
                if (this.IsStartOneClickTest)
                {
                    //一键测试结束，更新控件显示
                    RefreshTestItemControl();
                }
                else
                {
                    this.Invoke(new Action(()=>
                    {
                        this.lbx_testStatus.Text = "耐压测试已完成";
                        this.lbx_testStatus.ForeColor = Color.Red;
                        this.menu_VoltageWithStandTest.Enabled = true;
                        this.endTestDate = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    }));
                }
            }
        }

        private void RefreshTestItemControl()
        {
            this.menu_ConductionTest.Enabled = this.IsConductLatestCompleteStatus;
            this.menu_shortCircuitTest.Enabled = this.IsShortCircuitLatestCompleteStatus;
            this.menu_InsulationTest.Enabled = this.IsInsulateLatestCompleteStatus;
            this.menu_VoltageWithStandTest.Enabled = this.IsVoltageWithStandardLatestCompleteStatus;
        }

        private void UpdateTestSelfStudyGridView()
        {
            lock (this)
            {
                if (this.studyParamList.Count <= 0)
                    return;
                this.listViewCableTest.VirtualMode = true;
                this.Invoke(new Action(() =>
                {

                    int count = this.studyParamList.Count;
                    int i = 0;
                    this.listViewSelfStudy.BeginUpdate();
                    foreach (var selfParams in this.studyParamList)
                    {
                        var startPoint = selfParams.StartPoint;
                        var endPoint = selfParams.EndPoint;
                        var testResultVal = selfParams.TestResultVal;
                        var testResult = selfParams.TestReulst;
                        if (IsGridView2Exist(startPoint, endPoint))
                            ;//continue;
                        var currentRowIndex = this.listViewSelfStudy.Items.Count + i;
                        ListViewItem listViewItem = new ListViewItem();
                        listViewItem.Text = (currentRowIndex + 1).ToString();
                        listViewItem.SubItems.AddRange(new string[] { startPoint, endPoint, testResultVal, testResult });

                        listViewItem.UseItemStyleForSubItems = false;
                        if (testResult == "导通")
                        {
                            listViewItem.SubItems[4].BackColor = Color.Blue;
                        }
                        else if (testResult == "不导通")
                        {
                            listViewItem.SubItems[4].BackColor = Color.Red;
                        }
                        this.selfStudyListView.Add(listViewItem);
                        i++;
                    }

                    this.studyParamList.RemoveRange(0, count);
                    if (i > 0)
                    {
                        this.listViewSelfStudy.VirtualListSize = this.listViewSelfStudy.Items.Count + i;
                        this.listViewSelfStudy.Items[this.listViewSelfStudy.Items.Count - 1].EnsureVisible();
                    }
                    this.listViewSelfStudy.EndUpdate();

                    this.refreshDataTimer.Enabled = false;
                }));
            }
        }

        private void UpdateTestResultByTestPoint()
        {
            if (this.cableTestPramsList.Count <= 0)
                return;
            this.Invoke(new Action(()=> 
            {
                int count = this.cableTestPramsList.Count;
                int iRow = 0;
                this.listViewCableTest.BeginUpdate();
                foreach (var cableTestParams in this.cableTestPramsList)
                {
                    var startPoint = cableTestParams.StartPoint;
                    var endPoint = cableTestParams.EndPoint;
                    var testResultVal = cableTestParams.TestResultVal;
                    var testResult = cableTestParams.TestReulst;

                    for (int i = 0; i < this.listViewCableTest.Items.Count; i++)
                    {
                        var lv = this.listViewSelfStudy.Items[i];
                        var sp = lv.SubItems[2].Text.PadLeft(4, '0');
                        var ep = lv.SubItems[4].Text.PadLeft(4, '0');
                        if (sp == startPoint && ep == endPoint)
                        {
                            //更新测试记录
                            lv.SubItems[cableTestParams.StartIndex].Text = testResultVal;
                            lv.SubItems[cableTestParams.StartIndex + 1].Text = testResult;

                            lv.UseItemStyleForSubItems = false;
                            if (testResult == "导通")
                            {
                                lv.SubItems[4].BackColor = Color.Blue;
                            }
                            else if (testResult == "不导通")
                            {
                                lv.SubItems[4].BackColor = Color.Red;
                            }
                            iRow++;
                        }
                    }
                }
                this.listViewCableTest.VirtualListSize = this.listViewCableTest.Items.Count + iRow;
                this.listViewCableTest.Items[this.listViewCableTest.Items.Count - 1].EnsureVisible();
                this.cableTestPramsList.RemoveRange(0, count);
                this.listViewCableTest.EndUpdate();
            }));
        }

        private bool IsGridView2Exist(string startPoint, string endPoint)
        {
            if (this.listViewSelfStudy.Items.Count < 1)
            {
                return false;
            }
            for (int i = 0;i<this.listViewSelfStudy.Items.Count;i++)
            {
                var lv = this.listViewSelfStudy.Items[i];
                var sp = lv.SubItems[1].Text;
                var ep = lv.SubItems[2].Text;
                if (sp == startPoint && ep == endPoint)
                    return true;
            }
            return false;
        }

        private bool IsGridViewCableTestExist(string startPoint, string endPoint)
        {
            if (this.listViewSelfStudy.Items.Count < 1)
            {
                return false;
            }
            for (int i = 0; i < this.listViewSelfStudy.Items.Count; i++)
            {
                var lv = this.listViewSelfStudy.Items[i];
                if (lv.SubItems[2].Text.PadLeft(4, '0') == startPoint && lv.SubItems[4].Text.PadLeft(4, '0') == endPoint)
                {
                    return true;
                }
            }
            return false;
        }

        private void SuperEasyClient_NoticeConnectEvent(bool IsConnect, string tipMessage)
        {
            this.IsConnectServer = IsConnect;
            if (IsConnect)
            {
                //连接成功
                if (this.InvokeRequired)
                    this.Invoke(new Action(() =>
                    {
                        this.tool_ConnectDevice.Text = "断开连接";
                        this.tool_ConnectDevice.Image = Resources.server_delete;
                        this.lbx_conDevStatus.Text = "已连接";
                        this.lbx_serviceURL.Text = this.serviceURL;
                        this.lbx_servicePort.Text = this.servicePort.ToString();
                    }));
            }
            else
            {
                //断开连接/连接失败
                this.Invoke(new Action(() =>
                {
                    this.tool_ConnectDevice.Text = "连接设备";
                    this.tool_ConnectDevice.Image = Resources.server_link;
                    if (tipMessage != "")
                        MessageBox.Show(tipMessage, "提示", MessageBoxButtons.OK);
                    this.lbx_conDevStatus.Text = "未连接";
                    this.lbx_serviceURL.Text = "";
                    this.lbx_servicePort.Text = "";
                }));
            }
        }

        private void ConnectService()
        {
            if (!this.IsConnectServer)
            {
                //断开连接状态，开始连接服务
                if (this.serviceURL == "" || this.servicePort == 0)
                {
                    if (MessageBox.Show("服务器参数未配置，是否立即打开配置！", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1) == DialogResult.Yes)
                    {
                        OpenConnectConfig();
                        if (this.serviceURL == "")
                            return;
                        if (this.servicePort == 0)
                            return;
                    }
                    else
                        return;
                }
                else
                {
                    //update status
                    this.lbx_serviceURL.Text = this.serviceURL;
                    this.lbx_servicePort.Text = this.servicePort.ToString();
                }
                SuperEasyClient.serverUrl = this.serviceURL;
                SuperEasyClient.serverPort = this.servicePort.ToString();
                SuperEasyClient.ConnectServer();
            }
            else
            {
                //已连接服务，开始断开连接
                SuperEasyClient.client.Close();
            }
        }

        private void RadTreeView1_NodeMouseDoubleClick(object sender, RadTreeViewEventArgs e)
        {
            RadTreeNode radTreeNode = e.Node;
            var projectInfoData = projectInfoManager.GetDataSetByFieldsAndWhere("distinct ProjectName", "").Tables[0];
            if (projectInfoData.Rows.Count < 1)
                return;
            foreach (DataRow dr in projectInfoData.Rows)
            {
                var projectName = dr["ProjectName"].ToString();
                if (radTreeNode.RootNode.Text == projectName)
                {
                    switch (radTreeNode.Text)
                    {
                        case PROJECT_EDIT:
                            RadProjectCreat radProjectCreat = new RadProjectCreat("编辑项目", projectName, cableJudgeThreshold,true);
                            radProjectCreat.ShowDialog();
                            break;
                        case WORK_WEAR:

                            break;
                        case PROJECT_OPEN_TEST:
                            this.radDock1.AddDocument(this.documentWindow1);
                            this.currentTestProject = projectName;
                            QueryTestInfoByProjectName(projectName);
                            break;

                        case PROJECT_CLOSE_TEST:
                            this.radDock1.RemoveWindow(this.documentWindow1);
                            break;

                        case CABLE_DETAIL:

                            break;

                        case DEVICE_DETAIL:

                            break;
                    }
                }
            }
        }

        #endregion

        #region menu event
        private void Menu_historyData_Click(object sender, EventArgs e)
        {
            RadTestDataBasicInfo radTestDataBasicInfo = new RadTestDataBasicInfo();
            radTestDataBasicInfo.ShowDialog();
        }

        private void Menu_faultCode_Click(object sender, EventArgs e)
        {
            FaultCodeInfo faultCodeInfo = new FaultCodeInfo();
            faultCodeInfo.ShowDialog();  
        }

        private void Menu_disConnect_Click(object sender, EventArgs e)
        {
            ConnectService();
        }

        private void Menu_connectServer_Click(object sender, EventArgs e)
        {
            ConnectService();
        }

        private void Menu_cableLibrary_Click(object sender, EventArgs e)
        {
            OpenHarnessLibrary();
        }

        private void Menu_interfaceLibrary_Click(object sender, EventArgs e)
        {
            OpenInterfaceLibrary();
        }

        private void Menu_connectorLibrary_Click(object sender, EventArgs e)
        {
            RadConnectorLibrary radConnectorLibrary = new RadConnectorLibrary();
            radConnectorLibrary.ShowDialog();
        }

        /// <summary>
        /// 设备自检
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Menu_devSelfCheck_Click(object sender, EventArgs e)
        {
            DevSelfCheck devSelfCheck = new DevSelfCheck();
            devSelfCheck.ShowDialog();
        }

        /// <summary>
        /// 设备调试助手
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Menu_devDebugAssitant_Click(object sender, EventArgs e)
        {
            DevDebugAssitant deviceDebugging = new DevDebugAssitant();
            deviceDebugging.ShowDialog();
        }

        /// <summary>
        /// 设备校准
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Menu_devCalibration_Click(object sender, EventArgs e)
        {
            DevCalibration deviceCalibration = new DevCalibration();
            deviceCalibration.ShowDialog();
        }

        private void Menu_VoltageWithStandTest_Click(object sender, EventArgs e)
        {
            this.startTestDate = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            this.UpdateCurrentTestNumber();
            StartVoltageWithStandardTest();
        }

        private void Menu_InsulationTest_Click(object sender, EventArgs e)
        {
            this.startTestDate = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            this.UpdateCurrentTestNumber();
            StartInsulateTest();
        }

        private void Menu_shortCircuitTest_Click(object sender, EventArgs e)
        {
            this.startTestDate = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            this.UpdateCurrentTestNumber();
            StartCircuitTest();
        }

        private void Menu_ConductionTest_Click(object sender, EventArgs e)
        {
            this.startTestDate = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            this.UpdateCurrentTestNumber();
            StartConductTest();
        }

        private void Menu_OneKeyTest_Click(object sender, EventArgs e)
        {
            OneClickTestConfig oneClickTest = new OneClickTestConfig();
            if (oneClickTest.ShowDialog() == DialogResult.OK)
            {
                //清空异常数
                this.conductTestExceptCount = 0;
                this.shortCircuitTestExceptCount = 0;
                this.insulateTestExceptCount = 0;
                this.voltageWithStandardTestExceptCount = 0;

                this.menu_OneKeyTest.Enabled = false;
                this.IsStartOneClickTest = true;//启动一键测试
                this.IsStartConductTest = oneClickTest.IsCheckConductTest;
                this.IsStartShortCircuitTest = oneClickTest.IsCheckCircuitTest;
                this.IsStartInsulateTest = oneClickTest.IsCheckInsulateTest;
                this.IsStartVoltageWithStandardTest = oneClickTest.IsCheckPressureProofTest;

                this.startTestDate = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                this.UpdateCurrentTestNumber();
                StartOneClickTest();
            }
        }

        private void Menu_connectCfg_Click(object sender, EventArgs e)
        {
            OpenConnectConfig();
        }

        private void OpenConnectConfig()
        {
            AddConnection addConnection = new AddConnection(this.serviceURL, this.servicePort);
            if (addConnection.ShowDialog() == DialogResult.OK)
            {
                this.serviceURL = addConnection.serverIP;
                this.servicePort = addConnection.serverPort;
            }
        }

        private void Menu_userManager_Click(object sender, EventArgs e)
        {
            UserManager userManager = new UserManager();
            userManager.ShowDialog();
        }

        private void Menu_operateLog_Click(object sender, EventArgs e)
        {
            OperatorLog operatorLog = new OperatorLog();
            operatorLog.ShowDialog();
        }

        private void Menu_modifyPassword_Click(object sender, EventArgs e)
        {
            TUserManager userManager = new TUserManager();
            var dt = userManager.GetDataSetByFieldsAndWhere("UserID", $"where UserName='{LocalLogin.CurrentUserName}'").Tables[0];
            if (dt.Rows.Count < 1)
                return;
            ModifyPwd modifyPwd = new ModifyPwd(dt.Rows[0][0].ToString(),LocalLogin.CurrentUserName);
            modifyPwd.ShowDialog();
        }

        private void Menu_switchWorkWearLib_Click(object sender, EventArgs e)
        {
            SwitchWorkWearLibrary switchWorkWearLibrary = new SwitchWorkWearLibrary();
            switchWorkWearLibrary.ShowDialog();
        }

        private void Menu_switchStandMapLib_Click(object sender, EventArgs e)
        {
            TesterSwitchMapRelation testerSwitchMapRelation = new TesterSwitchMapRelation();
            testerSwitchMapRelation.ShowDialog();
        }

        private void Menu_SaveData_Click(object sender, EventArgs e)
        {
            SaveTestResult();
        }
        #endregion

        #region tool event

        private void Tool_Probe_Click(object sender, EventArgs e)
        {
            RadProbMeasure probeMeasure = new RadProbMeasure();
            probeMeasure.ShowDialog();
        }

        private void Tool_SelfStudy_Click(object sender, EventArgs e)
        {
            if (!this.IsConnectServer)
            {
                if (MessageBox.Show("设备未连接，是否进入参数设置页面？", "提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK)
                    return;
            }
            //设备连接成功后，才能进入自学习
            frmSefStudy sefStudy = new frmSefStudy();
            if (sefStudy.ShowDialog() == DialogResult.OK)
            {
                var selfStudyType = sefStudy.studyTypeEnum;
                var studyMinByPin = sefStudy.pinMin;
                var studyMaxByPin = sefStudy.pinMax;
                var conductThresholdByPin = sefStudy.conductThresholdByPin;
                var selftStudyString = Convert.ToString(studyMinByPin, 16).PadLeft(4, '0') +
                    Convert.ToString(studyMaxByPin, 16).PadLeft(4, '0') + FloatConvert.FloatConvertByte(conductThresholdByPin);
                SendSelfStudyCommand(selftStudyString);
                this.selfStudyThreshold = sefStudy.conductThresholdByPin;
                this.radDock1.AddDocument(this.documentWindow2);
                InitDatable(true);
            }
            UpdateTestSelfStudyGridView();
        }

        private void Tool_ConnectDevice_Click(object sender, EventArgs e)
        {
            //连接通讯设备
            ConnectService();
        }

        private void Tool_HistoryData_Click(object sender, EventArgs e)
        {
            RadTestDataBasicInfo testDataBasicInfo = new RadTestDataBasicInfo();
            testDataBasicInfo.ShowDialog();
        }

        private void Tool_OpenProject_Click(object sender, EventArgs e)
        {
            ProjectManage projectManager = new ProjectManage(cableJudgeThreshold);
            if (projectManager.ShowDialog() == DialogResult.OK)
            {
                if (projectManager.operateType == ProjectManage.OperateType.OpenProject)
                {
                    this.radDock1.AddDocument(this.documentWindow1);
                    QueryTestInfoByProjectName(projectManager.openProjectName);
                }
                else if (projectManager.operateType == ProjectManage.OperateType.DeleteProject)
                {
                    //update current project info
                    UpdateTreeView();
                    QueryTestInfoByProjectName(projectManager.openProjectName);
                }
            }
        }

        private void Tool_NewProject_Click(object sender, EventArgs e)
        {
            RadProjectCreat radProjectCreat = new RadProjectCreat("新建项目","",cableJudgeThreshold,false);
            if (radProjectCreat.ShowDialog() == DialogResult.OK)
            {
                UpdateTreeView();
                this.radDock1.AddDocument(this.documentWindow1);
                QueryTestInfoByProjectName(radProjectCreat.projectName);
            }
        }

        /// <summary>
        /// 线束库管理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Tool_HarnessLibrary_Click(object sender, EventArgs e)
        {
            OpenHarnessLibrary();
        }

        /// <summary>
        /// 接口库管理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Tool_InterfaceLibrary_Click(object sender, EventArgs e)
        {
            OpenInterfaceLibrary();
        }
        #endregion

        #region common method
        private void OpenHarnessLibrary()
        {
            RadCableLibrary radCableLibrary = new RadCableLibrary();
            if (radCableLibrary.ShowDialog() == DialogResult.OK)
            {
                UpdateTreeView();
            }
        }

        private void OpenInterfaceLibrary()
        {
            RadInterfaceLibrary interfaceLibrary = new RadInterfaceLibrary();
            interfaceLibrary.ShowDialog();
        }

        private void DeviceConnect()
        {
            try
            {
                KLineTestProcessor this2 = this.gLineTestProcessor;
                if (this2.bEmulationMode)
                {
                    //this.toolStripStatusLabel_csyConnStatus.Text = "连接正常";
                    MessageBox.Show("设备连接成功!", "提示", MessageBoxButtons.OK);
                }
                else if (!this2.bIsConnSuccFlag && !this2.mpDevComm.InitCAN())
                {
                    //this.bIsConnSuccFlag = false;
                    this.gLineTestProcessor.bIsConnSuccFlag = false;
                    MessageBox.Show("设备连接失败，请查看接线是否正常!", "提示", MessageBoxButtons.OK);
                }
                else
                {
                    System.Threading.Thread.Sleep(100);
                    //this.SendDeviceExistCmdFeedbackFunc();
                    System.Threading.Thread.Sleep(500);
                    KLineTestProcessor sender2 = this.gLineTestProcessor;
                    if (!sender2.mpDevComm.mParseCmd.DeviceExist)
                    {
                        try
                        {
                            this.gLineTestProcessor.mpDevComm.CloseCAN();
                        }
                        catch (System.Exception ex_B6)
                        {
                        }
                        //this.bIsConnSuccFlag = false;
                        this.gLineTestProcessor.bIsConnSuccFlag = false;
                        //this.toolStripStatusLabel_csyConnStatus.Text = "未连接";
                        MessageBox.Show("设备连接失败，请查看接线是否正常!", "提示", MessageBoxButtons.OK);
                    }
                    else
                    {
                        //this.bIsConnSuccFlag = true;
                        sender2.bIsConnSuccFlag = true;
                        //this.toolStripStatusLabel_csyConnStatus.Text = "连接正常";
                        MessageBox.Show("设备连接成功!", "提示", MessageBoxButtons.OK);
                    }
                }
            }
            catch (System.Exception arg_122_0)
            {
                KLineTestProcessor.ExceptionRecordFunc(arg_122_0.StackTrace);
            }
        }

        private void UpdateTreeView()
        {
            //select project
            var projectInfoData = projectInfoManager.GetDataSetByFieldsAndWhere("distinct ProjectName", "").Tables[0];
            if (projectInfoData.Rows.Count < 1)
                return;
            this.radTreeView1.Nodes.Clear();
            foreach (DataRow dr in projectInfoData.Rows)
            {
                var projectName = dr["ProjectName"].ToString();
                RadTreeNode root = this.radTreeView1.Nodes.Add(projectName);
                var projectNode = root.Nodes.Add(PROJECT_INFO, 1);
                projectNode.Nodes.Add(PROJECT_OPEN_TEST);
                projectNode.Nodes.Add(PROJECT_CLOSE_TEST);
                projectNode.Nodes.Add(PROJECT_EDIT, 1);
                projectNode.Nodes.Add(WORK_WEAR, 1);
                var cableNode = root.Nodes.Add(TEST_CABLE, 1);
                cableNode.Nodes.Add(CABLE_DETAIL, 1);
                var deviceNode = root.Nodes.Add(DEVICE_INFO, 1);
                deviceNode.Nodes.Add(DEVICE_DETAIL, 1);
            }
        }

        /// <summary>
        /// 保存测试记录：测试中/测试完成
        /// 多次保存的保存序列相同，每启动一次测试生成一个新测试序列
        /// </summary>
        private void SaveTestResult()
        {
            System.Threading.Tasks.Task task = new System.Threading.Tasks.Task(() =>
            {
                if (this.listViewCableTest.Items.Count < 1)
                    return;
                if (!IsExistTestRecore())//没有测试记录
                return;
            if (IsExistSaveTestSerialNumber())//已保存该测试序列
                return;
            var projectInfoData = this.projectInfoManager.GetDataSetByWhere($"where ProjectName = '{this.currentTestProject}'").Tables[0];
            var updateBasicCount = 0;
            var updateDetailCount = 0;

            #region project test basic info
            THistoryDataBasic historyDataInfo = new THistoryDataBasic();
            historyDataInfo.ID = TablePrimaryKey.InsertHistoryBasicPID();
            historyDataInfo.TestSerialNumber = this.projectTestNumber;
            historyDataInfo.ProjectName = this.currentTestProject;
            historyDataInfo.BatchNumber = projectInfoData.Rows[0]["BatchNumber"].ToString();
            historyDataInfo.TestCableName = projectInfoData.Rows[0]["TestCableName"].ToString();
            historyDataInfo.TestDate = this.startTestDate;
            //可新增结束日期
            historyDataInfo.TestOperator = LocalLogin.CurrentUserName;
            historyDataInfo.EnvironmentTemperature = this.environmentTemperature.ToString();
            historyDataInfo.EnvironmentAmbientHumidity = this.environmentAmbientHumidity.ToString();
            historyDataInfo.DeviceTypeNo = this.deviceTypeNo;
            historyDataInfo.DeviceMeasureNumber = "";//测试仪计量编号，无
            historyDataInfo.FinalTestResult = CalFinalTestResult();
            historyDataInfo.ConductThreshold = this.conductPassThreshold;
            historyDataInfo.ConductVoltage = double.Parse(projectInfoData.Rows[0]["ConductTestVoltage"].ToString());
            historyDataInfo.ConductElect = double.Parse(projectInfoData.Rows[0]["ConductTestCurrentElect"].ToString());
            historyDataInfo.ShortCircuitThreshold = this.shortCircuitPassThreshold;
            historyDataInfo.InsulateHighOrLowElect = double.Parse(projectInfoData.Rows[0]["InsulateHigthOrLowElect"].ToString());
            historyDataInfo.InsulateThreshold = this.insulatePassThreshold;
            historyDataInfo.InsulateHoldTime = double.Parse(projectInfoData.Rows[0]["InsulateTestHoldTime"].ToString());
            historyDataInfo.InsulateVoltage = double.Parse(projectInfoData.Rows[0]["InsulateTestVoltage"].ToString());
            historyDataInfo.InsulateRaiseTime = double.Parse(projectInfoData.Rows[0]["InsulateTestRaiseTime"].ToString());
            historyDataInfo.VoltageWithStandardThreshold = this.voltageWithStandardPassThreshold;
            historyDataInfo.VoltageWithStandardHoldTime = double.Parse(projectInfoData.Rows[0]["VoltageWithStandardHoldTime"].ToString());
            historyDataInfo.VoltageWithStandardVoltage = double.Parse(projectInfoData.Rows[0]["VoltageWithStandardVoltage"].ToString());
            if (this.IsPassConductionTest)
                historyDataInfo.IsConductTestComplete = 1;
            else
                historyDataInfo.IsConductTestComplete = 0;
            if (this.IsPassInsulateTest)
                historyDataInfo.IsInsulateTestComplete = 1;
            else
                historyDataInfo.IsInsulateTestComplete = 0;
            if (this.IsPassShortCircuitTest)
                historyDataInfo.IsShortCircuteTestComplete = 1;
            else
                historyDataInfo.IsShortCircuteTestComplete = 0;
            if (this.IsPassVoltageWithStandardTest)
                historyDataInfo.IsVoltageWithStandardTestComplete = 1;
            else
                historyDataInfo.IsVoltageWithStandardTestComplete = 0;

            historyDataInfo.ConductTestExceptCount = this.conductTestExceptCount;
            historyDataInfo.ShortcircuitTestExceptCount = this.shortCircuitTestExceptCount;
            historyDataInfo.InsulateTestExceptCount = this.insulateTestExceptCount;
            historyDataInfo.VoltageWithStandardTestExceptCount = this.voltageWithStandardTestExceptCount;
            updateBasicCount += historyDataInfoManager.Insert(historyDataInfo);
            #endregion

            #region project test detail info
            for (int i = 0;i < this.listViewCableTest.Items.Count;i++)
            {
                    var lv = this.listViewCableTest.Items[i];
                    THistoryDataDetail historyDataDetail = new THistoryDataDetail();
                historyDataDetail.ID = TablePrimaryKey.InsertHistoryDetailPID();
                historyDataDetail.TestSerialNumber = this.projectTestNumber.ToString();
                historyDataDetail.ProjectName = this.currentTestProject;
                if (lv.SubItems[1].Text != null)
                    historyDataDetail.StartInterface = lv.SubItems[1].Text.ToString();
                if (lv.SubItems[2].Text != null)
                    historyDataDetail.StartContactPoint = lv.SubItems[2].Text.ToString();
                if (lv.SubItems[3].Text != null)
                    historyDataDetail.EndInterface = lv.SubItems[3].Text.ToString();
                if (lv.SubItems[4].Text != null)
                    historyDataDetail.EndContactPoint = lv.SubItems[4].Text.ToString();
                if(lv.SubItems[5].Text != null)
                    historyDataDetail.ConductValue = lv.SubItems[5].Text.ToString();
                if(lv.SubItems[6].Text != null)
                    historyDataDetail.ConductTestResult = lv.SubItems[6].Text.ToString();
                if(lv.SubItems[7].Text != null)
                    historyDataDetail.InsulateValue = lv.SubItems[7].Text.ToString();
                if (lv.SubItems[8].Text != null)
                    historyDataDetail.InsulateTestResult = lv.SubItems[8].Text.ToString();
                if (lv.SubItems[9].Text != null)
                    historyDataDetail.VoltageWithStandardValue = lv.SubItems[9].Text.ToString();
                if (lv.SubItems[10].Text != null)
                    historyDataDetail.VoltageWithStandardTestResult = lv.SubItems[10].Text.ToString();

                updateDetailCount += historyDataDetailManager.Insert(historyDataDetail);
            }
            #endregion

            if (updateBasicCount > 0)
            {
                    MessageBox.Show("已保存成功!", "提示");
                    UpdateTestProjectStatus(historyDataInfo.TestCableName);
            }
            });
            task.Start();
        }

        private void UpdateTestProjectStatus(string testCableName)
        {
            try
            {
                var conductTestState = QueryTestItemStatusValue("ConductValue", testCableName);
                var shortCircuitTestState = QueryTestItemStatusValue("ShortCircuitValue", testCableName);
                var insulateTestState = QueryTestItemStatusValue("InsulateValue", testCableName);
                var voltageTestState = QueryTestItemStatusValue("VoltageWithStandardValue", testCableName);
                TProjectBasicInfo projectBasicInfo = new TProjectBasicInfo();

                var row = projectInfoManager.UpdateFields($"IsExistConductTest='{conductTestState}',IsExistShortCircuitTest='{shortCircuitTestState}'," +
                    $"IsExistInsulateTest='{insulateTestState}',IsExistVoltageWithStandardTest='{voltageTestState}'",
                    $"where TestCableName = '{testCableName}'");
                LogHelper.Log.Info($"更新测试项状态，更新记录={row}条");
            }
            catch (Exception ex)
            {
                LogHelper.Log.Error(ex.Message+ex.StackTrace);
            }
        }

        private int QueryTestItemStatusValue(string testItem,string testCableName)
        {
            var dt = historyDataInfoManager.GetDataSetByWhere($"where TestCableName = '{testCableName}'").Tables[0];
            if (dt.Rows.Count > 0)
            {
                foreach (DataRow dr in dt.Rows)
                {
                    var serialNum = dr["TestSerialNumber"].ToString();
                    dt = historyDataDetailManager.GetDataSetByWhere($"where TestSerialNumber='{serialNum}'").Tables[0];
                    if (dt.Rows.Count > 0)
                    {
                        foreach (DataRow dr1 in dt.Rows)
                        {
                            var conduct = dr1[testItem].ToString();
                            if (conduct != "")
                            {
                                return 1;
                            }
                        }
                    }
                }
            }
            return 0;
        }

        /// <summary>
        /// 查询当前视图是否有测试记录
        /// </summary>
        /// <returns></returns>
        private bool IsExistTestRecore()
        {
            if (this.listViewCableTest.Items.Count < 1)
                return false;
            for (int i = 0;i < this.listViewCableTest.Items.Count;i++)
            {
                var lv = this.listViewCableTest.Items[i];
                if (lv.SubItems[5].Text.ToString() != "" || lv.SubItems[7].Text.ToString() != "" || lv.SubItems[9].Text.ToString() != "" || lv.SubItems[11].Text.ToString() != "")
                    return true;
            }
            return false;
        }

        private bool IsExistSaveTestSerialNumber()
        {
            var count = historyDataInfoManager.GetRowCountByWhere($"where TestSerialNumber = '{this.projectTestNumber}'");
            if (count < 1)
                return false;
            return true;
        }

        private string CalFinalTestResult()
        {
            if (this.listViewCableTest.Items.Count < 1)
                return "未测试";
            for (int i = 0;i < this.listViewCableTest.Items.Count;i++)
            {
                var lv = this.listViewCableTest.Items[i];
                var conductResult = lv.SubItems[6].Text.ToString();
                var shortCircuitResult = lv.SubItems[6].Text.ToString();
                var insulateResult = lv.SubItems[6].Text.ToString();
                var voltageResult = lv.SubItems[6].Text.ToString();
                if (conductResult != "合格" || shortCircuitResult != "合格" || insulateResult != "合格" || voltageResult != "合格")
                {
                    return "不合格";
                }
            }
            return "合格";
        }

        private void QueryTestInfoByProjectName(string projectName)
        {
            this.listViewCableTest.VirtualMode = false;
            UpdateCurrentProTestFunStatusInfo(projectName);
            //RadGridViewProperties.ClearGridView(this.radGridView1);
            //RadGridViewProperties.SetRadGridViewProperty(this.radGridView1,false,true,13);
            //this.radGridView1.AutoSizeColumnsMode = GridViewAutoSizeColumnsMode.None;
            //查询项目信息-线束代号 by projectName
            var dt = projectInfoManager.GetDataSetByFieldsAndWhere("TestCableName", $"where ProjectName='{projectName}'").Tables[0];
            if (dt.Rows.Count > 0)
            {
                var cableName = dt.Rows[0][0].ToString();
                dt = lineStructLibraryDetailManager.GetDataSetByWhere($"where CableName='{cableName}'").Tables[0];
                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow dr in dt.Rows)
                    {
                        var startInterface = dr["StartInterface"].ToString();
                        var startContactPoint = dr["StartContactPoint"].ToString();
                        var endInterface = dr["EndInterface"].ToString();
                        var endContactPoint = dr["EndContactPoint"].ToString();
                        var IsTestConduct = dr["IsConductTest"].ToString();
                        var IsTestCircuit = dr["IsShortCircuitTest"].ToString();
                        var IsTestInsulate = dr["IsInsulateTest"].ToString();
                        var IsTestPreasureProof = dr["IsVoltageWithStandardTest"].ToString();
                        var measuringMethod = dr["MeasureMethod"].ToString();
                        var val_56 = "";
                        var val_78 = "";
                        var val_910 = "";
                        var val_1112 = "";
                        if (!IsExistGridView(startInterface,startContactPoint,endInterface,endContactPoint))
                        {
                            //this.radGridView1.Rows.AddNew();
                            var iRow = this.listViewCableTest.Items.Count;
                            ListViewItem lv = new ListViewItem();
                            lv.Text = (iRow + 1).ToString();
                            if (IsTestConduct == "0")
                            {
                                val_56 = "--";
                            }
                            if (IsTestCircuit == "0")
                            {
                                val_78 = "--";
                            }
                            if (IsTestInsulate == "0")
                            {
                                val_910 = "--";
                            }
                            if (IsTestPreasureProof == "0")
                            {
                                val_1112 = "--";
                            }
                            lv.SubItems.AddRange(new string[] { startInterface, startContactPoint, endInterface, endContactPoint,"",val_56,val_56,val_78,val_78,val_910,val_910,val_1112,val_1112, measuringMethod });
                            this.listViewCableTest.Items.Add(lv);
                        }
                    }
                }
            }
        }

        private void UpdateCurrentProTestFunStatusInfo(string projectName)
        {
            var dt = projectInfoManager.GetDataSetByWhere($"where ProjectName = '{projectName}'").Tables[0];
            if (dt.Rows.Count > 0)
            {
                double.TryParse(dt.Rows[0]["ConductTestThreshold"].ToString(), out this.conductPassThreshold);
                double.TryParse(dt.Rows[0]["ShortCircuitTestThreshold"].ToString(), out this.shortCircuitPassThreshold);
                double.TryParse(dt.Rows[0]["InsulateTestThreshold"].ToString(), out this.insulatePassThreshold);
                double.TryParse(dt.Rows[0]["VoltageWithStandardThreshold"].ToString(), out this.voltageWithStandardPassThreshold);
                var IsExistConduct = dt.Rows[0]["IsExistConductTest"].ToString();
                var IsExistCircuit = dt.Rows[0]["IsExistShortCircuitTest"].ToString();
                var IsExistInsulate = dt.Rows[0]["IsExistInsulateTest"].ToString();
                var IsExistVoltage = dt.Rows[0]["IsExistVoltageWithStandardTest"].ToString();
                if (IsExistConduct == "1")
                {
                    this.menu_shortCircuitTest.Enabled = true;
                    this.IsPassConductionTest = true;
                }
                if (IsExistCircuit == "1")
                {
                    this.menu_InsulationTest.Enabled = true;
                    this.menu_VoltageWithStandTest.Enabled = true;
                    this.IsPassShortCircuitTest = true;
                }
                if (IsExistInsulate == "1")
                {
                    this.IsPassInsulateTest = true;
                }
                if (IsExistVoltage == "1")
                {
                    this.IsPassVoltageWithStandardTest = true;
                }
            }
        }

        private void InitDefaultConfig()
        {
            configPath += DEVICE_CONFIG_FILE_NAME;
            if (!File.Exists(configPath))
            {
                INIFile.SetValue(CONFIG_SECTION_TEST_NAME, PROJECT_TEST_NUMBER_LEN_KEY, this.projectTestNumberLen.ToString(), configPath);
                INIFile.SetValue(CONFIG_SECTION_NAME, CONDUCTION_THRESHOLD_KEY, CONDUCTION_THRESHOLD, configPath);
                INIFile.SetValue(CONFIG_SECTION_NAME, IS_CONDUCTION_JUDGE_THAN_THRESHOLD_KEY, IS_CONDUCTION_JUDGE_THAN_THRESHOLD, configPath);
                INIFile.SetValue(CONFIG_SECTION_NAME, INSULATE_THRESHOLD_KEY, INSULATE_THRESHOLD, configPath);
                INIFile.SetValue(CONFIG_SECTION_NAME, IS_INSULATE_JUDGE_THAN_THRESHOLD_KEY, IS_INSULATE_JUDGE_THAN_THRESHOLD, configPath);
                INIFile.SetValue(CONFIG_SECTION_NAME, INSULATE_VOLTAGE_KEY, INSULATE_VOLTAGE, configPath);
                INIFile.SetValue(CONFIG_SECTION_NAME, INSULATE_HOLD_TIME_KEY, INSULATE_HOLD_TIME, configPath);
                INIFile.SetValue(CONFIG_SECTION_NAME, PRESSURE_PROOF_THRESHOLD_KEY, PRESSURE_PROOF_THRESHOLD, configPath);
                INIFile.SetValue(CONFIG_SECTION_NAME, IS_PRESSURE_JUDGE_THAN_THRESHOLD_KEY, IS_PRESSURE_JUDGE_THAN_THRESHOLD, configPath);
                INIFile.SetValue(CONFIG_SECTION_NAME, PRESSURE_PROOF_VOLTAGE_KEY, PRESSURE_PROOF_VOLTAGE, configPath);
                INIFile.SetValue(CONFIG_SECTION_NAME, PRESSURE_PROOF_HOLD_TIME_KEY, PRESSURE_PROOF_HOLD_TIME, configPath);
            }
            else
            {
                this.serviceURL = INIFile.GetValue(DEVICE_CONFIG_SECTION, DEVICE_CONFIG_SERVER_URL_KEY, configPath).ToString();
                var port = INIFile.GetValue(DEVICE_CONFIG_SECTION, DEVICE_CONFIG_SERVER_PORT_KEY, configPath).ToString();
                int.TryParse(port, out this.servicePort);

                var testNumberLen = INIFile.GetValue(CONFIG_SECTION_TEST_NAME, PROJECT_TEST_NUMBER_LEN_KEY, configPath).ToString();
                int testnLen;
                if (testNumberLen != "")
                {
                    int.TryParse(testNumberLen, out testnLen);
                    this.projectTestNumberLen = testnLen;
                }
                else
                    this.projectTestNumberLen = 8;
                var conduction_threshold = INIFile.GetValue(CONFIG_SECTION_NAME, CONDUCTION_THRESHOLD_KEY, configPath);
                if (conduction_threshold != "")
                    this.CONDUCTION_THRESHOLD = conduction_threshold.ToString();
                var is_conduction_judge_than_threshold = INIFile.GetValue(CONFIG_SECTION_NAME, IS_CONDUCTION_JUDGE_THAN_THRESHOLD_KEY, configPath).ToString();
                if (is_conduction_judge_than_threshold != "")
                    this.IS_CONDUCTION_JUDGE_THAN_THRESHOLD = is_conduction_judge_than_threshold;
                var insulate_threshold = INIFile.GetValue(CONFIG_SECTION_NAME, INSULATE_THRESHOLD_KEY, configPath).ToString();
                if (insulate_threshold != "")
                    this.INSULATE_THRESHOLD = insulate_threshold;
                var is_insulate_judge_than_threshold = INIFile.GetValue(CONFIG_SECTION_NAME, IS_INSULATE_JUDGE_THAN_THRESHOLD_KEY, configPath).ToString();
                if (is_insulate_judge_than_threshold != "")
                    this.IS_INSULATE_JUDGE_THAN_THRESHOLD = is_insulate_judge_than_threshold;
                var insulate_voltage = INIFile.GetValue(CONFIG_SECTION_NAME, INSULATE_VOLTAGE_KEY, configPath).ToString();
                if (insulate_voltage != "")
                    this.INSULATE_VOLTAGE = insulate_voltage;
                var insulate_hold_time = INIFile.GetValue(CONFIG_SECTION_NAME, INSULATE_HOLD_TIME_KEY, configPath).ToString();
                if (insulate_hold_time != "")
                    this.INSULATE_HOLD_TIME = insulate_hold_time;
                var presure_proof_threshold = INIFile.GetValue(CONFIG_SECTION_NAME, PRESSURE_PROOF_THRESHOLD_KEY, configPath).ToString();
                if (presure_proof_threshold != "")
                    this.PRESSURE_PROOF_THRESHOLD = presure_proof_threshold;
                var is_pressure_judge_than_threshold = INIFile.GetValue(CONFIG_SECTION_NAME, IS_PRESSURE_JUDGE_THAN_THRESHOLD_KEY, configPath).ToString();
                if (is_pressure_judge_than_threshold != "")
                    this.IS_CONDUCTION_JUDGE_THAN_THRESHOLD = is_pressure_judge_than_threshold;
                var pressure_voltage = INIFile.GetValue(CONFIG_SECTION_NAME, PRESSURE_PROOF_VOLTAGE_KEY, configPath).ToString();
                if (pressure_voltage != "")
                    this.PRESSURE_PROOF_VOLTAGE = pressure_voltage;
                var pressure_hold_time = INIFile.GetValue(CONFIG_SECTION_NAME, PRESSURE_PROOF_HOLD_TIME_KEY, configPath).ToString();
                if (pressure_hold_time != "")
                    this.PRESSURE_PROOF_HOLD_TIME = pressure_hold_time;
            }

            cableJudgeThreshold = new CableJudgeThreshold();
            cableJudgeThreshold.ConductionThreshold = this.CONDUCTION_THRESHOLD;
            cableJudgeThreshold.IsConductionJudgeThanThreshold = this.IS_CONDUCTION_JUDGE_THAN_THRESHOLD;
            cableJudgeThreshold.InsulateThreshold = this.INSULATE_THRESHOLD;
            cableJudgeThreshold.IsInsulateJudgeThanThreshold = this.IS_INSULATE_JUDGE_THAN_THRESHOLD;
            cableJudgeThreshold.InsulateVoltage = this.INSULATE_VOLTAGE;
            cableJudgeThreshold.InsulateHoldTime = this.INSULATE_HOLD_TIME;
            cableJudgeThreshold.PressureProofThreshold = this.PRESSURE_PROOF_THRESHOLD;
            cableJudgeThreshold.IsPressureJudgeThanThreshold = this.IS_PRESSURE_JUDGE_THAN_THRESHOLD;
            cableJudgeThreshold.PressureProofVoltage = this.PRESSURE_PROOF_VOLTAGE;
            cableJudgeThreshold.PressureProofHoldTime = this.PRESSURE_PROOF_HOLD_TIME;
        }

        private void SaveDeviceConfig()
        {
            INIFile.SetValue(CONFIG_SECTION_TEST_NAME, PROJECT_TEST_NUMBER_LEN_KEY, this.projectTestNumberLen.ToString(), configPath);
            INIFile.SetValue(DEVICE_CONFIG_SECTION, DEVICE_CONFIG_SERVER_URL_KEY, this.serviceURL, configPath);
            INIFile.SetValue(DEVICE_CONFIG_SECTION, DEVICE_CONFIG_SERVER_PORT_KEY, this.servicePort.ToString(), configPath);
        }

        private bool IsExistGridView(string startInterface,string startContactPoint,string endInterface,string endContactPoint)
        {
            if (this.listViewCableTest.Items.Count < 1)
                return false;
            for (int i = 0;i < this.listViewCableTest.Items.Count;i++)
            {
                var lv = this.listViewCableTest.Items[i];
                if (startInterface == lv.SubItems[1].Text.ToString() && startContactPoint == lv.SubItems[2].Text.ToString() && endInterface == lv.SubItems[3].Text.ToString() && endContactPoint == lv.SubItems[4].Text.ToString())
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 解密算法
        /// </summary>
        /// <param name="sourceStr"></param>
        /// <returns></returns>
        public unsafe static string DecodeDisposeFunc(string sourceStr)
        {
            byte[] tempArray = null;
            string rStr = "";
            try
            {
                int iCount = 0;
                int iIndex = 0;
                tempArray = new byte[KLineTestProcessor.CHAR_MAX_LEN];
                for (int i = 0; i < KLineTestProcessor.CHAR_MAX_LEN; i++)
                {
                    tempArray[i] = 0;
                }
                while (true)
                {
                    try
                    {
                        string tempStr = sourceStr.Substring(iCount, 3);
                        if (tempStr.Length < 3)
                        {
                            break;
                        }
                        short siTemp = System.Convert.ToInt16(tempStr);
                        byte chTemp = (byte)siTemp;
                        tempArray[iIndex] = (byte)chTemp;
                        iIndex++;
                    }
                    catch (System.Exception ex_6D)
                    {
                        break;
                    }
                    iCount += 3;
                }
                rStr = System.Text.Encoding.Default.GetString(tempArray, 0, iIndex);
            }
            catch (System.Exception ex_8B)
            {
            }
            return rStr;
        }

        #endregion

        #region device send command 

        /*
         * 导通测试与绝缘测试、耐压测试，发送的测试内容相同
         * 短路测试不能发送相同起始接点的指令
         */ 
        private void StartVoltageWithStandardTest()
        {
            if (!IsConnectSuccess())
                return;
            if (!IsSetEnvironmentParams())
                return;
            var voltageWithStandardCommand = CommonTestSendCommand(GetConductionTestInfo(), voltageWithStandardFunCode);
            if (voltageWithStandardCommand.Length < 1)
                return;
            //清空耐压异常数
            this.voltageWithStandardTestExceptCount = 0;
            if (IsPassConductionTest && IsPassShortCircuitTest)
                this.menu_VoltageWithStandTest.Enabled = true;
            else
            {
                this.menu_VoltageWithStandTest.Enabled = false;
                return;
            }
            LogHelper.Log.Info("耐压测试开始下发指令...");
            this.menu_VoltageWithStandTest.Enabled = false;
            this.IsPassVoltageWithStandardTest = false;//更新测试状态为未完成
            SuperEasyClient.SendMessage(DeviceFunCodeEnum.RequestHead, voltageWithStandardCommand);
            this.lbx_testStatus.Text = "耐压已开始测试，等待测试结果...";
            MessageBox.Show("耐压测试指令已下发", "提示", MessageBoxButtons.OK);
        }

        private void StartInsulateTest()
        {
            if (!IsConnectSuccess())
                return;
            if (!IsSetEnvironmentParams())
                return;
            var insulateCommand = CommonTestSendCommand(GetConductionTestInfo(), insulateTestFunCode);
            if (insulateCommand.Length < 1)
                return;
            //清空绝缘异常数
            this.insulateTestExceptCount = 0;
            if (IsPassConductionTest && IsPassShortCircuitTest)
                this.menu_InsulationTest.Enabled = true;
            else
            {
                this.menu_InsulationTest.Enabled = false;
                return;
            }
            LogHelper.Log.Info("绝缘测试开始下发指令...");
            this.menu_InsulationTest.Enabled = false;
            this.IsPassInsulateTest = false;//更新测试状态为未完成
            SuperEasyClient.SendMessage(DeviceFunCodeEnum.RequestHead, insulateCommand);
            this.lbx_testStatus.Text = "绝缘已开始测试，等待测试结果...";
            MessageBox.Show("绝缘测试指令已下发", "提示", MessageBoxButtons.OK);
        }

        #region 短路测试
        private void StartCircuitTest()
        {
            if (!IsConnectSuccess())
                return;
            if (!IsSetEnvironmentParams())
                return;
            var shortCircuitCommand = CommonTestSendCommand(GetShortCircuitTestInfo(), shortCircuitTestFunCode);
            if (shortCircuitCommand.Length < 1)
                return;
            //清空短路异常数
            this.shortCircuitTestExceptCount = 0;
            if (IsPassConductionTest)
            {
                this.menu_shortCircuitTest.Enabled = true;
            }
            else
            {
                this.menu_shortCircuitTest.Enabled = false;
                return;
            }

            LogHelper.Log.Info("短路测试开始下发指令...");
            this.menu_shortCircuitTest.Enabled = false;
            this.menu_InsulationTest.Enabled = false;
            this.menu_VoltageWithStandTest.Enabled = false;
            this.IsPassShortCircuitTest = false;//更新测试状态为未完成
            SuperEasyClient.SendMessage(DeviceFunCodeEnum.RequestHead, shortCircuitCommand);
            this.lbx_testStatus.Text = "短路已开始测试，等待测试结果...";
            MessageBox.Show("短路测试指令已下发", "提示", MessageBoxButtons.OK);
        }

        /// <summary>
        /// 短路测试发送指令区别于导通测试-不能发送相同的起始接点
        /// </summary>
        /// <returns></returns>
        private string GetShortCircuitTestInfo()
        {
            if (this.listViewCableTest.Items.Count < 1)
            {
                MessageBox.Show("没有可以测试的内容！", "提示", MessageBoxButtons.OK);
                return "";
            }
            string hexString = "";
            var method = "";
            List<string> startInterPointList = new List<string>();
            for (int i = 0;i < this.listViewCableTest.Items.Count;i++)
            {
                var lv = this.listViewCableTest.Items[i];
                var startInterface = lv.SubItems[1].Text.ToString();
                var startPoint = lv.SubItems[2].Text.ToString();
                if (startInterPointList.Contains(startPoint))
                    continue;
                startInterPointList.Add(startPoint);
                var endInterface = lv.SubItems[3].Text.ToString();
                var endPoint = lv.SubItems[4].Text.ToString();
                var conductTestResult = lv.SubItems[6].Text;
                var insulateTestResult = lv.SubItems[8].Text;
                var voltageTestResult = lv.SubItems[10].Text;
                var measuringMethod = lv.SubItems[13].Text.ToString();
                if (conductTestResult != null)
                {
                    if (conductTestResult.ToString() == "合格")
                        continue;
                }
                int startpoint, endpoint;
                int.TryParse(startPoint, out startpoint);
                int.TryParse(endPoint, out endpoint);
                method = measuringMethod;
                hexString += Convert.ToString(startpoint, 16).PadLeft(4, '0') + Convert.ToString(endpoint, 16).PadLeft(4, '0');
            }
            hexString = Convert.ToString(method).PadLeft(2, '0') + hexString;
            LogHelper.Log.Info("短路测试内容："+hexString);
            return hexString;
        }
        #endregion

        private void StartOneClickTest()
        {
            //一键测试：启动第一项测试
            if (IsStartConductTest)
            {
                StartConductTest();
            }
            else
            {
                //未选择导通测试，选择短路测试
                if (IsStartShortCircuitTest)
                {
                    StartCircuitTest();
                }
                else
                {
                    //未选择导通测试与短路测试
                    if (IsStartInsulateTest)
                    {
                        StartInsulateTest();
                    }
                    else
                    {
                        if (IsStartVoltageWithStandardTest)
                        {
                            StartVoltageWithStandardTest();
                        }
                    }
                }
            }
        }

        private void SendSelfStudyCommand(string selfStudyString)
        {
            byte[] selfStudyCommand = new byte[2 + selfStudyString.Length / 2];
            var selfStudyData = (selfStudyTestFunCode + " " + SplitStringByEmpty(selfStudyString)).Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var commandHex = ConvertByte.HexStringToByte(selfStudyCommand, selfStudyData, 0);
            SuperEasyClient.SendMessage(DeviceFunCodeEnum.RequestHead, commandHex);
            //MessageBox.Show("已发送自学习指令！", "提示", MessageBoxButtons.OK);
        }

        #region this is conduct test content
        private void StartConductTest()
        {
            if (!IsConnectSuccess())
                return;
            if (!IsSetEnvironmentParams())
                return;
            var conductionCommand = CommonTestSendCommand(GetConductionTestInfo(), conductionTestFunCode);
            if (conductionCommand.Length < 1)
                return;
            LogHelper.Log.Info("导通测试开始下发指令...");
            this.conductTestExceptCount = 0;//清空导通异常数
            this.menu_ConductionTest.Enabled = false;
            this.menu_shortCircuitTest.Enabled = false;
            this.menu_InsulationTest.Enabled = false;
            this.menu_VoltageWithStandTest.Enabled = false;
            this.IsPassConductionTest = false;//更新测试状态未未完成
            SuperEasyClient.SendMessage(DeviceFunCodeEnum.RequestHead, conductionCommand);
            this.lbx_testStatus.Text = "导通已开始测试，等待测试结果...";
            MessageBox.Show("导通测试指令已下发", "提示", MessageBoxButtons.OK);
        }

        private string GetConductionTestInfo()
        {
            if (this.listViewCableTest.Items.Count < 1)
            {
                MessageBox.Show("没有可以测试的内容！", "提示", MessageBoxButtons.OK);
                return "";
            }
            string hexString = "";
            var method = "";
            for (int i = 0;i < this.listViewCableTest.Items.Count;i++)
            {
                var lv = this.listViewCableTest.Items[i];
                var startInterface = lv.SubItems[1].Text.ToString();
                var startPoint = lv.SubItems[2].Text.ToString();
                var endInterface = lv.SubItems[3].Text.ToString();
                var endPoint = lv.SubItems[4].Text.ToString();
                var conductTestResult = lv.SubItems[6].Text;
                var insulateTestResult = lv.SubItems[8].Text;
                var voltageTestResult = lv.SubItems[10].Text;
                var measuringMethod = lv.SubItems[13].Text.ToString();
                if (conductTestResult != null)
                {
                    if (conductTestResult.ToString() == "合格")
                        continue;
                }
                int startpoint, endpoint;
                int.TryParse(startPoint, out startpoint);
                int.TryParse(endPoint, out endpoint);
                method = measuringMethod;
                hexString += Convert.ToString(startpoint, 16).PadLeft(4, '0') + Convert.ToString(endpoint, 16).PadLeft(4, '0');
            }
            hexString = Convert.ToString(method).PadLeft(2, '0') + hexString;

            return hexString;
        }

        #endregion

        /// <summary>
        /// 测试指令
        /// </summary>
        /// <param name="testCommandString">测试接点内容</param>
        /// <param name="testFunCode">功能码</param>
        /// <param name="testMethod">二线法/四线法，02/04</param>
        /// <returns></returns>
        private byte[] CommonTestSendCommand(string testCommandString,string testFunCode)
        {
            //eg:FF FF 00 0D F2 AA 00 01 00 02 00 03 00 06 01 02 01 0A
            //explain:1-2,3-6,258-266
            if (testCommandString.Length < 1)
                return new byte[0];
            byte[] conductionByte = new byte[2 + testCommandString.Length / 2];
            var conductArray = (testFunCode + " " + SplitStringByEmpty(testCommandString)).Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return ConvertByte.HexStringToByte(conductionByte, conductArray, 0);
        }

        private string SplitStringByEmpty(string hexString)
        {
            var arrayString = string.Empty;
            var conductionInfoChar = hexString.ToCharArray();
            for (int i = 0; i < conductionInfoChar.Length; i += 2)
            {
                arrayString += conductionInfoChar[i].ToString() + conductionInfoChar[i + 1].ToString() + " ";
            }
            return arrayString;
        }

        /// <summary>
        /// 打开项目时，生成测试序列
        /// </summary>
        private void UpdateCurrentTestNumber()
        {
            var historyData = historyDataInfoManager.GetDataSetByFieldsAndWhere("TestSerialNumber", "order by TestSerialNumber desc limit 1").Tables[0];
            if (historyData.Rows.Count < 1)
            {
                this.projectTestNumber = "1".PadLeft(this.projectTestNumberLen, '0');
                return;
            }
            var testNumberStr = historyData.Rows[0][0].ToString().Trim();
            var testNumber = 0;
            if (testNumberStr.Length > this.projectTestNumberLen)
                this.projectTestNumberLen = testNumberStr.Length;
            int.TryParse(testNumberStr,out testNumber);
            this.projectTestNumber = (testNumber + 1).ToString().PadLeft(this.projectTestNumberLen, '0');
        }

        private bool IsSetEnvironmentParams()
        {
            if (this.environmentAmbientHumidity == 0 || this.environmentTemperature == 0)
            {
                if (MessageBox.Show("环境参数未设置，是否立即设置？", "提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.OK)
                {
                    if (OpenEnvironmentParamsSet())
                    {
                        IsSetEnvironmentParams();
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            return true;
        }
        #endregion
    }
}
