using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.Runtime.Remoting.Messaging;
using System.Xml;
using System.Threading;

namespace Accounting
{
    public partial class CorpStarbaseListConfig : UserControl
    {
        public CorpStarbaseListConfig(IMainControl mainControl, IHostWindow hostWindow)
        {
            InitializeComponent();

            mMainControl = mainControl;
            mOptions = mainControl.Options;
            mData = mainControl.Data;
            mSession = mainControl.Session;
            mSession.FunctionCompleted += new FunctionCompletedHandler(mSession_FunctionCompleted);
            mHostWindow = hostWindow;

            timerAsyncDebugRun.Interval = 500;
            timerAsyncDebugRun.Tick += new EventHandler(timerAsyncDebugRun_Tick);
            timerStringAsyncDebugRun.Interval = 500;
            timerStringAsyncDebugRun.Tick += new EventHandler(timerStringAsyncDebugRun_Tick);

            mTableFuel = new DataTable("TableFuel");
            mTableFuel.Columns.Add("resourceTypeId", typeof(int));
            mTableFuel.Columns.Add("resourceTypeName", typeof(string));
            mTableFuel.Columns.Add("volume", typeof(Double));
            mTableFuel.Columns.Add("quantityAtPeriod", typeof(Double));
            mTableFuel.Columns.Add("quantity", typeof(Double));
            //mTableFuel.Columns.Add("purposeText", typeof(string));
            mTableFuel.Columns.Add("fuelEnd", typeof(DateTime));
            mTableFuel.Columns.Add("quantityAtCustomPeriod", typeof(Double));
            mTableFuel.Columns.Add("volumeAtCustomPeriod", typeof(Double));

            mData.GetCorpStarbaseList(mOptions.CharacterId);
            mData.GetCorpStarbaseListForView(mOptions.CharacterId);
            SetupDataGridView(dgvCorpStarbaseList);

            mData.GetCorpStarbaseStructures(mOptions.CharacterId);
            SetupDataGridView(dgvDefinedStructures);
            SetupDataGridView(dgvUndefinedStructures);

            dgvCorpStarbaseList.SelectionChanged += new EventHandler(mHostWindow.ShowGridPosition);

        }
        #region variables
        #region простые переменные
        private IMainControl mMainControl;
        private IHostWindow mHostWindow;
        private Options mOptions;
        private Session mSession;
        private DataClass mData;
        System.Windows.Forms.Timer timerAsyncDebugRun = new System.Windows.Forms.Timer();
        System.Windows.Forms.Timer timerStringAsyncDebugRun = new System.Windows.Forms.Timer();
        #endregion

        #region private
        DataTable mTableFuel;
        DateTime mCustomDate = DateTime.MinValue;
        DataView mDVUndefinedStructures;
        DataView mDVDefinedStructures;
        DataGridView mDGVFrom;
        DataGridView mDGVTo;
        int mControlTowerId;
        #endregion
        #endregion

        #region FunctionAsync
        void RunFunctionAsync(FunctionCompeletedEventArgs args)
        {
            //подготовка, если требуется
            switch (args.Function)
            {
                case ApiFunction.CorpStarbaseList:
                    dgvCorpStarbaseList.DataSource = null;
                    break;
                case ApiFunction.CorpStarbaseDetail:
                    dgvCorpStarbaseList.DataSource = null;
                    break;
            }
#if DEBUG
            timerAsyncDebugRun.Tag = args;
            timerAsyncDebugRun.Start();
#else
            FunctionAsyncDelegate f = this.FunctionAsync;
            f.BeginInvoke(args, new AsyncCallback(FunctionCompletedCallBack), this);
#endif
        }

