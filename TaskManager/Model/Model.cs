using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace TaskManager.Model
{
    public static class Model
    {

        public class ModelException : Exception
        {
            public string Header = TaskManager.Properties.Resources.MODEL_EXC_HEADER + "\n";
            public ModelException(string message)
                : base(message) { }
        }

        public static List<string> StatusNames = new List<string>
        {
            Properties.Resources.LIST_STATUS_0, // Назначена
            Properties.Resources.LIST_STATUS_1, // Выполняется
            Properties.Resources.LIST_STATUS_2, // Приостановлена
            Properties.Resources.LIST_STATUS_3  // Завершена
        };

        public static List<string> IconPaths = new List<string>
        {
            "/TaskManager;component/View/Icons/znew.png",   // Назначена
            "/TaskManager;component/View/Icons/zstart.png", // Выполняется
            "/TaskManager;component/View/Icons/zpause.png", // Приостановлена
            "/TaskManager;component/View/Icons/zdone.png"   // Завершена
        };

        public enum Operation
        {
            Insert,
            Update,
            Delete,
            ChangeStatus
        }

        public enum TaskStatus
        {
            None,
            New,
            Start,
            Pause,
            Done
        }

        public class TaskTreeElemment
        {
            public string Header { get; set; }
            public int? TaskId { get; set; }
            public string IconPath { get; set; }

            public ObservableCollection<TaskTreeElemment> Items { get; set; } = new ObservableCollection<TaskTreeElemment>();

            public TaskTreeElemment()
            {
                TaskId = 0;
                Header = "";
            }

            public TaskTreeElemment(int? taskID, String header)
            {
                TaskId = taskID;
                Header = header;
            }

            public override string ToString()
            {
                return Header;
            }
        }

        public static string TaskHandler(Operation operation, Task task)
        {
            string result;
            try
            {
                switch (operation)
                {

                    case Operation.Insert:
                        if (!task.ParentId.Equals(0) && GetTaskById(task.ParentId).StatusId == (int)TaskStatus.Done)
                            throw new ModelException(TaskManager.Properties.Resources.MODEL_EXC_AddingSubtasksToCompletedTasksIsProhibited);
                        else
                        {
                            result = InsertTask(task);
                        }
                        break;

                    case Operation.Update:
                        if (task.StatusId.Equals(TaskStatus.Done))
                            throw new ModelException(TaskManager.Properties.Resources.MODEL_EXC_ChangingCompletedTasksIsProhibited);
                        else
                        {
                            result = UpdateTask(task);
                        }
                        break;

                    case Operation.Delete:
                        if (CheckHasTaskChildTasks(task.TaskId))
                            throw new ModelException(TaskManager.Properties.Resources.MODEL_EXC_NecessaryToDeleteSubtasks);
                        else
                        {
                            result = DeleteTaskById(task.TaskId);
                        }
                        break;
                    case Operation.ChangeStatus:
                        if (task.StatusId == (int)TaskStatus.Done && CheckHasTaskUnstartedChildtasks(task))
                            throw new ModelException(TaskManager.Properties.Resources.MODEL_EXC_SubtasksAreNotAccepted);
                        else if (CheckIsTaskCompleted(task.TaskId))
                            throw new ModelException(TaskManager.Properties.Resources.MODEL_EXC_EditingCompletedTaskIsProhibited);
                        else
                        {
                            result = ChangeStatus(task);
                        }
                        break;
                    default:
                        throw new ModelException(TaskManager.Properties.Resources.MODEL_MSG_OperationNotDefined);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return result;
        }

        #region РАБОТА С БД. INSERT, UPDATE, DELETE, SELECT ---------------------------------------------------------

        // Найти задачу по ID...
        public static void InsertExecutionTime(int taskId)
        {
            try
            {
                using (Entities db = new Entities())
                {


                    ExecutionTime et = new ExecutionTime()
                    {
                        TaskId = taskId,
                        StartTime = DateTime.Now
                    };

                    db.ExecutionTimes.Add(et);

                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static void UpdateExecutionTime(int taskId)
        {
            try
            {
                using (Entities db = new Entities())
                {
                    var task = (from t in db.ExecutionTimes
                                where t.TaskId.Equals(taskId) && t.EndTime == null
                                select t).FirstOrDefault();
                    
                    if(task != null) task.EndTime = DateTime.Now;

                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private static string InsertTask(Task task)
        {
            try
            {
                using (Entities db = new Entities())
                {
                    db.Tasks.Add(task);
                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return TaskManager.Properties.Resources.MODEL_MSG_TaskSuccessfullyAdded;
        }

        private static string UpdateTask(Task task)
        {
            try
            {
                using (Entities db = new Entities())
                {
                    Task updTask = new Task();
                    updTask = (from t in db.Tasks
                               where t.TaskId.Equals(task.TaskId) && !t.IsDeleted
                               select t).FirstOrDefault();
                    updTask.TaskId = (int)task.TaskId;
                    updTask.ParentId = (int)task.ParentId;
                    updTask.Header = task.Header;
                    updTask.Body = task.Body;
                    updTask.Executors = task.Executors;
                    updTask.StatusId = task.StatusId;
                    updTask.PlanLaborInput = task.PlanLaborInput;

                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return TaskManager.Properties.Resources.MODEL_MSG_TaskSuccessfullyChanged;
        }

        private static string DeleteTaskById(int id)
        {
            using (Entities db = new Entities())
            {
                try
                {
                    Task delTask = (from t in db.Tasks
                                    where t.TaskId.Equals(id) && !t.IsDeleted
                                    select t).FirstOrDefault();

                    delTask.IsDeleted = true;
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    throw ex;
                }
                return TaskManager.Properties.Resources.MODEL_MSG_TaskSuccessfullyDeleted;
            }
        }

        private static string ChangeStatus(Task task)
        {
            try
            {
                using (Entities db = new Entities())
                {
                    Task updTask = new Task();
                    updTask = (from t in db.Tasks
                               where t.TaskId.Equals(task.TaskId) && !t.IsDeleted
                               select t).FirstOrDefault();

                    updTask.StatusId = task.StatusId;

                    if (task.StatusId == (int)TaskStatus.Start)
                    {
                        InsertExecutionTime(task.TaskId);
                    }
                    else if (task.StatusId == (int)TaskStatus.Pause)
                    {
                        UpdateExecutionTime(task.TaskId);
                    }
                    else if (task.StatusId == (int)TaskStatus.Done)
                    {
                        updTask.EndDate = DateTime.Today;
                        UpdateExecutionTime(updTask.TaskId);
                        void GetSubTask(Task i)
                        {
                            List<Task> subs = (from sub in db.Tasks
                                               where sub.ParentId == i.TaskId && !sub.IsDeleted
                                               select sub).ToList();

                            if (subs.Count > 0)
                            {
                                foreach (Task sub in subs)
                                {
                                    sub.StatusId = task.StatusId;
                                    sub.EndDate = DateTime.Today;
                                    UpdateExecutionTime(sub.TaskId);
                                    GetSubTask(sub);
                                }
                            }
                        }

                        GetSubTask(task);
                    }

                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return TaskManager.Properties.Resources.MODEL_MSG_TaskSuccessfullySaved;
        }

        // Получить дерево задач...
        public static List<TaskTreeElemment> GetTaskTree()
        {
            try
            {
                using (Entities db = new Entities())
                {
                    List<TaskTreeElemment> menuItemList = new List<TaskTreeElemment>();
                    TaskTreeElemment menuItem, menuSubItem;

                    List<fnGetFlatList_Result> taskTreeList = db.fnGetFlatList(0, 1).ToList();

                    var roots = (from root in taskTreeList where root.ParentId == 0 select root).ToList();

                    foreach (var root in roots)
                    {
                        menuItem = new TaskTreeElemment(root.TaskId, root.Header);
                        menuItem.IconPath = IconPaths[(int)root.StatusId - 1];
                        AddSubItem(menuItem, root);
                        menuItemList.Add(menuItem);
                    }

                    void AddSubItem(TaskTreeElemment mi, fnGetFlatList_Result i)
                    {
                        List<fnGetFlatList_Result> subs;

                        subs = (from sub in taskTreeList where sub.ParentId == i.TaskId select sub).ToList();

                        if (subs.Count > 0)
                        {
                            foreach (fnGetFlatList_Result sub in subs)
                            {
                                menuSubItem = new TaskTreeElemment(sub.TaskId, sub.Header);
                                menuSubItem.IconPath = IconPaths[(int)sub.StatusId - 1];
                                mi.Items.Add(menuSubItem);
                                AddSubItem(menuSubItem, sub);
                            }
                        }
                    }

                    return menuItemList;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        // Получить плоский список подзадач выбранной задачи 
        // (вариант с названиями статусов из ресурсов)...
        public class FlatListElement : fnGetFlatList_Result
        {
            public string StatusString { get; set; }
        }

        public static List<FlatListElement> GetFullTaskFlatList(int id = 0)
        {
            try
            {
                using (Entities db = new Entities())
                {
                    var list = db.fnGetFlatList(id, 0).ToList();
                    List<FlatListElement> result = new List<FlatListElement>();

                    result.ForEach(x =>
                    {
                        FlatListElement el = new FlatListElement();
                        el = x; el.StatusString = StatusNames[(int)x.StatusId - 1];
                        result.Add(el);
                    });

                    return result;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        // Получить плоский список подзадач выбранной задачи 
        // (вариант с названиями статусов из базы)...
        public static List<fnGetFlatList_Result> GetTaskFlatList(int id = 0)
        {
            try
            {
                using (Entities db = new Entities())
                {
                    return db.fnGetFlatList(id, 0).ToList();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        // Найти задачу по ID...
        public static Task GetTaskById(int id)
        {
            try
            {
                using (Entities db = new Entities())
                {
                    return db.Tasks.FirstOrDefault(p => p.TaskId == id);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        // Найти полную информацию по ID...
        public static fnGetFlatList_Result GetTaskInfoById(int id)
        {
            try
            {
                using (Entities db = new Entities())
                {
                    return db.fnGetFlatList((int)id, (int)0).FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        // Найти родительскую задачу...
        public static Task GetParentTask(int id)
        {
            try
            {
                using (Entities db = new Entities())
                {
                    return db.Tasks.FirstOrDefault(p => p.ParentId == id);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion


        #region БЛОК ПРОВЕРОК ---------------------------------------------------------        
        // У задачи есть подзадачи со статусом НАЗНАЧЕНА?
        private static bool CheckHasTaskUnstartedChildtasks(Task task)
        {
            bool result = false;
            if (task.StatusId == (int)TaskStatus.Done)
            {
                using (Entities db = new Entities())
                {
                    void AddSubItem(Task i)
                    {
                        List<Task> subs = (from sub in db.Tasks
                                           where sub.ParentId == i.TaskId && !sub.IsDeleted
                                           select sub).ToList();
                        if (subs.Count > 0)
                        {
                            foreach (Task sub in subs)
                            {
                                if (sub.StatusId == (int)TaskStatus.New)
                                {
                                    result = true;
                                    break;
                                }

                                AddSubItem(sub);
                            }
                        }
                    }

                    AddSubItem(task);
                }
            }

            return result;
        }

        // Задача имеет статус ВЫПОЛНЕНА?
        private static bool CheckIsTaskCompleted(int id)
        {
            using (Entities db = new Entities())
            {
                bool res = (from t in db.Tasks
                            where t.TaskId.Equals(id) && t.StatusId.Equals(4)
                            select t).Count() > 0;
                return res;
            }
        }

        // У задачи есть подзадачи?
        private static bool CheckHasTaskChildTasks(int id)
        {
            using (Entities db = new Entities())
            {
                bool r = (from t in db.Tasks
                          where t.ParentId.Equals(id) && !t.IsDeleted
                          select t).Count() != 0;
                return r;
            }
        }
        #endregion

        public static class Input
        {
            private static fnGetFlatList_Result selectedTaskFromList = new fnGetFlatList_Result();
            public static fnGetFlatList_Result SelectedTaskFromList
            {
                get
                {
                    return selectedTaskFromList;
                }
                set
                {
                    selectedTaskFromList = value;
                    if (value != null)
                    {
                        TaskId = (int)selectedTaskFromList.TaskId;
                        ParentId = (int)selectedTaskFromList.ParentId;
                        StatusId = (int)selectedTaskFromList.StatusId;
                        Header = selectedTaskFromList.Header;
                        Body = selectedTaskFromList.Body;
                        Executors = selectedTaskFromList.Executors;
                        PlanLaborInput = (int)selectedTaskFromList.PlanLaborInput;
                        FactLaborInput = (int)selectedTaskFromList.FactLaborInput;
                    }
                }
            }

            public static Operation Operation { get; set; }

            public static int SelectedTreeTaskId { get; set; }
            public static int SelectedListTaskId { get; set; }

            public static int TaskId { get; set; }
            public static int ParentId { get; set; }
            public static int StatusId { get; set; }
            public static int PlanLaborInput { get; set; }
            public static int FactLaborInput { get; set; }
            public static String Header { get; set; }
            public static String Body { get; set; }
            public static String Executors { get; set; }
            public static bool IsDeleted { get; set; }
        }

    }
}
