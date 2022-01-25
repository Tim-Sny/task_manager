using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
//using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TaskManager.Model;
using TaskManager.View;
using static TaskManager.Model.Model;
//using TreeViewItem = TaskManager.Model.DBManager.TreeViewItem;

namespace TaskManager.ViewModel
{
    class ViewModelException : Exception
    {
        public string Header = TaskManager.Properties.Resources.VM_EXC_HEADER+ "\n";
        public ViewModelException(string message)
            : base(message) { }
    }

    public class ViewModel : INotifyPropertyChanged
    {
        public fnGetFlatList_Result SelectedTaskFromList { get { return Input.SelectedTaskFromList; } set { Input.SelectedTaskFromList = value; } }

        // Дерево задач...
        private List<TaskTreeElemment> _taskTreeElemments = Model.Model.GetTaskTree();
        public List<TaskTreeElemment> TaskTreeElemments
        {
            get
            { return _taskTreeElemments; }
            set
            {
                _taskTreeElemments = value;
                NotifyPropertyChanged("TaskTreeChanged");
            }
        }

        // Плоский список задач...
        private List<fnGetFlatList_Result> _taskInfos = Model.Model.GetTaskFlatList(Input.SelectedTreeTaskId);
        public List<fnGetFlatList_Result> TaskInfos
        {
            get
            { return _taskInfos; }
            set
            {
                _taskInfos = value;
                NotifyPropertyChanged("TaskInfoChanged");
            }
        }


        //Открыть окно создания/редактирования задачи...
        private void OpenTaskWindow()
        {
            TaskWindow taskWindow = new TaskWindow();
            taskWindow.Owner = Application.Current.MainWindow;
            taskWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            taskWindow.ShowDialog();
        }

        //Открыть окно создания/редактирования задачи...
        private void OpenMessageWindow(string message)
        {
            MessageWindow taskWindow = new MessageWindow(message);
            taskWindow.Owner = Application.Current.MainWindow;
            taskWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            taskWindow.ShowDialog();
        }




        #region КОМАНДЫ ИНТЕРФЕЙСА --------------------------------------------------------------------------------------------------------------------

        // Редактировать задачу...
        private RelayCommand taskEditFormSaveCommand;
        public RelayCommand TaskEditFormSaveCommand
        {
            get
            {
                return taskEditFormSaveCommand ?? new RelayCommand(obj =>
                {
                    Window w = obj as Window;
                    int PlanLaborInputInt;
                    bool IsNum = int.TryParse(InputPlanLaborStr, out PlanLaborInputInt);

                    if (!IsNum || PlanLaborInputInt <= 0)
                        SetBlockControlBorder(w, "PlanHoursField");
                    else if(Input.Header == null || Input.Header.Replace(" ", "").Length == 0)
                        SetBlockControlBorder(w, "HeaderField");
                    else 
                    {
                        Input.PlanLaborInput = PlanLaborInputInt;
                        execTaskHandler(Properties.Settings.Default.ShowСonfirmationOfUpdating);
                        w.Close();
                    }
                });
            }
        }

        // Установить статус ВЫПОЛНЯЕТСЯ...
        private RelayCommand setStatusStartCommand;
        public RelayCommand SetStatusStartCommand
        {
            get
            {
                return setStatusStartCommand ?? new RelayCommand(obj =>
                {
                    SelectedTaskFromList = GetTaskInfoById((int)obj);
                    Input.Operation = Operation.ChangeStatus;
                    Input.StatusId = (int)TaskStatus.Start;
                    execTaskHandler(Properties.Settings.Default.ShowСonfirmationOfStatusChanging);
                });
            }
        }

        // Установить статус ПРИОСТАНОВЛЕНА...
        private RelayCommand setStatusPauseCommand;
        public RelayCommand SetStatusPauseCommand
        {
            get
            {
                return setStatusPauseCommand ?? new RelayCommand(obj =>
                {
                    SelectedTaskFromList = GetTaskInfoById((int)obj);
                    Input.Operation = Operation.ChangeStatus;
                    Input.StatusId = (int)TaskStatus.Pause;
                    execTaskHandler(Properties.Settings.Default.ShowСonfirmationOfStatusChanging);
                });
            }
        }