        ApiFunction FunctionAsync(FunctionCompeletedEventArgs args)
        {
            ApiFunction result = ApiFunction.None;
            XmlNode nodeRoot = args.XmlResponse.DocumentElement;
            XmlNode nodeCurrentTime = nodeRoot.SelectSingleNode("descendant::currentTime");
            DateTime currentTime = DateTime.Parse(nodeCurrentTime.InnerText);
            bool continueParsing = mOptions.ContinueParsing;
            int accountKey = Convert.ToInt32(mSession.AccountKey);
            switch (args.Function)
            {
                #region ApiFunction.CorpStarbaseList
                case ApiFunction.CorpStarbaseList:
                    {
                        mHostWindow.ShowStatus(0, "Обработка данных списка ПОСов...");
                        XmlNode nodeCorpStarbaseList = nodeRoot.SelectSingleNode("descendant::rowset[@name='starbases']");
                        DataTable dtCorpStarbaseList = DataClass.ParseRowset(nodeCorpStarbaseList);

                        //удаление существующих строк, если их нет среди новых
                        List<DataRow> oldRows = new List<DataRow>();
                        foreach (DataRow row in mData.TableCorpStarbaseList.Rows)
                        {
                            oldRows.Add(row);
                        }
                        int totalRows = dtCorpStarbaseList.Rows.Count;
                        int i1 = 0;
                        foreach (DataRow starbase in dtCorpStarbaseList.Rows)
                        {
                            mHostWindow.ShowStatus(i1++, dtCorpStarbaseList.Rows.Count);
                            //проверка наличия этой записи в базе
                            DataRow[] rows = mData.TableCorpStarbaseList.Select(String.Format("itemId = {0}", starbase["itemId"]));
                            DataRow newStarbase;

                            if (rows.Length == 0)
                                newStarbase = mData.TableCorpStarbaseList.NewRow();//если нету, добавляем
                            else
                            {
                                newStarbase = rows[0];//если есть, берём её
                                oldRows.Remove(newStarbase);
                            }

                            for (int i = 0; i < dtCorpStarbaseList.Columns.Count; i++)
                            {
                                if (mData.TableCorpStarbaseList.Columns.Contains(dtCorpStarbaseList.Columns[i].ColumnName))
                                {
                                    newStarbase[dtCorpStarbaseList.Columns[i].ColumnName] = starbase[dtCorpStarbaseList.Columns[i].ColumnName];
                                }
                            }
                            int typeId = Convert.ToInt32(starbase["typeId"]);
                            int cpu, power;
                            mData.GetTypeCpuPower(typeId, out cpu, out power);
                            newStarbase["cpu"] = cpu;
                            newStarbase["power"] = power;
                            if (rows.Length == 0)
                                mData.TableCorpStarbaseList.Rows.Add(newStarbase);
                            Thread.Sleep(10);
                            //планирование загрузки более подробной инфы об этом посе
                            mSession.CommandQueue.Enqueue(new Command(ApiFunction.CorpStarbaseDetail, mOptions.UserId, mOptions.ApiKey, mOptions.CharacterId, Convert.ToInt32(newStarbase["itemId"])));
                        }
                        foreach (DataRow row in oldRows)
                        {
                            row.Delete();
                        }
                        mHostWindow.ShowStatus(90, "Подтверждение данных...");
                        mData.CommitCorpStarbaseList();
                        //SetupDataGridView(dgvAlliances);
                        mHostWindow.ShowStatus(100, "Завершено");

                        result = args.Function;
                        break;
                    }
                #endregion
                #region ApiFunction.CorpStarbaseDetail
                case ApiFunction.CorpStarbaseDetail:
                    {
                        mHostWindow.ShowStatus(0, "Обработка данных...");
                        //XmlNode nodeCorpStarbaseDetail = nodeRoot.SelectSingleNode("descendant::rowset[@name='starbases']");
                        string starbaseDetail = args.XmlResponse.InnerXml;//nodeRoot.InnerXml;
                        int itemId = mSession.StarbaseId;
                        DataRow[] rows = mData.TableCorpStarbaseList.Select(String.Format("itemId = {0}", itemId));
                        if (rows.Length == 1)
                        {
                            rows[0]["starbaseDetail"] = starbaseDetail;
                            mData.CommitCorpStarbaseList();
                        }
                        mHostWindow.ShowStatus(100, "Завершено");
                        result = args.Function;
                        break;
                    }
                #endregion
            }
            if (result == ApiFunction.None)
                mHostWindow.ShowStatus("Произошла ошибка");
            else
                mHostWindow.ShowStatus("Завершено");
            return result;
        }
        void FunctionCompletedCallBack(IAsyncResult result)
        {
            try
            {
                AsyncResult r = (AsyncResult)result;
                FunctionAsyncDelegate command = (FunctionAsyncDelegate)r.AsyncDelegate;
                ApiFunction function = command.EndInvoke(result);
                FunctionCompleted(function);
            }
            catch (Exception exc)
            {
                mMainControl.ProcessException(System.Reflection.MethodInfo.GetCurrentMethod(), exc);
            }
        }
        void FunctionCompleted(ApiFunction function)
        {
            if (this.InvokeRequired)
            {
                this.Invoke((EventHandler)delegate
                {
                    FunctionCompleted(function);
                });
            }
            else
            {
                switch (function)
                {
                    #region ApiFunction.CorpStarbaseList
                    case ApiFunction.CorpStarbaseList:
                        {
                            SetupDataGridView(dgvCorpStarbaseList);
                            break;
                        }
                    #endregion
                    #region ApiFunction.CorpStarbaseDetail
                    case ApiFunction.CorpStarbaseDetail:
                        {
                            SetupDataGridView(dgvCorpStarbaseList);
                            break;
                        }
                    #endregion
                }
                mSession.ExecuteCommandFromQueue();
            }
        }
        void timerAsyncDebugRun_Tick(object sender, EventArgs e)
        {
            timerAsyncDebugRun.Stop();
            FunctionCompeletedEventArgs args = (FunctionCompeletedEventArgs)timerAsyncDebugRun.Tag;
            FunctionAsync(args);
            FunctionCompleted(args.Function);
        }

