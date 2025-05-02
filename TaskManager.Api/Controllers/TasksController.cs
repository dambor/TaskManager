using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Core.Models;
using TaskManager.Core.Repositories;

namespace TaskManager.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TasksController : ControllerBase
    {
        private readonly ITaskRepository _taskRepository;

        public TasksController(ITaskRepository taskRepository)
        {
            _taskRepository = taskRepository;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Core.Models.Task>>> GetAll()
        {
            return Ok(await _taskRepository.GetAllAsync());
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Core.Models.Task>> GetById(Guid id)
        {
            var task = await _taskRepository.GetByIdAsync(id);
            if (task == null)
            {
                return NotFound();
            }
            return Ok(task);
        }

        [HttpGet("status/{status}")]
        public async Task<ActionResult<IEnumerable<Core.Models.Task>>> GetByStatus(Core.Models.TaskStatus status)
        {
            return Ok(await _taskRepository.GetByStatusAsync(status));
        }

        [HttpGet("assignee/{assignee}")]
        public async Task<ActionResult<IEnumerable<Core.Models.Task>>> GetByAssignee(string assignee)
        {
            return Ok(await _taskRepository.GetByAssigneeAsync(assignee));
        }

        [HttpPost]
        public async Task<ActionResult> Create(Core.Models.Task task)
        {
            await _taskRepository.CreateAsync(task);
            return CreatedAtAction(nameof(GetById), new { id = task.Id }, task);
        }
        
        [HttpPut("{id}")]
        public async Task<ActionResult> Update(Guid id, [FromBody] Core.Models.Task task)
        {
            if (id != task.Id)
            {
                return BadRequest();
            }

            // Get the existing task to preserve assignee if not provided
            var existingTask = await _taskRepository.GetByIdAsync(id);
            if (existingTask == null)
            {
                return NotFound();
            }

            // Preserve assignee if not provided in the update
            if (string.IsNullOrEmpty(task.AssignedTo))
            {
                task.AssignedTo = existingTask.AssignedTo;
            }

            try
            {
                await _taskRepository.UpdateAsync(task);
                
                // Return the updated task instead of NoContent
                return Ok(task);
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error updating task: {ex.Message}");
                return StatusCode(500, "An error occurred while updating the task.");
            }
        }


        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(Guid id)
        {
            await _taskRepository.DeleteAsync(id);
            return NoContent();
        }
    }
}