        // Установить статус ЗАВЕРШЕНА...
        private RelayCommand setStatusDoneCommand;
        public RelayCommand SetStatusDoneCommand
        {
            get
            {
                return setStatusDoneCommand ?? new RelayCommand(obj =>
                {
                    try
                    {
                        SelectedTaskFromList = GetTaskInfoById((int)obj);
                        if (SelectedTaskFromList.StatusId == 1)
                            throw new ViewModelException(TaskManager.Properties.Resources.VM_EXC_ForbiddenToCompleteAssignedTask);
                        Input.Operation = Operation.ChangeStatus;
                        Input.StatusId = (int)TaskStatus.Done;
                        execTaskHandler(Properties.Settings.Default.ShowСonfirmationOfStatusChanging);
                    }
                    catch (ViewModelException ex)
                    {
                        OpenMessageWindow(ex.Message);
                    }
                });
            }
        }

        // Выбрать корневую задачу...
        private RelayCommand selectRootTaskCommand;
        public RelayCommand SelectRootTaskCommand
        {
            get
            {
                return selectRootTaskCommand ?? new RelayCommand(obj =>
                {
                    Input.SelectedTreeTaskId = (int)obj;
                    RefreshListDataView();
                });
            }
        }

        // Создать новую корневую задачу...
        private RelayCommand insertRootTaskWindowCommand;
        public RelayCommand InsertRootTaskWindowCommand
        {
            get
            {
                return insertRootTaskWindowCommand ?? new RelayCommand(obj =>
                {
                    SetPropertiesToNull();
                    Input.Operation = Operation.Insert;
                    Input.ParentId = 0;
                    Input.StatusId = (int)TaskStatus.New;
                    OpenTaskWindow();
                });
            }
        }

        // Создать новую подзадачу...
        private RelayCommand insertSubTaskWindowCommand;
        public RelayCommand InsertSubTaskWindowCommand
        {
            get
            {
                return insertSubTaskWindowCommand ?? new RelayCommand(obj =>
                 {
                     try
                     {
                         SelectedTaskFromList = GetTaskInfoById((int)obj);
                         if (SelectedTaskFromList.StatusId == 4)
                             throw new ViewModelException(TaskManager.Properties.Resources.VM_EXC_AddingSubtasksToCompletedTasksIsProhibited);
                         SetPropertiesToNull();
                         Input.Operation = Operation.Insert;
                         Input.ParentId = (int)SelectedTaskFromList.TaskId;
                         Input.StatusId = 1;
                         OpenTaskWindow();

                     }
                     catch (Exception ex)
                     {
                         OpenMessageWindow(ex.Message);
                     }
                 });
            }
        }

        // Измененить задачу/подзадачу...
        private RelayCommand updateTaskWindowCommand;
        public RelayCommand UpdateTaskWindowCommand
        {
            get
            {
                return updateTaskWindowCommand ?? new RelayCommand(obj =>
                {
                    try
                    {
                        SelectedTaskFromList = GetTaskInfoById((int)obj);
                        if (SelectedTaskFromList.StatusId == 4)
                            throw new ViewModelException(TaskManager.Properties.Resources.VM_EXC_ChangingCompletedTasksIsProhibited);
                        Input.Operation = Operation.Update;
                        OpenTaskWindow();
                    }
                    catch (ViewModelException ex)
                    {
                        OpenMessageWindow(ex.Message);
                    }
                });
            }
        }

