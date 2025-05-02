using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaskManager.Core.Models;

namespace TaskManager.Core.Repositories
{
    public interface ITaskRepository
    {
        System.Threading.Tasks.Task<Models.Task> GetByIdAsync(Guid id);
        System.Threading.Tasks.Task<IEnumerable<Models.Task>> GetAllAsync();
        System.Threading.Tasks.Task<IEnumerable<Models.Task>> GetByStatusAsync(Models.TaskStatus status);
        System.Threading.Tasks.Task<IEnumerable<Models.Task>> GetByAssigneeAsync(string assignee);
        System.Threading.Tasks.Task CreateAsync(Models.Task task);
        System.Threading.Tasks.Task UpdateAsync(Models.Task task);
        System.Threading.Tasks.Task DeleteAsync(Guid id);
    }
}