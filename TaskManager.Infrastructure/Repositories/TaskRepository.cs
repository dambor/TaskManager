using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cassandra;
using Cassandra.Mapping;
using TaskManager.Core.Models;
using TaskManager.Core.Repositories;
using TaskManager.Infrastructure;

namespace TaskManager.Infrastructure.Repositories
{
    public class TaskRepository : ITaskRepository
    {
        private readonly CassandraContext _context;
        private readonly IMapper _mapper;

        public TaskRepository(CassandraContext context)
        {
            _context = context;
            _mapper = new Mapper(_context.Session);
        }

        public async System.Threading.Tasks.Task<Core.Models.Task> GetByIdAsync(Guid id)
        {
            var query = "SELECT * FROM tasks WHERE id = ?";
            return await _mapper.SingleOrDefaultAsync<Core.Models.Task>(query, id);
        }

        public async System.Threading.Tasks.Task<IEnumerable<Core.Models.Task>> GetAllAsync()
        {
            return await _mapper.FetchAsync<Core.Models.Task>("SELECT * FROM tasks");
        }

        public async System.Threading.Tasks.Task<IEnumerable<Core.Models.Task>> GetByStatusAsync(Core.Models.TaskStatus status)
        {
            var query = "SELECT * FROM tasks_by_status WHERE status = ?";
            var taskIds = await _mapper.FetchAsync<Core.Models.Task>(query, (int)status);
            
            // Fetch complete task details for each task
            var tasks = new List<Core.Models.Task>();
            foreach (var task in taskIds)
            {
                var fullTask = await GetByIdAsync(task.Id);
                if (fullTask != null)
                {
                    tasks.Add(fullTask);
                }
            }
            
            return tasks;
        }

        public async System.Threading.Tasks.Task<IEnumerable<Core.Models.Task>> GetByAssigneeAsync(string assignee)
        {
            var query = "SELECT * FROM tasks_by_assignee WHERE assigned_to = ?";
            var taskIds = await _mapper.FetchAsync<Core.Models.Task>(query, assignee);
            
            // Fetch complete task details for each task
            var tasks = new List<Core.Models.Task>();
            foreach (var task in taskIds)
            {
                var fullTask = await GetByIdAsync(task.Id);
                if (fullTask != null)
                {
                    tasks.Add(fullTask);
                }
            }
            
            return tasks;
        }

        public async System.Threading.Tasks.Task CreateAsync(Core.Models.Task task)
        {
            task.Id = Guid.NewGuid();
            task.CreatedAt = DateTime.UtcNow;
            task.UpdatedAt = DateTime.UtcNow;

            // Insert into main table
            var insertQuery = new SimpleStatement(
                "INSERT INTO tasks (id, title, description, due_date, status, assigned_to, created_at, updated_at) VALUES (?, ?, ?, ?, ?, ?, ?, ?)",
                task.Id, task.Title, task.Description, task.DueDate, 
                (int)task.Status, task.AssignedTo, task.CreatedAt, task.UpdatedAt);
            
            await _context.Session.ExecuteAsync(insertQuery);

            // Insert into status index table
            var statusIndexQuery = new SimpleStatement(
                "INSERT INTO tasks_by_status (status, id, title, due_date, assigned_to) VALUES (?, ?, ?, ?, ?)",
                (int)task.Status, task.Id, task.Title, task.DueDate, task.AssignedTo);
            
            await _context.Session.ExecuteAsync(statusIndexQuery);

            // Insert into assignee index table
            var assigneeIndexQuery = new SimpleStatement(
                "INSERT INTO tasks_by_assignee (assigned_to, id, title, due_date, status) VALUES (?, ?, ?, ?, ?)",
                task.AssignedTo, task.Id, task.Title, task.DueDate, (int)task.Status);
            
            await _context.Session.ExecuteAsync(assigneeIndexQuery);
        }

