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
    public partial class CorpStarbaseList : UserControl
    {
        public CorpStarbaseList(IMainControl mainControl, IHostWindow hostWindow)
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

            //mTableFuel = new DataTable("TableFuel");
            //mTableFuel.Columns.Add("resourceTypeId", typeof(int));
            //mTableFuel.Columns.Add("resourceTypeName", typeof(string));
            //mTableFuel.Columns.Add("volume", typeof(Double));
            //mTableFuel.Columns.Add("quantityAtPeriod", typeof(Double));
            //mTableFuel.Columns.Add("quantity", typeof(Double));
            //mTableFuel.Columns["quantity"].DefaultValue = 0;
            ////mTableFuel.Columns.Add("purposeText", typeof(string));
            //mTableFuel.Columns.Add("fuelEnd", typeof(DateTime));
            //mTableFuel.Columns.Add("quantityAtCustomPeriod", typeof(Double));
            //mTableFuel.Columns.Add("volumeAtCustomPeriod", typeof(Double));
            mTableFuel = new DataTable("TableFuel");
            mTableFuel.Columns.Add("resourceTypeId", typeof(int));
            mTableFuel.Columns.Add("resourceTypeName", typeof(string));
            mTableFuel.Columns.Add("volume", typeof(Double));
            mTableFuel.Columns.Add("quantityAtPeriod", typeof(Double));
            mTableFuel.Columns.Add("quantity", typeof(Double));
            mTableFuel.Columns["quantity"].DefaultValue = 0;
            //mTableFuel.Columns.Add("purposeText", typeof(string));
            mTableFuel.Columns.Add("fuelTime", typeof(string));
            mTableFuel.Columns.Add("fuelEnd", typeof(DateTime));
            mTableFuel.Columns.Add("quantityAtCustomPeriod", typeof(Double));
            mTableFuel.Columns.Add("volumeAtCustomPeriod", typeof(Double));

            mData.GetCorpStarbaseList(mOptions.CharacterId);
            mData.GetCorpStarbaseListForView(mOptions.CharacterId);

            //PrepareData();
            //ShowData();

            //aCorpStarbaseList.onlineTimestamp, aCorpStarbaseList.starbaseDetail, aCorpStarbaseList.cpu, aCorpStarbaseList.power
            mTowers.Columns.Add("itemId", typeof(int));
            mTowers.Columns.Add("regionName", typeof(string));
            mTowers.Columns.Add("constellationName", typeof(string));
            mTowers.Columns.Add("locationId", typeof(int));
            mTowers.Columns.Add("locationName", typeof(string));
            mTowers.Columns.Add("moonId", typeof(int));
            mTowers.Columns.Add("moonName", typeof(string));
            mTowers.Columns.Add("typeId", typeof(int));
            mTowers.Columns.Add("typeName", typeof(string));
            mTowers.Columns.Add("state", typeof(int));
            mTowers.Columns.Add("stateTimeStamp", typeof(DateTime));
            mTowers.Columns.Add("onlineTimeStamp", typeof(DateTime));
            mTowers.Columns.Add("starbaseDetail", typeof(string));
            mTowers.Columns.Add("cpu", typeof(int));
            mTowers.Columns.Add("power", typeof(int));

            //dgvCorpMemberTracking.AutoGenerateColumns = false;
            //DataGridViewTextBoxColumn col1 = new DataGridViewTextBoxColumn();
            //col1.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            //col1.DataPropertyName = "regionName";
            //col1.HeaderText = "Регион";
            //dgvCorpMemberTracking.Columns.Add(col1);

            //col1 = new DataGridViewTextBoxColumn();
            //col1.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            //col1.DataPropertyName = "constelationName";
            //col1.HeaderText = "Созвездие";
            //dgvCorpMemberTracking.Columns.Add(col1);

            //col1 = new DataGridViewTextBoxColumn();
            //col1.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            //col1.DataPropertyName = "moonName";
            //col1.HeaderText = "Система - луна";
            //dgvCorpMemberTracking.Columns.Add(col1);

            //col1 = new DataGridViewTextBoxColumn();
            //col1.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            //col1.DataPropertyName = "stateToTime";
            //col1.HeaderText = "До";
            //dgvCorpMemberTracking.Columns.Add(col1);

            //col1 = new DataGridViewTextBoxColumn();
            //col1.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            //col1.DataPropertyName = "stateTimeLeft";
            //col1.HeaderText = "Осталось";
            //dgvCorpMemberTracking.Columns.Add(col1);

            FillFilters();
            //StartDisplayData();
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
        List<StarbaseInfo> mStarbaseInfoList = new List<StarbaseInfo>();
        List<DataRowView> mDRVTowers = new List<DataRowView>();
        StarbaseInfo mStarbaseInfo = new StarbaseInfo();
        int mStarbaseInfoCount;
        DataTable mTowers = new DataTable("towers");
        #endregion
        #endregion

        void RunFunctionAsync(FunctionCompeletedEventArgs args)
        {
            //подготовка, если требуется
            switch (args.Function)
            {
                case ApiFunction.CorpStarbaseList:
                    break;
                case ApiFunction.CorpStarbaseDetail:
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
                        mHostWindow.ShowStatus(90, "Подтверждение данных отложено до загрузки подробностей");
                        //mData.CommitCorpStarbaseList();
                        //mHostWindow.ShowStatus(100, "Список ПОСов обновлён.");

                        result = args.Function;
                        break;
                    }
                #endregion
                #region ApiFunction.CorpStarbaseDetail
                case ApiFunction.CorpStarbaseDetail:
                    {
                        //mHostWindow.ShowStatus(0, "Обработка данных...");
                        //XmlNode nodeCorpStarbaseDetail = nodeRoot.SelectSingleNode("descendant::rowset[@name='starbases']");
                        string starbaseDetail = args.XmlResponse.InnerXml;//nodeRoot.InnerXml;
                        int itemId = mSession.StarbaseId;
                        DataRow[] rows = mData.TableCorpStarbaseList.Select(String.Format("itemId = {0}", itemId));
                        if (rows.Length == 1)
                        {
                            rows[0]["starbaseDetail"] = starbaseDetail;
                            mHostWindow.ShowStatus(String.Format("Starbase ID {0}...", itemId));
                            mData.CommitCorpStarbaseList();
                        }
                        //mHostWindow.ShowStatus(String.Format("Starbase ID {0}, запись обновлена", itemId));
                        if (mSession.CommandQueue.Count == 0)
                        {
                            mHostWindow.ShowStatus(90, "Подтверждение данных...");
                            mData.CommitCorpStarbaseList();
                            mHostWindow.ShowStatus(100, "Список ПОСов обновлён");
                        }
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
                            break;
                        }
                    #endregion
                    #region ApiFunction.CorpStarbaseDetail
                    case ApiFunction.CorpStarbaseDetail:
                        {
                            break;
                        }
                    #endregion
                }
                if (mSession.CommandQueue.Count > 0)
                    mSession.ExecuteCommandFromQueue();
                else
                {
                    FillFilters();
                    //StartDisplayData();
                }
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

        private void BeforeDestroy()
        {
            mSession.FunctionCompleted -= new FunctionCompletedHandler(mSession_FunctionCompleted);
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
            if (mDRVTowers.Count == 0)
            {
                if (sender == bUpdate)
                {
                    mSession.GetFunctionAsync(ApiFunction.CorpStarbaseList);
                    //XmlDocument xmlDoc = new XmlDocument();
                    //xmlDoc.Load("C:\\Users\\Rius\\AppData\\Roaming\\Blind Octopus\\Accounting\\1.0.0.0\\download\\CorpStarbaseList\\2008.01.15 - 00.18.46.xml");
                    //RunFunctionAsync(new FunctionCompeletedEventArgs(ApiFunction.CorpStarbaseList, xmlDoc, ""));
                }
                if (sender == bStarbaseConfig)
                {
                    mMainControl.SwitchToInterfaceState(InterfaceState.CorpStarbaseListConfig, true);
                }
                if (sender == bSelectFilter)
                {
                    StartDisplayData();
                }
                if (sender == cbRegion)
                {
                    cbConstellation.Items.Clear();
                    cbConstellation.Items.Add("*");

                    cbSolarSystem.Items.Clear();
                    //cbSolarSystem.Items.Add("*");

                    if (cbRegion.SelectedIndex > 0)
                    {
                        foreach (DataRow row in mData.TableCorpStarbaseListForView.Rows)
                        {
                            if (row["regionName"].ToString() == cbRegion.SelectedItem.ToString())
                            {
                                if (!cbConstellation.Items.Contains(row["constellationName"]))
                                    cbConstellation.Items.Add(row["constellationName"]);
                            }
                        }
                        cbConstellation.SelectedIndex = 0;
                    }
                }
                if (sender == cbConstellation)
                {
                    cbSolarSystem.Items.Clear();
                    cbSolarSystem.Items.Add("*");

                    if (cbConstellation.SelectedIndex > 0)
                    {
                        foreach (DataRow row in mData.TableCorpStarbaseListForView.Rows)
                        {
                            if (row["constellationName"].ToString() == cbConstellation.SelectedItem.ToString())
                            {
                                if (!cbSolarSystem.Items.Contains(row["locationName"]))
                                    cbSolarSystem.Items.Add(row["locationName"]);
                            }
                        }
                        cbSolarSystem.SelectedIndex = 0;
                    }
                }
            }
        }

        private void SetupDataGridView(DataGridView dgv)
        {
        }

        private TimeSpan CalcFuelEndTime(int towerTypeId, string starbaseDetail, int state, DateTime stateTimeStamp, double cpu, double power, string fractionName, double security, double fuelSovMod)
        {
            TimeSpan result = TimeSpan.MaxValue;
            try
            {
                DateTime nearestTimeStamp = DateTime.UtcNow;
                //подгонка текущего времени к времени кормления поса
                TimeSpan correct = nearestTimeStamp.Subtract(stateTimeStamp);
                correct = new TimeSpan(0, correct.Minutes, correct.Seconds);
                DateTime dt3 = nearestTimeStamp.Subtract(correct);
                if (dt3 > nearestTimeStamp)
                    dt3 = dt3.AddHours(-1);
                nearestTimeStamp = dt3;

                //заполнение информации о ресурсах башни
                mData.GetControlTowerResources(towerTypeId);
                //заполнение таблицы топлива инфой о запросах башни
                mTableFuel.Rows.Clear();
                foreach (DataRow row in mData.TableControlTowerResources.Rows)
                {
                    DataRow newRow = mTableFuel.NewRow();
                    newRow["resourceTypeId"] = row["resourceTypeId"];
                    newRow["resourceTypeName"] = row["typeName"];
                    newRow["quantityAtPeriod"] = row["quantity"];
                    //newRow["purposeText"] = row["purposeText"];
                    newRow["volume"] = row["volume"];
                    mTableFuel.Rows.Add(newRow);
                }
                //разница между временем регистрации состояния поса и текущим
                TimeSpan span = nearestTimeStamp.Subtract(stateTimeStamp);
                double hours = Math.Ceiling(span.TotalHours);
                foreach (DataRow row in mTableFuel.Rows)
                {
                    //получение количества имеющегося ресурса из загруженных с сервера данных
                    DataRow[] row1 = mData.TableCorpStarbaseDetailFuel.Select(String.Format("typeId = {0}", row["resourceTypeID"]));
                    //если такой ресурс там есть, то запоминаем его количество, если нет, оно равно 0
                    if (row1.Length > 0)
                        row["quantity"] = row1[0]["quantity"];
                    //if (!(row["quantity"] is DBNull || row["quantityAtPeriod"] is DBNull))
                    row["fuelEnd"] = nearestTimeStamp;
                    {
                        string resTypeName = Convert.ToString(row["resourceTypeName"]);
                        double quantity = Convert.ToDouble(row["quantity"]);
                        double quantityAtPeriod = Convert.ToDouble(row["quantityAtPeriod"]);
                        //если это разрешение (Starbase Charter)
                        if (resTypeName.Contains("Charter"))
                        {
                            if (!resTypeName.Contains(fractionName) || security < 0.4)
                                quantityAtPeriod = 0;
                        }
                        //зависимость потребления от клайма
                        quantityAtPeriod *= fuelSovMod;
                        //изменение потребления озона в зависимости от загрузки power grid
                        if (resTypeName.Contains("Liquid Ozone"))
                            quantityAtPeriod *= power;
                        //изменение потребления воды в зависимости от загрузки cpu
                        if (resTypeName.Contains("Heavy Water"))
                            quantityAtPeriod *= cpu;
                        //DateTime dtEnd = mData.StarbaseDetail.CurrentTime.AddHours(quantity / quantityAtPeriod);
                        //row["fuelEnd"] = dtEnd;

                        //надо вычислить, сколько топлива осталось на текущий момент)
                        quantityAtPeriod = Math.Ceiling(quantityAtPeriod);
                        quantity -= quantityAtPeriod * hours;
                        row["quantity"] = quantity;
                        row["quantityAtPeriod"] = quantityAtPeriod;
                        DateTime dt;
                        if (state == 4)
                        {
                            if (quantityAtPeriod > 0)
                                dt = nearestTimeStamp.AddHours(Math.Truncate(quantity / quantityAtPeriod));
                            else
                                dt = DateTime.MaxValue;
                        }
                        else
                        {
                            dt = stateTimeStamp;
                        }
                        //вычисление значения меньшего математического fuelEnd, но совпадающего по шагу часов с stateTimeStamp
                        {
                            correct = dt.Subtract(stateTimeStamp);
                            correct = new TimeSpan(0, correct.Minutes, correct.Seconds);
                            dt3 = dt.Subtract(correct);
                            if (dt3 > dt)
                                dt3 = dt3.AddHours(-1);
                            dt = dt3;
                        }
                        row["fuelEnd"] = dt;

                        TimeSpan ts = dt.Subtract(nearestTimeStamp);
                        if (ts.Days < 10000)
                            row["fuelTime"] = String.Format("{0}д {1}ч {2}м", ts.Days, ts.Hours, ts.Minutes);
                        else
                            row["fuelTime"] = "∞";
                        if (!resTypeName.Contains("Strontium"))
                        {
                            if (ts < result)
                                result = ts;
                        }
                    }
                }
            }
            catch (Exception exc)
            {
            }
            return result;
        }

        private void FillFilters()
        {
            mData.GetCorpStarbaseListForView(mOptions.CharacterId);
            cbRegion.Items.Clear();

            cbRegion.Items.Add("*");
            foreach (DataRow row in mData.TableCorpStarbaseListForView.Rows)
            {
                if (!cbRegion.Items.Contains(row["regionName"]))
                    cbRegion.Items.Add(row["regionName"]);
            }
            cbRegion.SelectedIndex = 0;
        }
        private void StartDisplayData()
        {
            mDRVTowers.Clear();
            flpStarbases.Controls.Clear();
            //mData.GetCorpStarbaseListForView(mOptions.CharacterId);
            string filter = "";
            if (cbRegion.SelectedIndex > 0)
                filter = String.Format("regionName = '{0}'", cbRegion.SelectedItem);
            if (cbConstellation.SelectedIndex > 0)
            {
                if (filter != "") filter += " and ";
                filter = String.Format("constellationName = '{0}'", cbConstellation.SelectedItem);
            }
            if (cbSolarSystem.SelectedIndex > 0)
            {
                if (filter != "") filter += " and ";
                filter = String.Format("locationName = '{0}'", cbSolarSystem.SelectedItem);
            }
            DataView dv = new DataView(mData.TableCorpStarbaseListForView);
            dv.RowFilter = filter;
            dv.Sort = "moonName";
            foreach (DataRowView tower in dv)
            {
                mDRVTowers.Add(tower);
                //mTowers.ImportRow(tower.Row);
            }
            mStarbaseInfoCount = mDRVTowers.Count;
            RunStringFunctionAsync("display pos list");
            //foreach(Datarow
            //dgvCorpMemberTracking.DataSource = mTowers;
        }

        #region StringFunctionAsync
        void RunStringFunctionAsync(string value)
        {
            //подготовка, если требуется
            switch (value)
            {
                case "display pos list":
                    if (mDRVTowers.Count == 0)
                    {
                        mHostWindow.ShowStatus(100, "Завершено");
                        return;
                    }
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
            if (value == "display pos list")
            {
                DataRowView tower = mDRVTowers[0];
                string str = Convert.ToString(tower["starbaseDetail"]);
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(str);
                mData.GetCorpStarbaseDetail(xmlDoc);

                DateTime nearestTimeStamp = DateTime.UtcNow;
                //подгонка текущего времени к времени кормления поса
                DateTime stateTimeStamp = Convert.ToDateTime(tower["stateTimeStamp"]);
                TimeSpan correct = nearestTimeStamp.Subtract(stateTimeStamp);
                correct = new TimeSpan(0, correct.Minutes, correct.Seconds);
                DateTime dt3 = nearestTimeStamp.Subtract(correct);
                if (dt3 > nearestTimeStamp)
                    dt3 = dt3.AddHours(-1);
                nearestTimeStamp = dt3;


                mStarbaseInfo = new StarbaseInfo();
                mStarbaseInfo.MoonName = Convert.ToString(tower["moonName"]);
                mHostWindow.ShowStatus(mStarbaseInfoCount - mDRVTowers.Count, mStarbaseInfoCount, mStarbaseInfo.MoonName);
                mStarbaseInfo.Description = Convert.ToString(tower["typeName"]);
                mStarbaseInfo.TowerItemId = Convert.ToInt32(tower["itemId"]);
                int typeId = Convert.ToInt32(tower["typeId"]);
                Image imageTower = mData.GetInvTypeImage(typeId, ImageSize.x64, mOptions.LoadImagesFromWeb);
                mStarbaseInfo.TowerImage = imageTower;

                //получение инфы о башне поса
                int towerCpu = Convert.ToInt32(tower["cpu"]);
                int towerPower = Convert.ToInt32(tower["power"]);
                int locationId = Convert.ToInt32(tower["locationId"]);
                int towerItemId = Convert.ToInt32(tower["itemId"]);
                //info.Description += towerCpu.ToString() + " " + towerPower.ToString();
                //получение инфы о структурах поса
                int totalCpu = 0, totalPower = 0;
                mData.GetCorpStarbaseStructures(mOptions.CharacterId);
                DataView dvDefinedStructures = new DataView(mData.TableCorpStarbaseStructures);
                dvDefinedStructures.RowFilter = String.Format("locationId = {0} and parentId = {1}", locationId, towerItemId);
                dvDefinedStructures.Sort = "typeName";
                int structuresOnline = 0, structuresAnchored = dvDefinedStructures.Count;
                foreach (DataRowView structure in dvDefinedStructures)
                {
                    bool structureOnline = Convert.ToBoolean(structure["online"]);
                    if (structureOnline)
                    {
                        totalCpu += Convert.ToInt32(structure["cpu"]);
                        totalPower += Convert.ToInt32(structure["power"]);
                        structuresOnline++;
                    }
                }
                double cpu = (double)totalCpu / (double)towerCpu;
                double power = (double)totalPower / (double)towerPower;
                if (structuresOnline == 0)
                {
                    cpu = power = 1;
                }
                int state = Convert.ToInt32(tower["state"]);
                TimeSpan tsToFuelEnd = TimeSpan.Zero;
                DateTime dtFuelEnd = DateTime.UtcNow;
                if (state == 4 || state == 1)
                {
                    double security;
                    string fractionName;
                    string allianeName;
                    int sovereigntyLevel;
                    mData.GetSystemInfo(locationId, out fractionName, out security, out sovereigntyLevel, out allianeName);
                    mData.GetCorporationSheet(mOptions.CharacterId);
                    double fuelSovMod = 1;
                    if (allianeName == mData.CorporationSheetInfo.AllianceName)
                    {
                        if (sovereigntyLevel >= 1)
                            fuelSovMod = 0.75;
                        if (sovereigntyLevel == 4)
                            fuelSovMod = 0.7;
                    }

                    tsToFuelEnd = CalcFuelEndTime(typeId, Convert.ToString(tower["starbaseDetail"]), state, stateTimeStamp, cpu, power, fractionName, security, fuelSovMod);
                    //dtFuelEnd = DateTime.UtcNow.Add(tsToFuelEnd);
                    dtFuelEnd = nearestTimeStamp.Add(tsToFuelEnd);
                }
                if (state == 3)
                {
                    dtFuelEnd = stateTimeStamp;
                    tsToFuelEnd = dtFuelEnd.Subtract(DateTime.UtcNow);
                }
                //info.Description += String.Format("\nCPU: {0}/{1}\nPower: {2}/{3}", totalCpu, towerCpu, totalPower, towerPower);
                switch (Convert.ToInt32(tower["state"]))
                {
                    case 3:
                        mStarbaseInfo.Description += String.Format("\nReinforced до {0:dd MMM- HH:mm} ({1}д {2}ч)", dtFuelEnd, tsToFuelEnd.Days, tsToFuelEnd.Hours);
                        break;
                    case 4:
                        mStarbaseInfo.Description += String.Format("\nOnline до {0:dd MMM - HH:mm} ({1}д {2}ч)", dtFuelEnd, tsToFuelEnd.Days, tsToFuelEnd.Hours);
                        mStarbaseInfo.Description += String.Format("\nСтруктуры anchored - {0}, online: {1}", structuresAnchored, structuresOnline);
                        mStarbaseInfo.Description += String.Format("\nCPU: {0:F1}%, Power: {1:F1}%", 100.0 * cpu, 100.0 * power);
                        break;
                    case 1:
                        mStarbaseInfo.Description += String.Format("\nOffline. Запасов на {0}д {0}ч", tsToFuelEnd.Days, tsToFuelEnd.Hours);
                        mStarbaseInfo.Description += String.Format("\nСтруктуры anchored - {0}", structuresAnchored);
                        mStarbaseInfo.Description += String.Format("\nCPU: {0:F1}%, Power: {1:F1}%", 100.0 * cpu, 100.0 * power);
                        break;
                }
                mDRVTowers.Remove(tower);
                result = value;
            }
            if (result == "")
                mHostWindow.ShowStatus("Произошла ошибка");
            //else
            //    mHostWindow.ShowStatus(100, "Завершено");
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
                if (value.Contains("display pos list"))
                {
                    {
                        StartPageMenuCommandItem cmdItem = new StartPageMenuCommandItem(
                            mStarbaseInfo.TowerImage,
                            mStarbaseInfo.MoonName,
                            //"Gallente Control Tower\nСтатус: Online до 15 янв - 13:43\nСтруктуры: 31 online, 2 anchored",
                            mStarbaseInfo.Description,
                            mStarbaseInfo.TowerItemId.ToString());
                        cmdItem.Height = 90;
                        cmdItem.Width = 300;
                        cmdItem.Click += new EventHandler(StarbaseClick);
                        flpStarbases.Controls.Add(cmdItem);
                    }
                    RunStringFunctionAsync("display pos list");
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

        private void StarbaseClick(object sender, EventArgs e)
        {
            if (mDRVTowers.Count == 0)
            {
                IStartPageMenuItem item = (IStartPageMenuItem)sender;
                mMainControl.SwitchToInterfaceState(InterfaceState.CorpStarbaseListDetails, true, item.Command);
            }
        }
    }
    internal struct StarbaseInfo
    {
        public string MoonName;
        public string Description;
        public Image TowerImage;
        public int TowerItemId;
    }
}
