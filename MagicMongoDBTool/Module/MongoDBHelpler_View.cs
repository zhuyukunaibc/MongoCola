﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using MongoDB.Bson;
using MongoDB.Driver;
namespace MagicMongoDBTool.Module
{
    public static partial class MongoDBHelpler
    {
        /// <summary>
        /// 管理中服务器列表
        /// </summary>
        private static Dictionary<string, MongoServer> _mongoSrvLst = new Dictionary<String, MongoServer>();
        /// <summary>
        /// 增加管理服务器
        /// </summary>
        /// <param name="configLst"></param>
        /// <returns></returns>
        public static Boolean AddServer(List<ConfigHelper.MongoConnectionConfig> configLst)
        {
            try
            {
                foreach (ConfigHelper.MongoConnectionConfig config in configLst)
                {
                    if (_mongoSrvLst.ContainsKey(config.HostName))
                    {
                        _mongoSrvLst.Remove(config.HostName);
                    }
                    MongoServerSettings mongoSvrSetting = new MongoServerSettings();
                    mongoSvrSetting.ConnectionMode = ConnectionMode.Direct;
                    //Can't Use SlaveOk to a Route！！！
                    mongoSvrSetting.SlaveOk = config.IsSlaveOk;
                    mongoSvrSetting.Server = new MongoServerAddress(config.IpAddr, config.Port);
                    //MapReduce的时候将消耗大量时间。不过这里需要平衡一下，太长容易造成并发问题
                    mongoSvrSetting.SocketTimeout = new TimeSpan(0, 10, 0);
                    if ((config.UserName != string.Empty) & (config.Password != string.Empty))
                    {
                        //认证的设定:注意，这里的密码是明文
                        mongoSvrSetting.DefaultCredentials = new MongoCredentials(config.UserName, config.Password, config.LoginAsAdmin);
                    }
                    MongoServer masterMongoSvr = new MongoServer(mongoSvrSetting);
                    _mongoSrvLst.Add(config.HostName, masterMongoSvr);
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        #region"展示数据"
        /// <summary>
        /// 将Mongodb的服务器在树形控件中展示
        /// </summary>
        /// <param name="trvMongoDB"></param>
        public static void FillMongoServiceToTreeView(TreeView trvMongoDB)
        {
            trvMongoDB.Nodes.Clear();
            foreach (string mongoSvrKey in _mongoSrvLst.Keys)
            {
                MongoServer mongoSvr = _mongoSrvLst[mongoSvrKey];
                TreeNode mongoSrvNode = new TreeNode(mongoSvrKey + " [" + mongoSvr.Settings.Server.Host + ":" + mongoSvr.Settings.Server.Port + "]");
                try
                {
                    List<string> databaseNameList = new List<string>();
                    if (SystemManager.ConfigHelperInstance.ConnectionList[mongoSvrKey].DataBaseName != String.Empty)
                    {
                        TreeNode mongoDBNode = FillDataBaseInfoToTreeNode(SystemManager.ConfigHelperInstance.ConnectionList[mongoSvrKey].DataBaseName, mongoSvr, mongoSvrKey);
                        mongoDBNode.Tag = SINGLE_DATABASE_TAG + ":" + mongoSvrKey + "/" + SystemManager.ConfigHelperInstance.ConnectionList[mongoSvrKey].DataBaseName;
                        mongoSrvNode.Nodes.Add(mongoDBNode);
                        //单数据库模式
                        mongoSrvNode.Tag = SINGLE_DB_SERVICE_TAG + ":" + mongoSvrKey;
                    }
                    else
                    {
                        databaseNameList = mongoSvr.GetDatabaseNames().ToList<String>();
                        foreach (String strDBName in databaseNameList)
                        {
                            TreeNode mongoDBnode = FillDataBaseInfoToTreeNode(strDBName, mongoSvr, mongoSvrKey);
                            mongoSrvNode.Nodes.Add(mongoDBnode);
                        }
                        mongoSrvNode.Tag = SERVICE_TAG + ":" + mongoSvrKey;
                    }
                    trvMongoDB.Nodes.Add(mongoSrvNode);
                }
                catch (MongoAuthenticationException)
                {
                    //需要验证的数据服务器，没有Admin权限无法获得数据库列表
                    MessageBox.Show("认证信息错误，请检查Admin数据库的用户名和密码", "认证失败");
                }
            }
        }
        /// <summary>
        /// 获得一个表示数据库结构的节点
        /// </summary>
        /// <param name="strDBName"></param>
        /// <param name="mongoSvr"></param>
        /// <param name="mongoSvrKey"></param>
        /// <returns></returns>
        private static TreeNode FillDataBaseInfoToTreeNode(string strDBName, MongoServer mongoSvr, string mongoSvrKey)
        {
            TreeNode mongoDBNode;
            switch (strDBName)
            {
                case "admin":
                    mongoDBNode = new TreeNode("管理员权限(admin)");
                    break;
                case "local":
                    mongoDBNode = new TreeNode("本地(local)");
                    break;
                case "config":
                    mongoDBNode = new TreeNode("配置(config)");
                    break;
                default:
                    mongoDBNode = new TreeNode(strDBName);
                    break;
            }

            mongoDBNode.Tag = DATABASE_TAG + ":" + mongoSvrKey + "/" + strDBName;
            MongoDatabase mongoDB = mongoSvr.GetDatabase(strDBName);

            List<String> ColNameList = mongoDB.GetCollectionNames().ToList<String>();
            foreach (String strColName in ColNameList)
            {
                TreeNode mongoColNode = new TreeNode();
                try
                {
                    mongoColNode = FillCollectionInfoToTreeNode(strColName, mongoDB, mongoSvrKey);
                }
                catch (Exception)
                {
                    mongoColNode = new TreeNode(strColName + "[访问异常]");
                    throw;
                }
                mongoDBNode.Nodes.Add(mongoColNode);
            }
            return mongoDBNode;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="strColName"></param>
        /// <param name="mongoDB"></param>
        /// <param name="mongoSvrKey"></param>
        /// <returns></returns>
        private static TreeNode FillCollectionInfoToTreeNode(string strColName, MongoDatabase mongoDB, string mongoSvrKey)
        {
            TreeNode mongoColNode;
            String strTagColName = strColName;
            switch (strColName)
            {
                case "chunks":
                    if (mongoDB.Name == "config")
                    {
                        strColName = "数据块(" + strColName + ")";
                    }
                    break;
                case "collections":
                    if (mongoDB.Name == "config")
                    {
                        strColName = "数据集(" + strColName + ")";
                    }
                    break;
                case "databases":
                    if (mongoDB.Name == "config")
                    {
                        strColName = "数据库(" + strColName + ")";
                    }
                    break;
                case "lockpings":
                    if (mongoDB.Name == "config")
                    {
                        strColName = "数据锁(" + strColName + ")";
                    }
                    break;
                case "locks":
                    if (mongoDB.Name == "config")
                    {
                        strColName = "数据锁(" + strColName + ")";
                    }
                    break;
                case "mongos":
                    if (mongoDB.Name == "config")
                    {
                        strColName = "路由服务器(" + strColName + ")";
                    }
                    break;
                case "settings":
                    if (mongoDB.Name == "config")
                    {
                        strColName = "配置(" + strColName + ")";
                    }
                    break;
                case "shards":
                    if (mongoDB.Name == "config")
                    {
                        strColName = "分片(" + strColName + ")";
                    }
                    break;
                case "version":
                    if (mongoDB.Name == "config")
                    {
                        strColName = "版本(" + strColName + ")";
                    }
                    break;
                case "fs.chunks":
                    strColName = "数据块(" + strColName + ")";
                    break;
                case COLLECTION_NAME_GRID_FILE_SYSTEM:
                    strColName = "文件系统(" + strColName + ")";
                    break;
                case "oplog.rs":
                    strColName = "操作结果(" + strColName + ")";
                    break;
                case "system.indexes":
                    strColName = "索引(" + strColName + ")";
                    break;
                case COLLECTION_NAME_JAVASCRIPT:
                    strColName = "存储Javascript(" + strColName + ")";
                    break;
                case "system.replset":
                    strColName = "副本组(" + strColName + ")";
                    break;
                case "replset.minvalid":
                    strColName = "初始化同步(" + strColName + ")";
                    break;
                case COLLECTION_NAME_USER:
                    strColName = "用户列表(" + strColName + ")";
                    break;
                case "me":
                    if (mongoDB.Name == "local")
                    {
                        strColName = "副本组[从属机信息](" + strColName + ")";
                    }
                    break;
                case "slaves":
                    if (mongoDB.Name == "local")
                    {
                        strColName = "副本组[主机信息](" + strColName + ")";
                    }
                    break;
                default:
                    break;
            }
            mongoColNode = new TreeNode(strColName);
            if (strTagColName == COLLECTION_NAME_GRID_FILE_SYSTEM)
            {
                mongoColNode.Tag = GRID_FILE_SYSTEM_TAG + ":" + mongoSvrKey + "/" + mongoDB.Name + "/" + strTagColName;
            }
            else
            {
                mongoColNode.Tag = COLLECTION_TAG + ":" + mongoSvrKey + "/" + mongoDB.Name + "/" + strTagColName;
            }
            MongoCollection mongoCol = mongoDB.GetCollection(strTagColName);

            //Start ListIndex
            TreeNode mongoIndex = new TreeNode("Indexes");
            List<BsonDocument> indexList = mongoCol.GetIndexes().ToList<BsonDocument>();
            foreach (BsonDocument indexDoc in indexList)
            {
                TreeNode mongoIndexNode = new TreeNode("Index:" + indexDoc.GetValue("name"));
                foreach (String item in indexDoc.Names)
                {
                    TreeNode mongoIndexItemNode = new TreeNode(item + ":" + indexDoc.GetValue(item));

                    mongoIndexNode.Nodes.Add(mongoIndexItemNode);
                }
                mongoIndex.Nodes.Add(mongoIndexNode);
            }
            mongoColNode.Nodes.Add(mongoIndex);
            //End ListIndex

            //Start Data
            TreeNode mongoData = new TreeNode("Data");
            mongoData.Tag = DOCUMENT_TAG + ":" + mongoSvrKey + "/" + mongoDB.Name + "/" + strTagColName;
            mongoColNode.Nodes.Add(mongoData);
            //End Data
            return mongoColNode;
        }

        /// <summary>
        /// 是否有二进制数据
        /// </summary>
        private static Boolean _hasBSonBinary;
        /// <summary>
        /// 在第一次展示数据的时候，记录下字段名称，用于在Query的时候使用
        /// </summary>
        public static List<string> ColumnList = new List<string>();
        /// <summary>
        /// 展示数据
        /// </summary>
        /// <param name="strTag"></param>
        /// <param name="controls"></param>
        public static void FillDataToControl(string strTag, List<Control> controls)
        {
            string collectionPath = strTag.Split(":".ToCharArray())[1];
            String[] cp = collectionPath.Split("/".ToCharArray());
            MongoCollection mongoCol = _mongoSrvLst[cp[(int)PathLv.ServerLV]]
                                      .GetDatabase(cp[(int)PathLv.DatabaseLv])
                                      .GetCollection(cp[(int)PathLv.CollectionLV]);
            List<BsonDocument> dataList = new List<BsonDocument>();
            //Query condition:
            if (IsUseFilter)
            {
                dataList = mongoCol.FindAs<BsonDocument>(GetQuery())
                                   .SetSkip(SkipCnt)
                                   .SetFields(GetOutputFields())
                                   .SetSortOrder(GetSort())
                                   .SetLimit(SystemManager.ConfigHelperInstance.LimitCnt)
                                   .ToList<BsonDocument>();
            }
            else
            {
                dataList = mongoCol.FindAllAs<BsonDocument>()
                                   .SetSkip(SkipCnt)
                                   .SetLimit(SystemManager.ConfigHelperInstance.LimitCnt)
                                   .ToList<BsonDocument>();
            }
            if (dataList.Count == 0)
            {
                return;
            }
            if (SkipCnt == 0)
            {
                //第一次显示，获得整个记录集的长度
                CurrentCollectionTotalCnt = (int)mongoCol.FindAllAs<BsonDocument>().Count();
                ColumnList.Clear();
            }
            SetPageEnable();
            _hasBSonBinary = false;
            foreach (var control in controls)
            {
                switch (control.GetType().ToString())
                {
                    case "System.Windows.Forms.ListView":
                        FillDataToListView(cp[(int)PathLv.CollectionLV], (ListView)control, dataList);
                        break;
                    case "System.Windows.Forms.TextBox":
                        FillDataToTextBox(cp[(int)PathLv.CollectionLV], (TextBox)control, dataList);
                        break;
                    case "System.Windows.Forms.TreeView":
                        FillDataToTreeView(cp[(int)PathLv.CollectionLV], (TreeView)control, dataList);
                        break;
                    default:
                        break;
                }
            }
        }
        /// <summary>
        /// BsonValue转展示用字符
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static string ConvertForShow(BsonValue val)
        {
            String strVal;
            if (val.IsBsonBinaryData)
            {
                _hasBSonBinary = true;
                return "[二进制数据]";
            }
            if (val.IsBsonNull) { return "[空值]"; }
            if (val.IsBsonDocument)
            {
                strVal = val.ToString() + "[包含" + val.ToBsonDocument().ElementCount + "个元素的文档]";
            }
            else
            {
                strVal = val.ToString();
            }
            return strVal;
        }
        /// <summary>
        /// 将数据放入TextBox里进行展示
        /// </summary>
        /// <param name="collectionName"></param>
        /// <param name="txtData"></param>
        /// <param name="dataList"></param>
        public static void FillDataToTextBox(string collectionName, TextBox txtData, List<BsonDocument> dataList)
        {
            txtData.Clear();
            if (_hasBSonBinary)
            {
                txtData.Text = "二进制数据块";
            }
            else
            {
                foreach (var item in dataList)
                {
                    txtData.Text += item.ToString() + "\r\n";
                }
            }
        }
        /// <summary>
        /// 将数据放入TreeView里进行展示
        /// </summary>
        /// <param name="collectionName"></param>
        /// <param name="trvData"></param>
        /// <param name="dataList"></param>
        public static void FillDataToTreeView(string collectionName, TreeView trvData, List<BsonDocument> dataList)
        {
            trvData.Nodes.Clear();
            foreach (BsonDocument item in dataList)
            {
                String TreeText = String.Empty;
                if (!item.GetElement(0).Value.IsBsonArray)
                {
                    TreeText = item.GetElement(0).Name + ":" + item.GetElement(0).Value.ToString();
                }
                else
                {
                    TreeText = item.GetElement(0).Name + ":" + collectionName;
                }
                TreeNode dataNode = new TreeNode(TreeText);

                //这里保存真实的主Key数据，删除的时候使用
                dataNode.Tag = item.GetElement(0).Value;
                FillBsonDocToTreeNode(dataNode, item);
                trvData.Nodes.Add(dataNode);
            }
        }
        /// <summary>
        /// 将数据放入TreeNode里进行展示
        /// </summary>
        /// <param name="trvnode"></param>
        /// <param name="doc"></param>
        private static void FillBsonDocToTreeNode(TreeNode trvnode, BsonDocument doc)
        {
            foreach (var item in doc.Elements)
            {
                if (item.Value.IsBsonDocument)
                {
                    TreeNode t = new TreeNode(item.Name);
                    FillBsonDocToTreeNode(t, item.Value.ToBsonDocument());
                    trvnode.Nodes.Add(t);
                }
                else
                {
                    if (item.Value.IsBsonArray)
                    {
                        TreeNode t = new TreeNode(item.Name);
                        foreach (var SubItem in item.Value.AsBsonArray)
                        {
                            TreeNode m = new TreeNode(SubItem.ToString());
                            t.Nodes.Add(m);
                        }
                        trvnode.Nodes.Add(t);
                    }
                    else
                    {
                        trvnode.Nodes.Add(item.Name + ":" + ConvertForShow(item.Value));
                    }
                }
            }
        }
        /// <summary>
        /// 将数据放入ListView中进行展示
        /// </summary>
        /// <param name="strTag"></param>
        /// <param name="lstData"></param>
        public static void FillDataToListView(string collectionName, ListView lstData, List<BsonDocument> dataList)
        {
            lstData.Clear();
            lstData.SmallImageList = null;
            switch (collectionName)
            {
                case COLLECTION_NAME_GRID_FILE_SYSTEM:
                    SetGridFileToListView(dataList, lstData);
                    break;
                case COLLECTION_NAME_USER:
                    SetUserListToListView(dataList, lstData);
                    break;
                default:
                    List<String> Columnlist = new List<String>();
                    foreach (BsonDocument Docitem in dataList)
                    {
                        ListViewItem lstItem = new ListViewItem();
                        foreach (String item in Docitem.Names)
                        {
                            if (!Columnlist.Contains(item))
                            {
                                Columnlist.Add(item);
                                lstData.Columns.Add(item);
                                ColumnList.Add(item);
                            }
                        }
                        //Key:_id
                        lstItem.Text = Docitem.GetValue(Columnlist[0]).ToString();
                        //这里保存真实的主Key数据，删除的时候使用
                        lstItem.Tag = Docitem.GetValue(Columnlist[0]);
                        //OtherItems
                        for (int i = 1; i < Columnlist.Count; i++)
                        {
                            BsonValue val;
                            Docitem.TryGetValue(Columnlist[i].ToString(), out val);
                            if (val == null)
                            {
                                lstItem.SubItems.Add("");
                            }
                            else
                            {
                                lstItem.SubItems.Add(ConvertForShow(val));
                            }
                        }
                        lstData.Items.Add(lstItem);
                    }
                    break;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataList"></param>
        /// <param name="lstData"></param>
        private static void SetUserListToListView(List<BsonDocument> dataList, ListView lstData)
        {
            lstData.Clear();
            lstData.Columns.Add("ID");
            lstData.Columns.Add("用户名");
            lstData.Columns.Add("是否只读");
            //密码是明码表示的，这里可能会有安全隐患
            lstData.Columns.Add("密码");
            foreach (BsonDocument docFile in dataList)
            {
                ListViewItem lstItem = new ListViewItem();
                lstItem.Text = docFile.GetValue("_id").ToString();
                lstItem.SubItems.Add(docFile.GetValue("user").ToString());
                lstItem.SubItems.Add(docFile.GetValue("readOnly").ToString());
                lstItem.SubItems.Add(docFile.GetValue("pwd").ToString());
                lstData.Items.Add(lstItem);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataList"></param>
        /// <param name="lstData"></param>
        private static void SetGridFileToListView(List<BsonDocument> dataList, ListView lstData)
        {
            lstData.Clear();
            lstData.Columns.Add("文件名称");
            lstData.Columns.Add("文件大小");
            lstData.Columns.Add("块大小");
            lstData.Columns.Add("上传日期");
            lstData.Columns.Add("MD5");
            lstData.SmallImageList = GetSystemIcon.IconImagelist;
            foreach (BsonDocument docFile in dataList)
            {
                ListViewItem lstItem = new ListViewItem();
                lstItem.ImageIndex = GetSystemIcon.GetIconIndexByFileName(docFile.GetValue("filename").ToString(), false);
                lstItem.Text = docFile.GetValue("filename").ToString();
                lstItem.SubItems.Add(GetSize((int)docFile.GetValue("length")));
                lstItem.SubItems.Add(GetSize((int)docFile.GetValue("chunkSize")));
                lstItem.SubItems.Add(ConvertForShow(docFile.GetValue("uploadDate")));
                lstItem.SubItems.Add(ConvertForShow(docFile.GetValue("md5")));
                lstData.Items.Add(lstItem);
            }
        }
        #endregion

        #region"展示状态"
        public static void FillDBStatusToList(ListView lstData)
        {
            lstData.Clear();
            lstData.Columns.Add("名称");
            lstData.Columns.Add("文档数量");
            lstData.Columns.Add("实际大小");
            lstData.Columns.Add("占用大小");
            lstData.Columns.Add("索引");
            lstData.Columns.Add("平均对象大小");
            lstData.Columns.Add("填充因子");
            foreach (String mongoSvrKey in _mongoSrvLst.Keys)
            {
                MongoServer mongoSvr = _mongoSrvLst[mongoSvrKey];
                List<string> databaseNameList = mongoSvr.GetDatabaseNames().ToList<string>();
                foreach (String strDBName in databaseNameList)
                {
                    MongoDatabase mongoDB = mongoSvr.GetDatabase(strDBName);

                    List<String> colNameList = mongoDB.GetCollectionNames().ToList<String>();
                    foreach (String strColName in colNameList)
                    {

                        CollectionStatsResult dbStatus = mongoDB.GetCollection(strColName).GetStats();
                        ListViewItem lst = new ListViewItem(strDBName + "." + strColName);
                        lst.SubItems.Add(dbStatus.ObjectCount.ToString());
                        lst.SubItems.Add(GetSize(dbStatus.DataSize));
                        lst.SubItems.Add(GetSize(dbStatus.StorageSize));
                        lst.SubItems.Add(GetSize(dbStatus.TotalIndexSize));
                        try
                        {
                            //在某些条件下，这个值会抛出异常，IndexKeyNotFound
                            lst.SubItems.Add(GetSize((long)dbStatus.AverageObjectSize));
                        }
                        catch (Exception)
                        {
                            lst.SubItems.Add("-");
                        }
                        try
                        {
                            //在某些条件下，这个值会抛出异常，IndexKeyNotFound
                            lst.SubItems.Add(dbStatus.PaddingFactor.ToString());
                        }
                        catch (Exception)
                        {
                            lst.SubItems.Add("-");
                        }
                        lstData.Items.Add(lst);
                    }
                }
            }
        }
        public static void FillSrvStatusToList(ListView lstData)
        {
            lstData.Clear();
            lstData.Columns.Add("名称");
            lstData.Columns.Add("数据集数量");
            lstData.Columns.Add("数据大小");
            lstData.Columns.Add("文件大小");
            lstData.Columns.Add("索引数量");
            lstData.Columns.Add("索引数量大小");
            lstData.Columns.Add("对象数量");
            lstData.Columns.Add("占用大小");
            foreach (String mongoSvrKey in _mongoSrvLst.Keys)
            {
                MongoServer mongoSvr = _mongoSrvLst[mongoSvrKey];
                List<String> databaseNameList = mongoSvr.GetDatabaseNames().ToList<String>();
                foreach (String strDBName in databaseNameList)
                {
                    MongoDatabase mongoDB = mongoSvr.GetDatabase(strDBName);
                    DatabaseStatsResult dbStatus = mongoDB.GetStats();
                    ListViewItem lst = new ListViewItem(mongoSvrKey + "." + strDBName);
                    try
                    {
                        lst.SubItems.Add(dbStatus.CollectionCount.ToString());

                    }
                    catch (Exception)
                    {

                        lst.SubItems.Add(string.Empty);
                    }

                    lst.SubItems.Add(GetSize(dbStatus.DataSize));
                    lst.SubItems.Add(GetSize(dbStatus.FileSize));
                    lst.SubItems.Add(dbStatus.IndexCount.ToString());
                    lst.SubItems.Add(GetSize(dbStatus.IndexSize));
                    lst.SubItems.Add(dbStatus.ObjectCount.ToString());
                    lst.SubItems.Add(GetSize(dbStatus.StorageSize));
                    lstData.Items.Add(lst);
                }
            }
        }
        public static void FillSrvOprToList(ListView lstData)
        {
            lstData.Clear();
            Boolean hasHeader = false;
            foreach (String mongoSvrKey in _mongoSrvLst.Keys)
            {
                MongoServer mongosvr = _mongoSrvLst[mongoSvrKey];
                List<String> databaseNameList = mongosvr.GetDatabaseNames().ToList<String>();
                foreach (String strDBName in databaseNameList)
                {
                    MongoDatabase mongoDB = mongosvr.GetDatabase(strDBName);
                    BsonDocument dbStatus = mongoDB.GetCurrentOp();
                    if (dbStatus.GetValue("inprog").AsBsonArray.Count > 0)
                    {
                        if (!hasHeader)
                        {

                            lstData.Columns.Add("Name");
                            foreach (String item in dbStatus.GetValue("inprog").AsBsonArray[0].AsBsonDocument.Names)
                            {
                                lstData.Columns.Add(item);
                            }
                            hasHeader = true;
                        }

                        BsonArray doc = dbStatus.GetValue("inprog").AsBsonArray;
                        foreach (BsonDocument item in doc)
                        {
                            ListViewItem lst = new ListViewItem(mongoSvrKey + "." + strDBName);
                            foreach (String itemName in item.Names)
                            {
                                lst.SubItems.Add(item.GetValue(itemName).ToString());
                            }
                            lstData.Items.Add(lst);
                        }
                    }
                }
            }
        }
        #endregion

        #region"数据导航"
        /// <summary>
        /// 数据集总记录数
        /// </summary>
        public static int CurrentCollectionTotalCnt = 0;
        /// <summary>
        /// Skip记录数
        /// </summary>
        public static int SkipCnt = 0;
        /// <summary>
        /// 是否存在下一页
        /// </summary>
        public static Boolean HasNextPage;
        /// <summary>
        /// 是否存在上一页
        /// </summary>
        public static Boolean HasPrePage;
        /// <summary>
        /// 数据导航
        /// </summary>
        public enum PageChangeOpr
        {
            /// <summary>
            /// 第一页
            /// </summary>
            FirstPage,
            /// <summary>
            /// 最后一页
            /// </summary>
            LastPage,
            /// <summary>
            /// 上一页
            /// </summary>
            PrePage,
            /// <summary>
            /// 下一页
            /// </summary>
            NextPage
        }

        /// <summary>
        /// 换页操作
        /// </summary>
        /// <param name="IsNext"></param>
        /// <param name="strTag"></param>
        /// <param name="dataShower"></param>
        public static void PageChanged(PageChangeOpr pageChangeMode, string strTag, List<Control> dataShower)
        {
            switch (pageChangeMode)
            {
                case PageChangeOpr.FirstPage:
                    SkipCnt = 0;
                    break;
                case PageChangeOpr.LastPage:
                    if (CurrentCollectionTotalCnt % SystemManager.ConfigHelperInstance.LimitCnt == 0)
                    {
                        //没有余数的时候，600 % 100 == 0  => Skip = 600-100 = 500
                        SkipCnt = CurrentCollectionTotalCnt - SystemManager.ConfigHelperInstance.LimitCnt;
                    }
                    else
                    {
                        // 630 % 100 == 30  => Skip = 630-30 = 600  
                        SkipCnt = CurrentCollectionTotalCnt - CurrentCollectionTotalCnt % SystemManager.ConfigHelperInstance.LimitCnt;
                    }
                    break;
                case PageChangeOpr.NextPage:
                    SkipCnt += SystemManager.ConfigHelperInstance.LimitCnt;
                    break;
                case PageChangeOpr.PrePage:
                    SkipCnt -= SystemManager.ConfigHelperInstance.LimitCnt;
                    break;
                default:
                    break;
            }
            FillDataToControl(strTag, dataShower);
        }
        public static void SetPageEnable()
        {
            if (SkipCnt == 0)
            {
                HasPrePage = false;
            }
            else
            {
                HasPrePage = true;
            }
            if ((SkipCnt + SystemManager.ConfigHelperInstance.LimitCnt) >= CurrentCollectionTotalCnt)
            {
                HasNextPage = false;
            }
            else
            {
                HasNextPage = true;
            }
        }

        #endregion

        #region "辅助方法"
        private static String GetSize(long size)
        {
            String strSize = String.Empty;
            String[] Unit = new String[]{
                "Byte","KB","MB","GB","TB"
            };
            if (size == 0)
            {
                return "0 Byte";
            }
            byte unitOrder = 2;
            Double tempSize = size / Math.Pow(2, 20);
            while (!(tempSize > 0.1 & tempSize < 1000))
            {
                if (tempSize < 0.1)
                {
                    tempSize = tempSize * 1024;
                    unitOrder--;
                }
                else
                {

                    tempSize = tempSize / 1024;
                    unitOrder++;
                }
            }
            return string.Format("{0:F2}", tempSize) + " " + Unit[unitOrder];
        }
        #endregion

    }
}
