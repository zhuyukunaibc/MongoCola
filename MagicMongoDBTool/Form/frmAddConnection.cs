﻿using System;
using System.Collections.Generic;
using System.Windows.Forms;
using MagicMongoDBTool.Module;
using MongoDB.Driver;
namespace MagicMongoDBTool
{

    public partial class frmAddConnection : Form
    {
        //http://www.mongodb.org/display/DOCS/Connections

        /// <summary>
        /// 连接配置
        /// </summary>
        public ConfigHelper.MongoConnectionConfig ModifyConn = new ConfigHelper.MongoConnectionConfig();
        /// <summary>
        /// 初始化（新建）
        /// </summary>
        public frmAddConnection()
        {
            InitializeComponent();
            OnLoad();
        }
        private void OnLoad()
        {
            cmdCancel.Click += new EventHandler((x, y) => { this.Close(); });
            numPort.GotFocus += new EventHandler((x, y) =>
            {
                numPort.Select(0, 5);
            });
            if (!SystemManager.IsUseDefaultLanguage())
            {
                this.Text = SystemManager.mStringResource.GetText(StringResource.TextType.AddConnection_Title);
                lblConnectionName.Text = SystemManager.mStringResource.GetText(StringResource.TextType.AddConnection_ConnectionName);
                lblHost.Text = SystemManager.mStringResource.GetText(StringResource.TextType.Common_Host);
                lblPort.Text = SystemManager.mStringResource.GetText(StringResource.TextType.Common_Port);
                lblUsername.Text = SystemManager.mStringResource.GetText(StringResource.TextType.Common_Username);
                lblPassword.Text = SystemManager.mStringResource.GetText(StringResource.TextType.Common_Password);
                lblDataBaseName.Text = SystemManager.mStringResource.GetText(StringResource.TextType.AddConnection_DBName);
                lblConnectionString.Text = SystemManager.mStringResource.GetText(StringResource.TextType.AddConnection_ConnectionString);
                lblAttentionPassword.Text = SystemManager.mStringResource.GetText(StringResource.TextType.AddConnection_Password_Description);

                chkSlaveOk.Text = SystemManager.mStringResource.GetText(StringResource.TextType.AddConnection_MasterSlave);
                chkSafeMode.Text = SystemManager.mStringResource.GetText(StringResource.TextType.AddConnection_SafeMode);
                lblTimeOut.Text = SystemManager.mStringResource.GetText(StringResource.TextType.AddConnection_TimeOut);

                lblMainReplsetName.Text = SystemManager.mStringResource.GetText(StringResource.TextType.AddConnection_MainReplsetName);


                cmdAdd.Text = SystemManager.mStringResource.GetText(StringResource.TextType.Common_Add);
                cmdCancel.Text = SystemManager.mStringResource.GetText(StringResource.TextType.Common_Cancel);
                cmdTest.Text = SystemManager.mStringResource.GetText(StringResource.TextType.Common_Test);
            }
        }
        /// <summary>
        /// 初始化（修改）
        /// </summary>
        /// <param name="ConnectionName"></param>
        public frmAddConnection(String ConnectionName)
        {
            InitializeComponent();
            //Modify Mode
            ModifyConn = SystemManager.ConfigHelperInstance.ConnectionList[ConnectionName];
            OnLoad();
            txtConnectionName.Text = ModifyConn.ConnectionName;
            txtConnectionName.Enabled = false;
            txtHost.Text = ModifyConn.Host;
            numPort.Text = ModifyConn.Port.ToString();
            txtUsername.Text = ModifyConn.UserName;
            txtPassword.Text = ModifyConn.Password;

            txtDataBaseName.Text = ModifyConn.DataBaseName;
            txtConnectionString.Text = ModifyConn.ConnectionString;

            chkSlaveOk.Checked = ModifyConn.IsSlaveOk;
            chkSafeMode.Checked = ModifyConn.IsSafeMode;
            NumSocketTimeOut.Value = ModifyConn.socketTimeoutMS;
            txtReplsetName.Text = ModifyConn.ReplSetName;

            if (SystemManager.IsUseDefaultLanguage())
            {
                cmdAdd.Text = "Modify";
            }
            else
            {
                cmdAdd.Text = SystemManager.mStringResource.GetText(StringResource.TextType.Common_Modify);
            }
        }
        /// <summary>
        /// 新建或者修改
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cmdAdd_Click(object sender, EventArgs e)
        {
            CreateConnection();
            //保存配置
            if (SystemManager.ConfigHelperInstance.ConnectionList.ContainsKey(ModifyConn.ConnectionName))
            {
                SystemManager.ConfigHelperInstance.ConnectionList[ModifyConn.ConnectionName] = ModifyConn;
            }
            else
            {
                SystemManager.ConfigHelperInstance.ConnectionList.Add(ModifyConn.ConnectionName, ModifyConn);
            }
            this.Close();
        }
        /// <summary>
        /// 测试连接
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cmdTest_Click(object sender, EventArgs e)
        {
            CreateConnection();
            try
            {
                MongoServer srv = MongoDBHelper.CreateMongoSetting(ref ModifyConn);
                srv.Connect();
                srv.Disconnect();
                MyMessageBox.ShowMessage("Connect Test", "Connected OK.");
            }
            catch (MongoAuthenticationException ex)
            {
                //需要验证的数据服务器，没有Admin权限无法获得数据库列表
                if (!SystemManager.IsUseDefaultLanguage())
                {
                    MyMessageBox.ShowMessage(SystemManager.mStringResource.GetText(StringResource.TextType.Exception_AuthenticationException),
                                             SystemManager.mStringResource.GetText(StringResource.TextType.Exception_AuthenticationException_Note), ex.ToString(), true);
                }
                else
                {
                    MyMessageBox.ShowMessage("MongoAuthenticationException:", "Please check UserName and Password", ex.ToString(), true);
                }
            }
            catch (Exception ex)
            {
                //暂时不处理任何异常，简单跳过
                //无法连接的理由：
                //1.服务器没有启动
                //2.认证模式不正确
                if (!SystemManager.IsUseDefaultLanguage())
                {
                    MyMessageBox.ShowMessage(SystemManager.mStringResource.GetText(StringResource.TextType.Exception_NotConnected),
                                             SystemManager.mStringResource.GetText(StringResource.TextType.Exception_NotConnected_Note), ex.ToString(), true);
                }
                else
                {
                    MyMessageBox.ShowMessage("Exception", "Mongo Server may not Startup or Auth Mode is not correct", ex.ToString(), true);
                }
            }
        }
        /// <summary>
        /// 新建连接
        /// </summary>
        private void CreateConnection()
        {
            ModifyConn.ConnectionName = txtConnectionName.Text;
            ///感谢 呆呆 的Bug 报告，不论txtConnectionString.Text是否存在都进行赋值，防止删除字符后，值还是保留的BUG
            ModifyConn.ConnectionString = txtConnectionString.Text;
            if (txtConnectionString.Text != String.Empty)
            {
                if (!MongoDBHelper.FillConfigWithConnectionString(ref ModifyConn))
                {
                    MyMessageBox.ShowMessage("Url Exception", "Url Formation，please check it");
                    return;
                };
            }
            else
            {
                ModifyConn.Host = txtHost.Text;
                if (numPort.Text != String.Empty)
                {
                    ModifyConn.Port = Convert.ToInt32(numPort.Text);
                }
                ModifyConn.UserName = txtUsername.Text;
                ModifyConn.Password = txtPassword.Text;
                ModifyConn.DataBaseName = txtDataBaseName.Text;

                //仅有用户名或密码
                if (txtUsername.Text != String.Empty && txtPassword.Text == String.Empty)
                {
                    MessageBox.Show("Please Input Password");
                    return;
                }
                if (txtUsername.Text == String.Empty && txtPassword.Text != String.Empty)
                {
                    MessageBox.Show("Please Input UserName");
                    return;
                }
                //数据库名称存在，则必须输入用户名和密码
                if (txtDataBaseName.Text != String.Empty)
                {
                    //用户名或者密码为空
                    if (txtUsername.Text == String.Empty || txtPassword.Text == String.Empty)
                    {
                        MessageBox.Show("Please Input UserName or Password");
                        return;
                    }
                }

                ModifyConn.IsSafeMode = chkSafeMode.Checked;
                ModifyConn.socketTimeoutMS = (int)NumSocketTimeOut.Value;
                ModifyConn.IsSlaveOk = chkSlaveOk.Checked;
                ModifyConn.ReplSetName = txtReplsetName.Text;

            }
        }
        /// <summary>
        /// 添加HostList
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cmdAddHost_Click(object sender, EventArgs e)
        {
            String strHost = String.Empty;
            strHost = txtReplHost.Text;
            if (NumReplPort.Value != 0)
            {
                strHost += ":" + NumReplPort.Value.ToString();
            }
            lstHost.Items.Add(strHost);
        }

    }
}