        delegate ApiFunction FunctionAsyncDelegate(FunctionCompeletedEventArgs args);
        #endregion

        private void BeforeDestroy()
        {
            mSession.FunctionCompleted -= new FunctionCompletedHandler(mSession_FunctionCompleted);
            dgvCorpStarbaseList.SelectionChanged -= new EventHandler(mHostWindow.ShowGridPosition);
        }

        private void mSession_FunctionCompleted(object sender, FunctionCompeletedEventArgs e)
        {
            if (e.ErrorMessage == "")
            {
                RunFunctionAsync(e);
            }
            else
            {
                mSession.ExecuteCommandFromQueue();
            }
        }

        private void OnButtonClick(object sender, EventArgs e)
        {
            if (sender == bUpdate)
            {
                mSession.GetFunctionAsync(ApiFunction.CorpStarbaseList);
                //XmlDocument xmlDoc = new XmlDocument();
                //xmlDoc.Load("C:\\Users\\Rius\\AppData\\Roaming\\Blind Octopus\\Accounting\\1.0.0.0\\download\\CorpStarbaseList\\2008.01.12 - 10.36.50.xml");
                //RunFunctionAsync(new FunctionCompeletedEventArgs(ApiFunction.CorpStarbaseList, xmlDoc, ""));
            }
            //if (sender == bClearStructuresList)
            //{
            //    if (MessageBox.Show(
            //        this,
            //        "Вы действительно хотите очистить список всех структур?",
            //        "Удаление структур",
            //        MessageBoxButtons.OKCancel,
            //        MessageBoxIcon.Question,
            //        MessageBoxDefaultButton.Button2) == DialogResult.OK)
            //    {
            //        mData.ClearCorpStarbaseStructures(mOptions.CharacterId);
            //    }
            //}
            if (sender == bInitializeStructuresList)
            {
                RunStringFunctionAsync("init structures list");
            }
        }

