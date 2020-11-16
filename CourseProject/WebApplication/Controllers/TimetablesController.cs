using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebApplication.Data;
using WebApplication.Infrastructure;
using WebApplication.Models;
using WebApplication.Services;
using WebApplication.ViewModels;
using WebApplication.ViewModels.Entities;
using WebApplication.ViewModels.Filters;

namespace WebApplication.Controllers
{
    [Authorize]
    public class TimetablesController : Controller
    {
        private readonly TvChannelContext db;
        private readonly CacheProvider cache;

        private const string filterKey = "timetables";

        public TimetablesController(TvChannelContext context, CacheProvider cacheProvider)
        {
            db = context;
            cache = cacheProvider;
        }

        public IActionResult Index(SortState sortState = SortState.ShowNameAsc, int page = 1)
        {
            TimetablesFilterViewModel filter = HttpContext.Session.Get<TimetablesFilterViewModel>(filterKey);
            if (filter == null)
            {
                filter = new TimetablesFilterViewModel { DayOfWeek = 0, Month = 0, Year = 0 };
                HttpContext.Session.Set(filterKey, filter);
            }

            string modelKey = $"{typeof(Timetable).Name}-{page}-{sortState}-{filter.DayOfWeek}-{filter.Month}-{filter.Year}";
            if (!cache.TryGetValue(modelKey, out TimetablesViewModel model))
            {
                model = new TimetablesViewModel();

                IQueryable<Timetable> timetables = GetSortedEntities(sortState, filter.DayOfWeek, filter.Month, filter.Year);

                int count = timetables.Count();
                int pageSize = 10;
                model.PageViewModel = new PageViewModel(page, count, pageSize);

                model.Entities = count == 0 ? new List<Timetable>() : timetables.Skip((model.PageViewModel.CurrentPage - 1) * pageSize).Take(pageSize).ToList();
                model.SortViewModel = new SortViewModel(sortState);
                model.TimetablesFilterViewModel = filter;

                cache.Set(modelKey, model);
            }

            return View(model);
        }

        [HttpPost]
        public IActionResult Index(TimetablesFilterViewModel filterModel, int page)
        {
            TimetablesFilterViewModel filter = HttpContext.Session.Get<TimetablesFilterViewModel>(filterKey);
            if (filter != null)
            {
                filter.DayOfWeek = filterModel.DayOfWeek;
                filter.Month = filterModel.Month;
                filter.Year = filterModel.Year;

                HttpContext.Session.Remove(filterKey);
                HttpContext.Session.Set(filterKey, filter);
            }

            return RedirectToAction("Index", new { page });
        }


        public ActionResult Create(int page)
        {
            TimetablesViewModel model = new TimetablesViewModel();
            model.PageViewModel = new PageViewModel { CurrentPage = page };
            model.SelectList = db.Shows.ToList();

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(TimetablesViewModel model)
        {
            model.SelectList = db.Shows.ToList();

            var show = db.Shows.FirstOrDefault(s => s.Name == model.ShowName);
            if (show == null)
            {
                ModelState.AddModelError(string.Empty, "Please select show from list.");
                return View(model);
            }

            if (ModelState.IsValid)
            {
                if (model.Entity.Year > show.ReleaseDate.Year ||
                        (model.Entity.Year == show.ReleaseDate.Year && model.Entity.Month >= show.ReleaseDate.Month))
                {
                    model.Entity.ShowId = show.ShowId;
                    model.Entity.EndTime = model.Entity.StartTime + show.Duration;

                    await db.Timetables.AddAsync(model.Entity);
                    await db.SaveChangesAsync();

                    cache.Clean();

                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, $"Mark year(month) must be more then release date ({show.ReleaseDate.ToString("d")})");
                }
            }
            
            return View(model);
        }

        public async Task<IActionResult> Edit(int id, int page)
        {
            Timetable timetable = await db.Timetables.Include(t => t.Show).FirstOrDefaultAsync(t => t.TimetableId == id);
            if (timetable != null)
            {
                TimetablesViewModel model = new TimetablesViewModel();
                model.PageViewModel = new PageViewModel { CurrentPage = page };
                model.Entity = timetable;
                model.SelectList = db.Shows.ToList();
                model.ShowName = model.Entity.Show.Name;

                return View(model);
            }

            return NotFound();
        }

