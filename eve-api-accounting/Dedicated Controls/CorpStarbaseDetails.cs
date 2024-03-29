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
    public partial class CorpStarbaseDetails : UserControl
    {
        public CorpStarbaseDetails(IMainControl mainControl, IHostWindow hostWindow, object[] data)
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
            mTableFuel.Columns.Add("priceOne", typeof(Double));
            mTableFuel.Columns.Add("priceAtCustomPeriod", typeof(Double));

            mData.GetCorpStarbaseList(mOptions.CharacterId);
            mData.GetCorpStarbaseListForView(mOptions.CharacterId);
            mData.GetControlTowerFuelPrices(mOptions.CharacterId);
            //mData.GetInvTypes();
            //dtpCustomToPeriod.Value = new DateTime(2008, 1, 
            //dgvCorpStarbaseList.DataSource = mData.TableCorpStarbaseList;
            dtpCustomToDate.Value = DateTimePicker.MinimumDateTime;
            dgvCorpStarbaseDetailFuel.SelectionChanged += new EventHandler(mHostWindow.ShowGridPosition);

            DataRow[] rows = mData.TableCorpStarbaseListForView.Select(String.Format("itemId = {0}", data[0]));
            if (rows.Length == 1)
                mRowSelectedTower = rows[0];
            ShowDataForSelectedPos();
        }
        #region variables
        #region простые переменные
        private IMainControl mMainControl;
        private IHostWindow mHostWindow;
        private Options mOptions;
        private Session mSession;
        private DataClass mData;
        System.Windows.Forms.Timer timerAsyncDebugRun = new System.Windows.Forms.Timer();
        #endregion

        #region private
        DataTable mTableFuel;
        DateTime mCustomDate = DateTime.MinValue;
        DataRow mRowSelectedTower;
        #endregion
        #endregion

        void RunFunctionAsync(FunctionCompeletedEventArgs args)
        {
            //подготовка, если требуется
            switch (args.Function)
            {
                case ApiFunction.CorpStarbaseList:
                    dgvCorpStarbaseDetailFuel.DataSource = null;
                    break;
                case ApiFunction.CorpStarbaseDetail:
                    dgvCorpStarbaseDetailFuel.DataSource = null;
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
                        mHostWindow.ShowStatus(0, "Обработка данных...");
                        XmlNode nodeCorpStarbaseList = nodeRoot.SelectSingleNode("descendant::rowset[@name='starbases']");
                        DataTable dtCorpStarbaseList = DataClass.ParseRowset(nodeCorpStarbaseList);

                        //удаление существующих строк
                        foreach (DataRow row in mData.TableCorpStarbaseList.Rows)
                        {
                            row.Delete();
                        }
                        mData.CommitCorpStarbaseList();
                        //data.TableAlliances.Clear();
                        int totalRows = dtCorpStarbaseList.Rows.Count;
                        foreach (DataRow starbase in dtCorpStarbaseList.Rows)
                        {
                            DataRow newStarbase = mData.TableCorpStarbaseList.NewRow();
                            if (mData.TableCorpStarbaseList.Rows.Count > totalRows)
                                totalRows = mData.TableCorpStarbaseList.Rows.Count;
                            mHostWindow.ShowStatus(
                                mData.TableCorpStarbaseList.Rows.Count,
                                totalRows,
                                String.Format("{2}, Добавление: {0}/{1}...", mData.TableCorpStarbaseList.Rows.Count, totalRows, args.Function));
                            for (int i = 0; i < dtCorpStarbaseList.Columns.Count; i++)
                            {
                                if (mData.TableCorpStarbaseList.Columns.Contains(dtCorpStarbaseList.Columns[i].ColumnName))
                                {
                                    newStarbase[dtCorpStarbaseList.Columns[i].ColumnName] = starbase[dtCorpStarbaseList.Columns[i].ColumnName];
                                }
                            }
                            mData.TableCorpStarbaseList.Rows.Add(newStarbase);
                            Thread.Sleep(10);
                            mSession.CommandQueue.Enqueue(new Command(ApiFunction.CorpStarbaseDetail, mOptions.UserId, mOptions.ApiKey, mOptions.CharacterId, Convert.ToInt32(newStarbase["itemId"])));
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

        private void BeforeDestroy()
        {
            mSession.FunctionCompleted -= new FunctionCompletedHandler(mSession_FunctionCompleted);
            dgvCorpStarbaseDetailFuel.SelectionChanged -= new EventHandler(mHostWindow.ShowGridPosition);
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
                //mSession.GetFunctionAsync(ApiFunction.CorpStarbaseList);
                ShowDataForSelectedPos();
                //XmlDocument xmlDoc = new XmlDocument();
                //xmlDoc.Load("C:\\Users\\Rius\\AppData\\Roaming\\Blind Octopus\\Accounting\\1.0.0.0\\download\\CorpStarbaseList\\2007.12.26 - 23.04.53.xml");
                //RunFunctionAsync(new FunctionCompeletedEventArgs(ApiFunction.CorpStarbaseList, xmlDoc, ""));
            }
            if (sender == dtpCustomToDate)
            {
                mCustomDate = dtpCustomToDate.Value;
                CalcCustomPeriod();
            }
            if (sender == rbCalcFuelToTime)
            {
                dtpCustomToDate.Show();
                numCustomToPeriodDays.Hide();
                numCustomToPeriodHours.Hide();
                lTimePeriod.Text = "Дата дозаправки:";
                CalcCustomPeriod();
            }
            if (sender == rbCalcFuelToPeriod)
            {
                dtpCustomToDate.Hide();
                numCustomToPeriodDays.Show();
                numCustomToPeriodHours.Show();
                lTimePeriod.Text = "Период, дни и часы:";
                CalcCustomPeriod();
            }
            if (sender == numCustomToPeriodDays || sender == numCustomToPeriodHours)
            {
                CalcCustomPeriod();
            }
            if (sender == bStarbaseConfig)
            {
                mMainControl.SwitchToInterfaceState(InterfaceState.CorpStarbaseListConfig, true);
            }
            if (sender == bFuelPrices)
            {
                mMainControl.SwitchToInterfaceState(InterfaceState.ControlTowerFuelPrices, true);
            }
        }

        private void SetupDataGridView(DataGridView dgv)
        {
            //if (dgv == dgvCorpStarbaseList)
            //{
            //    mData.GetCorpStarbaseListForView(mOptions.CharacterId);
            //    dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            //    dgv.DataSource = mData.DataSet;
            //    dgv.DataMember = "aCorpStarbaseListForView";
            //    //mData.FormatDataGridView(dgv);

            //    //dgv.Columns["recordId"].Visible = false;
            //    //dgv.Columns["userId"].Visible = false;
            //    dgv.Columns["moonId"].Visible = false;
            //    dgv.Columns["typeId"].Visible = false;
            //    dgv.Columns["locationId"].Visible = false;
            //    dgv.Columns["starbaseDetail"].Visible = false;
            //    dgv.Columns["locationName"].HeaderText = "Система";
            //    dgv.Columns["moonName"].HeaderText = "Луна";
            //    dgv.Columns["typeName"].HeaderText = "Башня";
            //    dgv.Columns["typeName"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            //}
            if (dgv == dgvCorpStarbaseDetailFuel)
            {
                //dgv.Columns["resourceTypeId"].Visible = false;
                mData.FormatDataGridView(dgv);
                dgv.RowHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.False;
                dgv.Columns["resourceTypeId"].Visible = false;
                dgv.Columns["priceOne"].Visible = false;
                dgv.Columns["resourceTypeName"].HeaderText = "Ресурс";
                dgv.Columns["resourceTypeName"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                dgv.Columns["resourceTypeName"].DisplayIndex = 0;
                //dgv.Columns["resourceTypeName"].Frozen = true;

                dgv.Columns["quantity"].HeaderText = "Кол-во";
                dgv.Columns["quantity"].DefaultCellStyle.Format = "#,##0";
                dgv.Columns["quantity"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

                dgv.Columns["volume"].HeaderText = "V, м³";
                dgv.Columns["volume"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;

                dgv.Columns["quantityAtPeriod"].HeaderText = "Потр. в час";
                dgv.Columns["quantityAtPeriod"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                dgv.Columns["quantityAtPeriod"].DefaultCellStyle.Format = "#,##0.0";
                dgv.Columns["quantityAtPeriod"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

                //dgv.Columns["purposeText"].HeaderText = "Режим";
                dgv.Columns["fuelEnd"].HeaderText = "До";
                dgv.Columns["fuelEnd"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;

                dgv.Columns["fuelTime"].HeaderText = "Осталось";
                dgv.Columns["fuelTime"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;

                dgv.Columns["quantityAtCustomPeriod"].HeaderText = "Кол-во расч.";
                dgv.Columns["quantityAtCustomPeriod"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                dgv.Columns["quantityAtCustomPeriod"].DefaultCellStyle.Format = "#,##0";
                dgv.Columns["quantityAtCustomPeriod"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

                dgv.Columns["volumeAtCustomPeriod"].HeaderText = "V расч., м³";
                dgv.Columns["volumeAtCustomPeriod"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                dgv.Columns["volumeAtCustomPeriod"].DefaultCellStyle.Format = "#,##0.00";
                dgv.Columns["volumeAtCustomPeriod"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

                dgv.Columns["priceAtCustomPeriod"].HeaderText = "Цена, ISK";
                dgv.Columns["priceAtCustomPeriod"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                dgv.Columns["priceAtCustomPeriod"].DefaultCellStyle.Format = "#,##0.00";
                dgv.Columns["priceAtCustomPeriod"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
        }

        private void ShowDataForSelectedPos()
        {
            if (mRowSelectedTower != null && !(mRowSelectedTower["starbaseDetail"] is DBNull))
            {
                int locationId = Convert.ToInt32(mRowSelectedTower["locationId"]);
                string str = Convert.ToString(mRowSelectedTower["starbaseDetail"]);
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(str);
                mData.GetCorpStarbaseDetail(xmlDoc);
                int starbaseTypeId = Convert.ToInt32(mRowSelectedTower["typeId"]);
                DateTime stateTimeStamp = Convert.ToDateTime(mRowSelectedTower["stateTimeStamp"]);

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

                lTowerInfo1.Text = String.Format(
                    "Система: {0} ({1}), [{2}]\n" +
                    "Луна: {3}\n" +
                    "Тип: {4}",
                    mRowSelectedTower["locationName"], security, fractionName,
                    mRowSelectedTower["moonName"],
                    mRowSelectedTower["typeName"]);

                pbImage.Image = mData.GetInvTypeImage(starbaseTypeId, ImageSize.x64, true);

                //dgvCorpStarbaseDetailFuel.DataSource = mData.TableCorpStarbaseDetailFuel;
                //SetupDataGridView(dgvCorpStarbaseDetailFuel);
                lStarbaseDetailInfo.Text = String.Format(
                    "Данные получены: {0}\nДанные можно обновить после: {1}\nGeneral settings:\n" +
                    "    DeployFlags: {2}\n    UsageFlags: {3}\n    Доступ сокорповцам: {4}\n    Доступ со-альянсовцам: {5}\n    ClaimSovereignty: {6}\n" +
                    "Combat settings:\n    Атаковать при стендинге менее {8}: {7}\n    Атаковать при SS менее {10}: {9}\n" +
                    "    Атаковать корпы, с которыми оффвар: {11}\n    Атаковать при агрессии: {12}\nStateTimeStamp: {13}",
                    mData.StarbaseDetail.CurrentTime,
                    mData.StarbaseDetail.CachedUntil,
                    mData.StarbaseDetail.GeneralSettings.DeployFlags,
                    mData.StarbaseDetail.GeneralSettings.UsageFlags,
                    mData.StarbaseDetail.GeneralSettings.AllowCorporationMembers,
                    mData.StarbaseDetail.GeneralSettings.AllowAllianceMembers,
                    mData.StarbaseDetail.GeneralSettings.ClaimSovereignty,
                    mData.StarbaseDetail.CombatSettings.OnStandingDropEnabled,
                    (double)mData.StarbaseDetail.CombatSettings.OnStandingDropValue / 10.0,
                    mData.StarbaseDetail.CombatSettings.OnStatusDropEnabled,
                    (double)mData.StarbaseDetail.CombatSettings.OnStatusDropValue / 10.0,
                    mData.StarbaseDetail.CombatSettings.OnCorporationWarEnabled,
                    mData.StarbaseDetail.CombatSettings.OnAggressionEnabled,
                    stateTimeStamp);

                //получение инфы о башне поса
                int towerCpu = Convert.ToInt32(mRowSelectedTower["cpu"]);
                int towerPower = Convert.ToInt32(mRowSelectedTower["power"]);
                int towerItemId = Convert.ToInt32(mRowSelectedTower["itemId"]);
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
                int state = Convert.ToInt32(mRowSelectedTower["state"]);
                TimeSpan tsToFuelEnd = TimeSpan.Zero;
                DateTime dtFuelEnd = DateTime.UtcNow;

                dtpCustomToDate.CustomFormat = String.Format("dd MMMM yyyy - HH:{0:D2}", stateTimeStamp.Minute);
                //dtpCustomDate.Value = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day,
                //    DateTime.UtcNow.Hour, stateTimeStamp.Minute, 0);

                if (state == 4 || state == 1)
                {
                    tsToFuelEnd = CalcFuelEndTime(starbaseTypeId, Convert.ToString(mRowSelectedTower["starbaseDetail"]), state, stateTimeStamp, cpu, power, fractionName, security, fuelSovMod);
                    dtFuelEnd = DateTime.UtcNow.Add(tsToFuelEnd);
                    if (dtpCustomToDate.Value == DateTimePicker.MinimumDateTime)
                        dtpCustomToDate.Value = dtFuelEnd.AddDays(12);
                }
                if (state == 3)
                {
                    dtFuelEnd = stateTimeStamp;
                    tsToFuelEnd = dtFuelEnd.Subtract(DateTime.UtcNow);
                }
                pbCpuLevel.Value = 0;
                pbPowerLevel.Value = 0;
                //lTowerInfo1.Text += String.Format("\nCPU: {0}/{1}\nPower: {2}/{3}", totalCpu, towerCpu, totalPower, towerPower);
                switch (state)
                {
                    case 3:
                        lTowerInfo1.Text += String.Format("\nReinforced до {0:dd MMMM - HH:mm} (осталось {1}д {2}ч)", dtFuelEnd, tsToFuelEnd.Days, tsToFuelEnd.Hours);
                        break;
                    case 4:
                        lTowerInfo1.Text += String.Format("\nOnline до {0:dd MMMM - HH:mm} (осталось {1}д {2}ч)", dtFuelEnd, tsToFuelEnd.Days, tsToFuelEnd.Hours);
                        lTowerInfo1.Text += String.Format("\nСтруктуры: anchored - {0}, online: {1}", structuresAnchored, structuresOnline);
                        lCPU.Text = String.Format("{0:F1}%,  {1}/{2} tf", 100.0 * cpu, totalCpu, towerCpu);
                        lPower.Text = String.Format("{0:F1}%,  {1}/{2} MW", 100.0 * power, totalPower, towerPower);
                        //lTowerInfo1.Text += String.Format("\nCPU: {0:F1}%, Power: {1:F1}%", 100.0 * cpu, 100.0 * power);
                        pbCpuLevel.Maximum = towerCpu;
                        pbPowerLevel.Maximum = towerPower;
                        pbCpuLevel.Value = totalCpu;
                        pbPowerLevel.Value = totalPower;
                        break;
                    case 1:
                        lTowerInfo1.Text += String.Format("\nOffline. Запасов на {0}д {0}ч", tsToFuelEnd.Days, tsToFuelEnd.Hours);
                        lTowerInfo1.Text += String.Format("\nСтруктуры anchored - {0}", structuresAnchored);
                        lCPU.Text = String.Format("{0:F1}%,  {1}/{2} tf", 100.0 * cpu, totalCpu, towerCpu);
                        lPower.Text = String.Format("{0:F1}%,  {1}/{2} MW", 100.0 * power, totalPower, towerPower);
                        pbCpuLevel.Maximum = towerCpu;
                        pbPowerLevel.Maximum = towerPower;
                        pbCpuLevel.Value = totalCpu;
                        pbPowerLevel.Value = totalPower;
                        break;
                }

                dgvCorpStarbaseDetailFuel.DataSource = mTableFuel;
                SetupDataGridView(dgvCorpStarbaseDetailFuel);
                CalcCustomPeriod();
            }
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
                    
                    //получение стоимости ресурса
                    DataRow[] row2 = mData.TableControlTowerFuelPrices.Select(String.Format("typeId = {0}", row["resourceTypeID"]));
                    //если такой ресурс там есть, то запоминаем его количество, если нет, оно равно 0
                    if (row2.Length > 0)
                        row["priceOne"] = row2[0]["price"];
                    
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

                        //надо вычислить, сколько топлива осталось на текущий момент
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
        private void CalcCustomPeriod()
        {
            if (mRowSelectedTower != null)
            {
                DateTime nearestTimeStamp = DateTime.UtcNow;
                //подгонка текущего времени к времени кормления поса
                DateTime stateTimeStamp = Convert.ToDateTime(mRowSelectedTower["stateTimeStamp"]);
                TimeSpan correct = nearestTimeStamp.Subtract(stateTimeStamp);
                correct = new TimeSpan(0, correct.Minutes, correct.Seconds);
                DateTime dt3 = nearestTimeStamp.Subtract(correct);
                if (dt3 > nearestTimeStamp)
                    dt3 = dt3.AddHours(-1);
                nearestTimeStamp = dt3;

                //берём число часов между нужной датой и текущей датой
                TimeSpan span = mCustomDate.Subtract(nearestTimeStamp);
                double hours = Math.Ceiling(span.TotalHours);
                double summVolume = 0;
                bool byPeriod = rbCalcFuelToPeriod.Checked;
                if (byPeriod)
                    hours = Convert.ToDouble(numCustomToPeriodDays.Value * 24 + numCustomToPeriodHours.Value);
                foreach (DataRow row in mTableFuel.Rows)
                {
                    //берём потребление в час
                    if (!(row["quantityAtPeriod"] is DBNull || row["quantity"] is DBNull || row["priceOne"] is DBNull))
                    {
                        double quantityAtPeriod = Convert.ToInt32(row["quantityAtPeriod"]);
                        //нужное топливо на указанный интервал = (потребление*часы)
                        double quantityAtCustomPeriod = quantityAtPeriod * hours;
                        //если не на период, то вычитаем имеющийся объём
                        if (!byPeriod)
                            quantityAtCustomPeriod -= Convert.ToDouble(row["quantity"]);
                        if (quantityAtCustomPeriod < 0)
                            quantityAtCustomPeriod = 0;
                        row["quantityAtCustomPeriod"] = quantityAtCustomPeriod;
                        if (!(row["volume"] is DBNull))
                        {
                            double volume = Convert.ToDouble(row["volume"]);
                            double customVolume = quantityAtCustomPeriod * volume;
                            row["volumeAtCustomPeriod"] = customVolume;
                            if (Convert.ToString(row["resourceTypeName"]) != "Strontium Clathrates")
                            {
                                summVolume += customVolume;
                            }
                        }
                        double priceOne = Convert.ToInt32(row["priceOne"]);
                        double priceAtCustomPeriod = quantityAtCustomPeriod * priceOne;
                        row["priceAtCustomPeriod"] = priceAtCustomPeriod;
                    }
                }
                lCustomVolume.Text = String.Format("Расчётный объём (не включая стронций): {0:#,##0.00} м³", summVolume);
            }
        }

        private void dgvCorpStarbaseDetailFuel_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (sender == dgvCorpStarbaseDetailFuel)
            {
                if (e.ColumnIndex >= 0 && e.RowIndex >= 0 && dgvCorpStarbaseDetailFuel.Columns[e.ColumnIndex].DataPropertyName == "fuelEnd")
                {
                    DataRowView drv = (DataRowView)dgvCorpStarbaseDetailFuel.Rows[e.RowIndex].DataBoundItem;
                    if (!(drv["fuelEnd"] is DBNull))
                    {
                        DateTime dt = Convert.ToDateTime(drv["fuelEnd"]);
                        if (dt.Year > 2099)
                        {
                            e.Value = "∞";
                            e.FormattingApplied = true;
                        }
                    }
                }
            }
        }
    }
}