        private void SetupDataGridView(DataGridView dgv)
        {
            if (dgv == dgvCorpStarbaseList)
            {
                mData.GetCorpStarbaseListForView(mOptions.CharacterId);
                dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                dgv.DataSource = mData.DataSet;
                dgv.DataMember = "aCorpStarbaseListForView";
                //mData.FormatDataGridView(dgv);

                //dgv.Columns["recordId"].Visible = false;
                //dgv.Columns["userId"].Visible = false;
                dgv.Columns["moonId"].Visible = false;
                dgv.Columns["typeId"].Visible = false;
                dgv.Columns["locationId"].Visible = false;
                dgv.Columns["starbaseDetail"].Visible = false;
                dgv.Columns["locationName"].HeaderText = "Система";
                dgv.Columns["moonName"].HeaderText = "Луна";
                dgv.Columns["typeName"].HeaderText = "Башня";
                dgv.Columns["typeName"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                dgv.Columns["cpu"].Visible = false;
                dgv.Columns["power"].Visible = false;

                //if (!dgv.Columns.Contains("locationName"))
                //{
                //    DataGridViewComboBoxColumn dgvc = new DataGridViewComboBoxColumn();
                //    dgvc.DataPropertyName = "locationId";
                //    dgvc.DataSource = mData.DataSet;
                //    dgvc.DisplayMember = "aMapSovereignty.solarSystemName";
                //    dgvc.HeaderText = "LocationName";
                //    dgvc.Name = "LocationName";
                //    dgvc.ValueMember = "aMapSovereignty.solarSystemId";
                //    dgvc.ReadOnly = true;
                //    dgvc.DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing;
                //    dgv.Columns.Add(dgvc);
                //}
            }
            if (dgv == dgvDefinedStructures || dgv == dgvUndefinedStructures)
            {
                dgv.AutoGenerateColumns = false;
                dgv.Columns.Clear();
                DataGridViewCheckBoxColumn cbcol = new DataGridViewCheckBoxColumn();
                cbcol.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                cbcol.DataPropertyName = "online";
                cbcol.ReadOnly = false;
                cbcol.HeaderText = "online";
                dgv.Columns.Add(cbcol);

                DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn();
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                col.DataPropertyName = "typeName";
                col.ReadOnly = true;
                if (dgv == dgvDefinedStructures)
                    col.HeaderText = "Используемые";
                if (dgv == dgvUndefinedStructures)
                    col.HeaderText = "Неиспользуемые";
                dgv.Columns.Add(col);

                col = new DataGridViewTextBoxColumn();
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                col.DataPropertyName = "containedTypeName";
                col.ReadOnly = true;
                col.HeaderText = "Содержимое";
                dgv.Columns.Add(col);
            }
        }

        private void dgvCorpStarbaseList_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvCorpStarbaseList.CurrentRow != null && dgvCorpStarbaseList.CurrentRow.Cells["starbaseDetail"].Value != null)
            {
                DataRowView drv = (DataRowView)dgvCorpStarbaseList.CurrentRow.DataBoundItem;
                int itemId = Convert.ToInt32(drv["itemId"]);
                mControlTowerId = itemId;
                int locationId = Convert.ToInt32(drv["locationId"]);
                ShowStructuresList(itemId, locationId);
                //string str = Convert.ToString(dgvCorpStarbaseList.CurrentRow.Cells["starbaseDetail"].Value);
                //int starbaseTypeId = Convert.ToInt32(dgvCorpStarbaseList.CurrentRow.Cells["typeId"].Value);
                //if (str != "")
                //{
                //    //XmlDocument xmlDoc = new XmlDocument();
                //    //xmlDoc.LoadXml(str);
                //    //mData.GetCorpStarbaseDetail(xmlDoc);
                //}
                //else
                //{
                //}
            }
        }
        private void dgvCorpStarbaseList_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (sender == dgvCorpStarbaseList)
            {
                DataGridView dgv = (DataGridView)sender;
                if (dgv.Columns[e.ColumnIndex].Name == "state" && !e.FormattingApplied)
                {
                    int state = Convert.ToInt32(e.Value);
                    switch (state)
                    {
                        case 1:
                            e.Value = "Anchored (?)";
                            break;
                        case 3:
                            e.Value = "Reinforced";
                            break;
                        case 4:
                            e.Value = "Online";
                            break;
                    }
                    e.FormattingApplied = true;
                }
            }
        }
        private void HideStructuresList()
        {
            if (mDVDefinedStructures != null)
            {
                dgvDefinedStructures.DataSource = null;
                mDVDefinedStructures.Dispose();
                mDVDefinedStructures = null;
            }
            if (mDVUndefinedStructures != null)
            {
                dgvUndefinedStructures.DataSource = null;
                mDVUndefinedStructures.Dispose();
                mDVUndefinedStructures = null;
            }
        }
        private void ShowStructuresList(int towerItemId, int locationId)
        {
            HideStructuresList();
            mDVDefinedStructures = new DataView(mData.TableCorpStarbaseStructures);
            mDVDefinedStructures.RowFilter = String.Format("locationId = {0} and parentId = {1}", locationId, towerItemId);
            mDVDefinedStructures.Sort = "typeName";
            dgvDefinedStructures.DataSource = mDVDefinedStructures;

            mDVUndefinedStructures = new DataView(mData.TableCorpStarbaseStructures);
            mDVUndefinedStructures.RowFilter = String.Format("locationId = {0} and parentId Is Null", locationId);
            mDVUndefinedStructures.Sort = "typeName";
            dgvUndefinedStructures.DataSource = mDVUndefinedStructures;

            CalcTowerInfo();
        }
        private void CalcTowerInfo()
        {
            if (dgvCorpStarbaseList.CurrentRow != null)
            {
                DataRowView drv = (DataRowView)dgvCorpStarbaseList.CurrentRow.DataBoundItem;
                int towerCpu = Convert.ToInt32(drv["cpu"]);
                int towerPower = Convert.ToInt32(drv["power"]);
                int usedCpu = 0, usedPower = 0;
                foreach (DataRowView drv1 in mDVDefinedStructures)
                {
                    bool online = Convert.ToBoolean(drv1["online"]);
                    if (online)
                    {
                        usedCpu += Convert.ToInt32(drv1["cpu"]);
                        usedPower += Convert.ToInt32(drv1["power"]);
                    }
                }
                pbTowerCPU.Value = 0;
                pbTowerCPU.Maximum = 100;
                pbTowerPower.Value = 0;
                pbTowerPower.Maximum = 100;

                if (towerCpu > usedCpu)
                {
                    pbTowerCPU.Maximum = towerCpu;
                    pbTowerCPU.Value = usedCpu;
                }
                lTowerCPU.Text = String.Format("{2:F1}%    {0}/{1} tf", usedCpu, towerCpu, 100.0 * (double)usedCpu / (double)towerCpu);

                if (towerPower > usedPower)
                {
                    pbTowerPower.Maximum = towerPower;
                    pbTowerPower.Value = usedPower;
                }
                lTowerPower.Text = String.Format("{2:F1}%    {0}/{1} MW", usedPower, towerPower, 100.0 * (double)usedPower / (double)towerPower);
            }
        }
        #region StringFunctionAsync
        void RunStringFunctionAsync(string value)
        {
            //подготовка, если требуется
            switch (value)
            {
                case "init structures list":
                    dgvCorpStarbaseList.ClearSelection();
                    HideStructuresList();
                    break;
            }
#if DEBUG
            timerStringAsyncDebugRun.Tag = value;
            timerStringAsyncDebugRun.Start();
#else
            StringFunctionAsyncDelegate f = this.StringFunctionAsync;
            f.BeginInvoke(value, new AsyncCallback(StringFunctionCompletedCallBack), this);
#endif
        }

        string StringFunctionAsync(string value)
        {
            string result = "";
            //инициализация или обновление списка структур
            if (value == "init structures list")
            {
                //список строк для удаления
                List<DataRow> delRows = new List<DataRow>();
                //выбрать список структур в системе
                mData.GetCorpStarbaseStructures(mOptions.CharacterId);
                foreach (DataRow row in mData.TableCorpStarbaseStructures.Rows)
                {
                    delRows.Add(row);
                }
                int i1 = 0;
                //перебор списка установленных башен посов
                foreach (DataRow rowTower in mData.TableCorpStarbaseListForView.Rows)
                {
                    mHostWindow.ShowStatus(i1++, mData.TableCorpStarbaseListForView.Rows.Count, String.Format("{0} - {1}", rowTower["moonName"], rowTower["typeName"]));
                    //получить id локации и башни
                    int towerItemId = Convert.ToInt32(rowTower["itemId"]);
                    int locationId = Convert.ToInt32(rowTower["locationId"]);
                    int towerTypeId = Convert.ToInt32(rowTower["typeId"]);

                    //выбрать предметы, имеющиеся в системе и являющиеся структурами
                    mData.GetAssetListForView_StructuresInLocation(mOptions.CharacterId, locationId);
                    //узнать число башен в этой системе
                    DataRow[] rows = mData.TableCorpStarbaseListForView.Select(String.Format("locationId = {0}", locationId));
                    bool multipleTowers = rows.Length > 1;
                    foreach (DataRow structure in mData.TableAssetListForView.Rows)
                    {
                        int itemId = Convert.ToInt32(structure["itemId"]);
                        //проверка, не является ли структура башней поса
                        string typeName = Convert.ToString(structure["typeName"]);
                        //если в названии нет Control Tower, это не башня
                        if (!typeName.Contains("Control Tower"))
                        {
                            DataRow row;
                            //проверка наличия данной структуры в списке
                            rows = mData.TableCorpStarbaseStructures.Select(String.Format("itemId = {0}", itemId));
                            if (rows.Length > 0)
                            {
                                row = rows[0];
                                delRows.Remove(row);
                            }
                            else
                                row = mData.TableCorpStarbaseStructures.NewRow();
                            int typeId = Convert.ToInt32(structure["typeId"]);
                            row["recordId"] = structure["recordId"];//id строки в таблице структур будет соответствовать таблице assets
                            row["itemId"] = itemId;
                            if (!multipleTowers)
                                row["parentId"] = towerItemId;
                            row["locationId"] = locationId;
                            row["typeId"] = typeId;
                            row["typeName"] = typeName;
                            //if (row["cpu"] is DBNull || row["power"] is DBNull)
                            {
                                int cpu, power;
                                mData.GetTypeCpuPower(typeId, out cpu, out power);
                                row["cpu"] = cpu;
                                row["power"] = power;
                            }
                            if (rows.Length == 0)
                                mData.TableCorpStarbaseStructures.Rows.Add(row);
                        }
                    }
                    //перебор структур для добавления информации об их содержимом
                    foreach (DataRow structure in mData.TableCorpStarbaseStructures.Rows)
                    {
                        mData.GetAssetListForView(mOptions.CharacterId, (Guid)structure["recordId"]);
                        rows = mData.TableAssetListForView.Select("flag = 0");
                        if (rows.Length == 0)
                        {
                            structure["containedTypeId"] = DBNull.Value;
                            structure["containedTypeName"] = DBNull.Value;
                        }
                        if (rows.Length == 1)
                        {
                            structure["containedTypeId"] = rows[0]["typeId"];
                            structure["containedTypeName"] = rows[0]["typeName"];
                        }
                        if (rows.Length > 1)
                        {
                            structure["containedTypeId"] = DBNull.Value;
                            StringBuilder sb = new StringBuilder();
                            foreach (DataRow row in rows)
                            {
                                if (sb.Length > 0)
                                    sb.Append(", ");
                                sb.Append(row["typeName"]);
                            }
                            structure["containedTypeName"] = sb.ToString();
                        }
                    }
                }
                foreach (DataRow row in delRows)
                {
                    row.Delete();
                }
                mData.CommitCorpStarbaseStructures();
                result = value;
            }
            if (result == "")
                mHostWindow.ShowStatus("Произошла ошибка");
            else
                mHostWindow.ShowStatus(100, "Завершено");
            return result;
        }
        void StringFunctionCompletedCallBack(IAsyncResult result)
        {
            try
            {
                AsyncResult r = (AsyncResult)result;
                StringFunctionAsyncDelegate command = (StringFunctionAsyncDelegate)r.AsyncDelegate;
                string val = command.EndInvoke(result);
                StringFunctionCompleted(val);
            }
            catch (Exception exc)
            {
                mMainControl.ProcessException(System.Reflection.MethodInfo.GetCurrentMethod(), exc);
            }
        }
        void StringFunctionCompleted(string value)
        {
            if (this.InvokeRequired)
            {
                this.Invoke((EventHandler)delegate
                {
                    StringFunctionCompleted(value);
                });
            }
            else
            {
                if (value.Contains("init structures list"))
                {
                    mData.GetCorpStarbaseStructures(mOptions.CharacterId);
                }
            }
        }
        void timerStringAsyncDebugRun_Tick(object sender, EventArgs e)
        {
            timerStringAsyncDebugRun.Stop();
            string value = (string)timerStringAsyncDebugRun.Tag;
            value = StringFunctionAsync(value);
            StringFunctionCompleted(value);
        }
        delegate string StringFunctionAsyncDelegate(string value);
        #endregion
        #region Drag and Drop
        private void dgvDefinedStructures_DragDrop(object sender, DragEventArgs e)
        {
            //return;
            try
            {
                List<DataRow> list = (List<DataRow>)e.Data.GetData(typeof(List<DataRow>));
                foreach (DataRow row in list)
                {
                    if (row["parentid"] is DBNull)
                    {
                        row["parentId"] = mControlTowerId;
                        mData.CommitCorpStarbaseStructures();
                        row.AcceptChanges();
                    }
                    else
                    {
                        row["parentId"] = DBNull.Value;
                        mData.CommitCorpStarbaseStructures();
                        row.AcceptChanges();
                    }
                }
                CalcTowerInfo();
            }
            catch (Exception exc)
            {
                mMainControl.ProcessException(System.Reflection.MethodInfo.GetCurrentMethod(), exc);
            }
        }

        private void dgvDefinedStructures_DragOver(object sender, DragEventArgs e)
        {
            try
            {
                List<DataRow> list = (List<DataRow>)e.Data.GetData(typeof(List<DataRow>));
                if (list.Count > 0)
                {
                    if (sender != mDGVFrom)
                        e.Effect = DragDropEffects.Move;
                    else
                        e.Effect = DragDropEffects.None;
                }
            }
            catch
            {
            }
        }

        private void dgvUndefinedStructures_MouseDown(object sender, MouseEventArgs e)
        {
            try
            {
                if (e.Button == MouseButtons.Left)
                {
                    DataGridView dgv = (DataGridView)sender;
                    if (dgv.SelectedRows.Count > 0)
                    {
                        List<DataRow> list = new List<DataRow>();
                        foreach (DataGridViewRow dgrv in dgv.SelectedRows)
                        {
                            DataRowView drv = (DataRowView)dgrv.DataBoundItem;
                            list.Add(drv.Row);
                        }
                        //dgv.ClearSelection();
                        //dgv.CurrentCell = null;
                        //dgvCorpStarbaseList.Focus();
                        mDGVFrom = dgv;
                        dgv.DoDragDrop(list, DragDropEffects.All | DragDropEffects.Move);
                        //dgv.DataSource = null;
                        //dgv.ClearSelection();
                        //dgv.CurrentCell = null;
                        //dgvCorpStarbaseList.Focus();
                        //if(dgv.Rows.Count == 0)
                        //    dgv.CurrentCell = dgv.
                    }
                }
            }
            catch
            {
            }
        }
        #endregion

        private void dgvUndefinedStructures_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                DataGridView dgv = (DataGridView)sender;
                if (e.ColumnIndex >= 0 && e.ColumnIndex < dgv.Columns.Count && e.RowIndex >= 0 && e.RowIndex < dgv.Rows.Count)
                {
                    if (dgv.Columns[e.ColumnIndex].DataPropertyName == "online")
                    {
                        DataRowView drv = (DataRowView)dgv.Rows[e.RowIndex].DataBoundItem;
                        if (drv.IsEdit)
                            drv.EndEdit();
                        mData.CommitCorpStarbaseStructures();
                        drv.Row.AcceptChanges();
                    }
                    CalcTowerInfo();
                }
            }
            catch (Exception exc)
            {
                mMainControl.ProcessException(System.Reflection.MethodInfo.GetCurrentMethod(), exc);
            }
        }
    }
}