        // Удалить задачу/подзадачу...
        private RelayCommand deleteTaskWindowCommand;
        public RelayCommand DeleteTaskWindowCommand
        {
            get
            {
                return deleteTaskWindowCommand ?? new RelayCommand(obj =>
                {
                    SelectedTaskFromList = GetTaskInfoById((int)obj);
                    Input.Operation = Operation.Delete;
                    Input.TaskId = InputTaskId;
                    execTaskHandler(Properties.Settings.Default.ShowСonfirmationOfDeletion);
                });
            }
        }

        #endregion

        //Запуск обработки модели...
        private void execTaskHandler(bool showMessage)
        {
            string result;

            try
            {
                Model.Task task = new Model.Task();

                task.TaskId = Input.TaskId;
                task.ParentId = Input.ParentId;
                task.Header = Input.Header;
                task.Body = Input.Body == null ? "" : Input.Body;
                task.Executors = Input.Executors == null ? "" : Input.Executors;
                task.StartDate = DateTime.Today.Date;
                task.PlanLaborInput = Input.PlanLaborInput;
                //task.FactLaborInput = Input.FactLaborInput;
                task.StatusId = Input.StatusId;
                task.IsDeleted = false;

                result = Model.Model.TaskHandler(Input.Operation, task);
                if (showMessage)
                    OpenMessageWindow(result);
            }
            catch (ViewModelException ex)
            {
                OpenMessageWindow(ex.Header + ex.Message);
            }
            catch (ModelException ex)
            {
                OpenMessageWindow(ex.Header + ex.Message);
            }
            catch (Exception ex)
            {
                OpenMessageWindow(ex.Message);
            }
            finally
            {
                RefreshAllDataViews();
            }
        }

        //Обновление элементов интерфейса...
        private void RefreshListDataView()
        {
            TaskInfos = Model.Model.GetTaskFlatList(Input.SelectedTreeTaskId);
            MainWindow.ViewTaskList.ItemsSource = null;
            MainWindow.ViewTaskList.Items.Clear();
            MainWindow.ViewTaskList.ItemsSource = TaskInfos;
            MainWindow.ViewTaskList.Items.Refresh();
            MainWindow.ViewTaskList.Focus();
        }

        private void RefreshTreeDataView()
        {
            TaskTreeElemments = Model.Model.GetTaskTree();
            MainWindow.ViewTaskTree.ItemsSource = null;
            MainWindow.ViewTaskTree.Items.Clear();
            MainWindow.ViewTaskTree.ItemsSource = TaskTreeElemments;
            MainWindow.ViewTaskTree.Items.Refresh();
        }


        private void RefreshAllDataViews()
        {
            RefreshTreeDataView();
            RefreshListDataView();
        }

        //Устновка свойств в NULL...
        private void SetPropertiesToNull()
        {
            Input.TaskId = 0;
            Input.ParentId = 0;
            Input.StatusId = 1;
            Input.Header = "";
            Input.Body = "";
            Input.Executors = "";
            Input.PlanLaborInput = 0;
            Input.IsDeleted = false;
        }

        // Выделить UI элемент красной рамкой...
        private void SetBlockControlBorder(Window w, string name)
        {
            Control block = w.FindName(name) as Control;
            block.BorderBrush = Brushes.Red;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public int InputTaskId { get { return Input.TaskId; } set { Input.TaskId = value; } }
        public int InputParentId { get { return Input.ParentId; } set { Input.ParentId = value; } }
        public int InputStatusId { get { return Input.StatusId; } set { Input.StatusId = value; } }
        public String InputHeader { get { return Input.Header; } set { Input.Header = value; } }
        public String InputBody { get { return Input.Body; } set { Input.Body = value; } }
        public String InputExecutors { get { return Input.Executors; } set { Input.Executors = value; } }
        public int InputPlanLabor { get { return Input.PlanLaborInput; } set { Input.PlanLaborInput = value; } }
        public String InputPlanLaborStr { get { return InputPlanLabor.ToString(); } set { Input.PlanLaborInput = int.Parse(value); } }
        public bool InputIsDeleted { get { return Input.IsDeleted; } set { Input.IsDeleted = value; } }


    }
}
