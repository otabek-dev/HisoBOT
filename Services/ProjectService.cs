﻿using HisoBOT.DB;
using HisoBOT.Models;
using HisoBOT.Results;
using Microsoft.EntityFrameworkCore;

namespace HisoBOT.Services
{
    public class ProjectService
    {
        private readonly AppDbContext _context;

        public ProjectService(AppDbContext context) 
        {
            _context = context;
        }

        public List<Project> GetAllProjects()
        {
            return _context.Projects.ToList();
        }

        public Result CreateProject(string chatId, string projectName)
        {
            if (_context.Projects.Any(x => x.ChatId == chatId))
                return new Result(false, "Проект с таким chat id уже существует! Введите другую...");

            if (chatId.Length < 3 || projectName.Length < 3)
                return new Result(false, "Меньше 3 символов в форматах не допускается!");

            _context.Projects.Add(new Project()
            {
                Id = Guid.NewGuid(),
                ChatId = chatId,
                Name = projectName
            });

            _context.SaveChanges();
            return new Result(true, "Проект создан");
        }

        public void DeleteProject(string chatId)
        {
            _context.Projects.Where(p => p.ChatId == chatId).ExecuteDelete();
        }
    }
}
