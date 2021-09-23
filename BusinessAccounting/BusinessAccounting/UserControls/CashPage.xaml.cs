﻿using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using BusinessAccounting.Entity;
using XDatabase;
using XDatabase.Core;

namespace BusinessAccounting.UserControls
{
    /// <summary>
    /// Interaction logic for CashPage.xaml
    /// </summary>
    public partial class CashPage
    {
        public CashPage()
        {
            InitializeComponent();

            InputDate.DataContext = this;

            LoadDefaultDate();
            LoadHistory();
            LoadEmployees();
        }

        public DateTime? DefaultInputDate { get; set; }

        public static RoutedCommand SaveRecordCommand = new RoutedCommand();
        public static RoutedCommand LoadHistoryCommand = new RoutedCommand();
        public static RoutedCommand DeleteHistoryRecordCommand = new RoutedCommand();

        private const int PreloadRecordsCount = 30;
        private List<CashTransaction> _history = new List<CashTransaction>();
        private List<Employee> _employees = new List<Employee>();

        #region Functionality methods
        // load _history of cash operations from db and fill listview
        private void LoadHistory(bool all = false)
        {
            var query = "select c.id, c.datestamp, c.summa, c.comment, e.fullname from ba_cash_operations as c " +
                "left join ba_employees_cash as ec on ec.opid = c.id left " + 
                "join ba_employees_cardindex as e on e.id = ec.emid " +
                $"order by c.id desc { (all ? "" : "limit " + PreloadRecordsCount)};";

            _history = new List<CashTransaction>();

            var historyRecords = App.Sqlite.SelectTable(query);
            if (historyRecords != null)
            {
                foreach (DataRow row in historyRecords.Rows)
                {
                    _history.Add(new CashTransaction()
                    {
                        Id = Convert.ToInt32(row.ItemArray[0].ToString()),
                        Date = Convert.ToDateTime(row.ItemArray[1]),
                        Sum = decimal.Parse(row.ItemArray[2].ToString()),
                        Comment = row.ItemArray[3].ToString(),
                        EmployeeFullName = row.ItemArray[4].ToString()
                    });
                }
                LvHistory.ItemsSource = _history;
            }
            else
            {
                GroupHistory.Header = "Нет последних записей";
                GroupHistory.IsEnabled = false;
            }
        }

        private void LoadEmployees()
        {
            _employees = new List<Employee>();

            var employeesData = App.Sqlite.SelectTable("select id, fullname from ba_employees_cardindex where fired is null;");
            if (employeesData != null && employeesData.Rows.Count > 0)
            {
                foreach (DataRow r in employeesData.Rows)
                {
                    _employees.Add(new Employee
                    {
                        Id = Convert.ToInt32(r.ItemArray[0]),
                        FullName = r.ItemArray[1].ToString()
                    });
                }
            }
            ComboEmployee.ItemsSource = _employees;
        }

        // save new cash operation to db
        private void SaveRecord()
        {
            bool result;

            if (SalaryMode.IsChecked.HasValue && (bool) SalaryMode.IsChecked)
            {
                const string insertTransactionSql = "insert into ba_cash_operations (datestamp, summa, Comment) values (@D, @s, @c);";
                const string insertSalarySql = "insert into ba_employees_cash (emid, opid) values (@e, (select max(ba_cash_operations.id) from ba_cash_operations));";

                App.Sqlite.BeginTransaction();

                result = App.Sqlite.Insert(insertTransactionSql,
                    new XParameter("@d", InputDate.SelectedDate),
                    new XParameter("@s", Convert.ToDecimal(InputSum.Text)),
                    new XParameter("@c", InputComment.Text != "" ? InputComment.Text : null)) >=
                         (int) XQuery.XResult.ChangesApplied;

                if (!result)
                {
                    App.Sqlite.RollbackTransaction();
                }

                result = App.Sqlite.Insert(insertSalarySql, new XParameter("@e", _employees[ComboEmployee.SelectedIndex].Id)) >= (int) XQuery.XResult.ChangesApplied;

                if (!result)
                {
                    App.Sqlite.RollbackTransaction();
                }
                else
                {
                    result = App.Sqlite.CommitTransaction();
                }
            }
            else
            {
                const string insertSql = "insert into ba_cash_operations (datestamp, summa, Comment) values (@d, @s, @c);";
                result = App.Sqlite.Insert(insertSql,
                    new XParameter("@d", InputDate.SelectedDate),
                    new XParameter("@s", Convert.ToDecimal(InputSum.Text)),
                    new XParameter("@c", InputComment.Text != "" ? InputComment.Text : null)) >=
                         (int) XQuery.XResult.ChangesApplied;
            }

            if (result)
            {
                InputDate.SelectedDate = DefaultInputDate;
                InputSum.Text = "";
                InputComment.Text = "";
                ComboEmployee.SelectedIndex = -1;
                SalaryMode.IsChecked = false;
                LoadHistory();
            }
            else
            {
                ShowMessage("Не удалось сохранить запись в базе данных!");
            }
        }