        [HttpPost]
        public async Task<IActionResult> Edit(TimetablesViewModel model)
        {
            model.SelectList = db.Shows.ToList();

            var show = db.Shows.FirstOrDefault(s => s.Name == model.ShowName);
            if (show == null)
            {
                ModelState.AddModelError(string.Empty, "Please select show from list.");
                return View(model);
            }

            if (ModelState.IsValid)
            {
                Timetable timetable = await db.Timetables.FindAsync(model.Entity.TimetableId);
                if (timetable != null)
                {
                    if (model.Entity.Year > show.ReleaseDate.Year ||
                        (model.Entity.Year == show.ReleaseDate.Year && model.Entity.Month >= show.ReleaseDate.Month))
                    {
                        timetable.DayOfWeek = model.Entity.DayOfWeek;
                        timetable.Month = model.Entity.Month;
                        timetable.Year = model.Entity.Year;

                        timetable.ShowId = show.ShowId;

                        timetable.StartTime = model.Entity.StartTime;
                        timetable.EndTime = timetable.StartTime + show.Duration;

                        db.Timetables.Update(timetable);
                        await db.SaveChangesAsync();

                        cache.Clean();

                        return RedirectToAction("Index", "Timetables", new { page = model.PageViewModel.CurrentPage });
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, $"Mark year(month) must be more then release date ({show.ReleaseDate.ToString("d")})");
                    }
                }
                else
                {
                    return NotFound();
                }
            }

            return View(model);
        }

        public async Task<IActionResult> Delete(int id, int page)
        {
            Timetable timetable = await db.Timetables.FindAsync(id);
            if (timetable == null)
                return NotFound();

            bool deleteFlag = false;
            string message = "Do you want to delete this entity";

            if (db.Timetables.Any(t => t.TimetableId == timetable.TimetableId))
                message = "This entity has entities, which dependents from this. Do you want to delete this entity and other, which dependents from this?";

            TimetablesViewModel model = new TimetablesViewModel();
            model.Entity = timetable;
            model.PageViewModel = new PageViewModel { CurrentPage = page };
            model.DeleteViewModel = new DeleteViewModel { Message = message, IsDeleted = deleteFlag };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(TimetablesViewModel model)
        {
            Timetable timetable = await db.Timetables.FindAsync(model.Entity.TimetableId);
            if (timetable == null)
                return NotFound();

            db.Timetables.Remove(timetable);

            cache.Clean();

            model.DeleteViewModel = new DeleteViewModel { Message = "The entity was successfully deleted.", IsDeleted = true };

            return View(model);
        }

        private IQueryable<Timetable> GetSortedEntities(SortState sortState, int dayOfWeek, int month, int year)
        {
            IQueryable<Timetable> timtetables = db.Timetables.Include(t => t.Show).AsQueryable();
            switch (sortState)
            {
                case SortState.TimetableDayOfWeekAsc:
                    timtetables = timtetables.OrderBy(t => t.DayOfWeek);
                    break;
                case SortState.TimetableDayOfWeekDesc:
                    timtetables = timtetables.OrderByDescending(t => t.DayOfWeek);
                    break;
                case SortState.TimetableMonthAsc:
                    timtetables = timtetables.OrderBy(t => t.Month);
                    break;
                case SortState.TimetableMonthDesc:
                    timtetables = timtetables.OrderByDescending(t => t.Month);
                    break;
                case SortState.TimetableYearAsc:
                    timtetables = timtetables.OrderBy(t => t.Year);
                    break;
                case SortState.TimetableYearDesc:
                    timtetables = timtetables.OrderByDescending(t => t.Year);
                    break;
                case SortState.ShowNameAsc:
                    timtetables = timtetables.OrderBy(t => t.Show.Name);
                    break;
                case SortState.ShowNameDesc:
                    timtetables = timtetables.OrderByDescending(t => t.Show.Name);
                    break;
                case SortState.TimetableStartTimeAsc:
                    timtetables = timtetables.OrderBy(t => t.StartTime);
                    break;
                case SortState.TimetableStartTimeDesc:
                    timtetables = timtetables.OrderByDescending(t => t.StartTime);
                    break;
                case SortState.TimetablEndTimeAsc:
                    timtetables = timtetables.OrderBy(t => t.EndTime);
                    break;
                case SortState.TimetablEndTimeDesc:
                    timtetables = timtetables.OrderByDescending(t => t.EndTime);
                    break;
            }

            if (dayOfWeek != 0)
                timtetables = timtetables.Where(t => t.DayOfWeek == dayOfWeek).AsQueryable();
            if (month != 0)
                timtetables = timtetables.Where(t => t.Month == month).AsQueryable();
            if (year != 0)
                timtetables = timtetables.Where(t => t.Year == year).AsQueryable();

            return timtetables;
        }
    }
}