        // In TaskRepository.UpdateAsync method - add null checking:
        public async System.Threading.Tasks.Task UpdateAsync(Core.Models.Task task)
        {
            // First, get the old task to update the indexes properly
            var oldTask = await GetByIdAsync(task.Id);
            if (oldTask == null)
            {
                throw new Exception($"Task with ID {task.Id} not found");
            }

            // IMPORTANT: Preserve the original assignee if the new one is null or empty
            if (string.IsNullOrEmpty(task.AssignedTo))
            {
                task.AssignedTo = oldTask.AssignedTo;
            }

            task.CreatedAt = oldTask.CreatedAt;
            task.UpdatedAt = DateTime.UtcNow;

            // Update the main table
            var updateQuery = new SimpleStatement(
                "UPDATE tasks SET title = ?, description = ?, due_date = ?, status = ?, assigned_to = ?, updated_at = ? WHERE id = ?",
                task.Title, task.Description, task.DueDate, 
                (int)task.Status, task.AssignedTo, task.UpdatedAt, task.Id);
            
            await _context.Session.ExecuteAsync(updateQuery);

            // If status changed, update the status index
            if (oldTask.Status != task.Status)
            {
                // Delete from old status index
                var deleteStatusQuery = new SimpleStatement(
                    "DELETE FROM tasks_by_status WHERE status = ? AND id = ?",
                    (int)oldTask.Status, task.Id);
                
                await _context.Session.ExecuteAsync(deleteStatusQuery);

                // Insert into new status index
                var insertStatusQuery = new SimpleStatement(
                    "INSERT INTO tasks_by_status (status, id, title, due_date, assigned_to) VALUES (?, ?, ?, ?, ?)",
                    (int)task.Status, task.Id, task.Title, task.DueDate, task.AssignedTo);
                
                await _context.Session.ExecuteAsync(insertStatusQuery);
            }
            else
            {
                // Just update the existing status record
                var updateStatusQuery = new SimpleStatement(
                    "UPDATE tasks_by_status SET title = ?, due_date = ?, assigned_to = ? WHERE status = ? AND id = ?",
                    task.Title, task.DueDate, task.AssignedTo, (int)task.Status, task.Id);
                
                await _context.Session.ExecuteAsync(updateStatusQuery);
            }

            // If assignee changed, handle the assignee index carefully
            if (oldTask.AssignedTo != task.AssignedTo)
            {
                // Only delete from old assignee index if the old assignee was not null or empty
                if (!string.IsNullOrEmpty(oldTask.AssignedTo))
                {
                    var deleteAssigneeQuery = new SimpleStatement(
                        "DELETE FROM tasks_by_assignee WHERE assigned_to = ? AND id = ?",
                        oldTask.AssignedTo, task.Id);
                    
                    await _context.Session.ExecuteAsync(deleteAssigneeQuery);
                }

                // Only insert into new assignee index if the new assignee is not null or empty
                if (!string.IsNullOrEmpty(task.AssignedTo))
                {
                    var insertAssigneeQuery = new SimpleStatement(
                        "INSERT INTO tasks_by_assignee (assigned_to, id, title, due_date, status) VALUES (?, ?, ?, ?, ?)",
                        task.AssignedTo, task.Id, task.Title, task.DueDate, (int)task.Status);
                    
                    await _context.Session.ExecuteAsync(insertAssigneeQuery);
                }
            }
            else if (!string.IsNullOrEmpty(task.AssignedTo))
            {
                // Just update the existing assignee record if assignee exists
                var updateAssigneeQuery = new SimpleStatement(
                    "UPDATE tasks_by_assignee SET title = ?, due_date = ?, status = ? WHERE assigned_to = ? AND id = ?",
                    task.Title, task.DueDate, (int)task.Status, task.AssignedTo, task.Id);
                
                await _context.Session.ExecuteAsync(updateAssigneeQuery);
            }
        }

        // This is the corrected DeleteAsync method for TaskRepository.cs
        public async System.Threading.Tasks.Task DeleteAsync(Guid id)
        {
            // First, get the task to delete from indexes
            var task = await GetByIdAsync(id);
            if (task == null)
            {
                // Task doesn't exist, so nothing to delete
                return;
            }

            // Add null checks and empty guid checks
            if (id == Guid.Empty)
            {
                throw new ArgumentException("Task ID cannot be empty", nameof(id));
            }

            // Delete from main table
            var deleteMainQuery = new SimpleStatement("DELETE FROM tasks WHERE id = ?", id);
            await _context.Session.ExecuteAsync(deleteMainQuery);
            
            // Delete from status index - we can use the task.Status directly
            var deleteStatusQuery = new SimpleStatement(
                "DELETE FROM tasks_by_status WHERE status = ? AND id = ?", 
                (int)task.Status, id);
            
            await _context.Session.ExecuteAsync(deleteStatusQuery);
            
            // Make sure we have a valid assignedTo before trying to delete from the assignee index
            if (!string.IsNullOrEmpty(task.AssignedTo))
            {
                // Delete from assignee index
                var deleteAssigneeQuery = new SimpleStatement(
                    "DELETE FROM tasks_by_assignee WHERE assigned_to = ? AND id = ?", 
                    task.AssignedTo, id);
                
                await _context.Session.ExecuteAsync(deleteAssigneeQuery);
            }
        }
    }
}