        private async Task AskAndDelete(CashTransaction record)
        {
            if (record == null)
            {
                ShowMessage("Сначала выделите запись!");
                return;
            }

            var w = (MetroWindow)Parent.GetParentObject().GetParentObject();
            var result = await w.ShowMessageAsync("Delete запись?", 
                string.Format("Date: {1:dd MMMM yyyy}{0}Sum: {2:C}{0}A comment: {3}{0}Employee: {4}",
                Environment.NewLine, record.Date, record.Sum, record.Comment, record.EmployeeFullName), 
                MessageDialogStyle.AffirmativeAndNegative);

            if (result == MessageDialogResult.Affirmative)
            {
                const string deleteTransactionSql = "delete from ba_cash_operations where id = @id;";
                if (App.Sqlite.Delete(deleteTransactionSql, new XParameter("@id", record.Id)) <= (int)XQuery.XResult.NothingChanged)
                {
                    ShowMessage("Не удалось удалить запись из базы данных!");
                    return;
                }
                LoadHistory();
            }
        }

        private void ShowMessage(string text)
        {
            for (var visual = this as Visual; visual != null; visual = VisualTreeHelper.GetParent(visual) as Visual)
            {
                var window = visual as MetroWindow;
                window?.ShowMessageAsync("Проблемка", text + Environment.NewLine + App.Sqlite.LastErrorMessage);
            }
        }

        private void LoadDefaultDate()
        {
            int offset;
            if (!int.TryParse(ConfigurationManager.AppSettings["DefaultInputDateOffset"], out offset)) return;
            DefaultInputDate = DateTime.Now.Date.AddDays(offset);
        }
        #endregion

        #region Commands
        private void SaveRecord_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            decimal sum;

            e.CanExecute =
                SalaryMode.IsChecked.HasValue && (bool)SalaryMode.IsChecked & 
                (
                    ComboEmployee != null &&
                    ComboEmployee.SelectedIndex != -1 && // employee is selected
                    decimal.TryParse(InputSum.Text, out sum) && // Sum is entered
                    sum <= 0 // Sum is less then zero because you spent money
                    // or equals if it is a trial period for person
                )
                ||
                SalaryMode.IsChecked.HasValue && !(bool)SalaryMode.IsChecked &
                (
                    InputDate.SelectedDate != null && // Date is selected
                    InputSum.Text.Length > 0 && // Sum is entered
                    decimal.TryParse(InputSum.Text, out sum) // Sum is correct
                );
        }

        private void SaveRecord_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            using (new WaitCursor())
            {
                SaveRecord();
            }
        }

        private void LoadHistory_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = LvHistory != null && LvHistory.Items.Count <= PreloadRecordsCount;
        }

        private void LoadHistory_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            using (new WaitCursor())
            {
                LoadHistory(true);
            }
        }

        private void DeleteHistoryRecord_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = LvHistory.SelectedIndex >= 0;
        }

        private async void DeleteHistoryRecord_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (LvHistory.SelectedItem == null) return;
            var record = (CashTransaction) LvHistory.SelectedItem;
            await AskAndDelete(record);
        }

        #endregion
    }
}
