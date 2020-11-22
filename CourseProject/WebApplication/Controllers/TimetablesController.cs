using System;
using System.Buffers;
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
                filter = new TimetablesFilterViewModel { DayOfWeek = 0, Month = 0, Year = 0, StaffName = string.Empty, ShowName = string.Empty };
                HttpContext.Session.Set(filterKey, filter);
            }

            string modelKey = $"{typeof(Timetable).Name}-{page}-{sortState}-{filter.DayOfWeek}-{filter.Month}-{filter.Year}-{filter.StaffName}-{filter.ShowName}";
            if (!cache.TryGetValue(modelKey, out TimetablesViewModel model))
            {
                model = new TimetablesViewModel();

                IQueryable<Timetable> timetables = GetSortedEntities(sortState, filter);

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
        [ValidateAntiForgeryToken]
        public IActionResult Index(TimetablesViewModel filterModel, int page = 1)
        {
            TimetablesFilterViewModel filter = HttpContext.Session.Get<TimetablesFilterViewModel>(filterKey);
            if (filter != null)
            {
                filter.DayOfWeek = filterModel.TimetablesFilterViewModel.DayOfWeek;
                filter.Month = filterModel.TimetablesFilterViewModel.Month;
                filter.Year = filterModel.TimetablesFilterViewModel.Year;
                filter.StaffName = filterModel.TimetablesFilterViewModel.StaffName;
                filter.ShowName = filterModel.TimetablesFilterViewModel.ShowName;

                HttpContext.Session.Remove(filterKey);
                HttpContext.Session.Set(filterKey, filter);
            }

            return RedirectToAction("Index", new { page });
        }

        public IActionResult ClearFilter(int page)
        {
            HttpContext.Session.Remove(filterKey);
            return RedirectToAction("Index", new { page });
        }


        [Authorize(Roles = "admin")]
        public async Task<ActionResult> Create(int page)
        {
            TimetablesViewModel model = new TimetablesViewModel();
            model.PageViewModel = new PageViewModel { CurrentPage = page };

            model.ShowsSelectList = await db.Shows.Select(s => s.Name).ToListAsync();
            model.StaffSelectList = await db.Staff.Select(s => s.FullName).ToListAsync();

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TimetablesViewModel model)
        {
            model.ShowsSelectList = await db.Shows.Select(s => s.Name).ToListAsync();
            model.StaffSelectList = await db.Staff.Select(s => s.FullName).ToListAsync();

            var show = db.Shows.FirstOrDefault(s => s.Name == model.ShowName);
            if (show == null)
            {
                ModelState.AddModelError(string.Empty, "Please select show from list.");
                return View(model);
            }

            var staff = db.Staff.FirstOrDefault(s => s.FullName == model.StaffName);
            if (staff == null)
            {
                ModelState.AddModelError(string.Empty, "Please select staff from list.");
                return View(model);
            }

            if (ModelState.IsValid)
            {
                if (model.Entity.Year > show.ReleaseDate.Year ||
                        (model.Entity.Year == show.ReleaseDate.Year && model.Entity.Month >= show.ReleaseDate.Month))
                {
                    model.Entity.ShowId = show.ShowId;
                    model.Entity.EndTime = model.Entity.StartTime + show.Duration;
                    model.Entity.StaffId = staff.StaffId;

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

        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Edit(int id, int page)
        {
            Timetable timetable = await db.Timetables.Include(t => t.Show).Include(t => t.Staff).FirstOrDefaultAsync(t => t.TimetableId == id);
            if (timetable != null)
            {
                TimetablesViewModel model = new TimetablesViewModel();
                model.PageViewModel = new PageViewModel { CurrentPage = page };
                model.Entity = timetable;
                model.ShowsSelectList = db.Shows.Select(s => s.Name).AsNoTracking().AsQueryable();
                model.StaffSelectList = db.Staff.Select(s => s.FullName).AsNoTracking().AsQueryable();
                model.ShowName = model.Entity.Show.Name;
                model.StaffName = model.Entity.Staff.FullName;

                return View(model);
            }

            return NotFound();
        }

        [HttpPost]
        [Authorize(Roles = "admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(TimetablesViewModel model)
        {
            model.ShowsSelectList = db.Shows.Select(s => s.Name).AsNoTracking().AsQueryable();
            model.StaffSelectList = db.Staff.Select(s => s.FullName).AsNoTracking().AsQueryable();

            var show = db.Shows.FirstOrDefault(s => s.Name == model.ShowName);
            if (show == null)
            {
                ModelState.AddModelError(string.Empty, "Please select show from list.");
                return View(model);
            }

            var staff = db.Staff.FirstOrDefault(s => s.FullName == model.StaffName);
            if (staff == null)
            {
                ModelState.AddModelError(string.Empty, "Please select staff from list.");
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

                        timetable.StaffId = staff.StaffId;

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

        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Delete(int id, int page)
        {
            Timetable timetable = await db.Timetables.FindAsync(id);
            if (timetable == null)
                return NotFound();

            bool deleteFlag = false;
            string message = "Do you want to delete this entity";

            //if (db.Timetables.Any(t => t.TimetableId == timetable.TimetableId))
            //    message = "This entity has entities, which dependents from this. Do you want to delete this entity and other, which dependents from this?";

            TimetablesViewModel model = new TimetablesViewModel();
            model.Entity = timetable;
            model.PageViewModel = new PageViewModel { CurrentPage = page };
            model.DeleteViewModel = new DeleteViewModel { Message = message, IsDeleted = deleteFlag };

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(TimetablesViewModel model)
        {
            Timetable timetable = await db.Timetables.FindAsync(model.Entity.TimetableId);
            if (timetable == null)
                return NotFound();

            db.Timetables.Remove(timetable);
            await db.SaveChangesAsync();

            cache.Clean();

            model.DeleteViewModel = new DeleteViewModel { Message = "The entity was successfully deleted.", IsDeleted = true };

            return View(model);
        }

        private IQueryable<Timetable> GetSortedEntities(SortState sortState, TimetablesFilterViewModel filterModel)
        {
            IQueryable<Timetable> timetables = db.Timetables.Include(t => t.Show).Include(t => t.Staff).AsQueryable();
            switch (sortState)
            {
                case SortState.TimetableDayOfWeekAsc:
                    timetables = timetables.OrderBy(t => t.DayOfWeek);
                    break;
                case SortState.TimetableDayOfWeekDesc:
                    timetables = timetables.OrderByDescending(t => t.DayOfWeek);
                    break;
                case SortState.TimetableMonthAsc:
                    timetables = timetables.OrderBy(t => t.Month);
                    break;
                case SortState.TimetableMonthDesc:
                    timetables = timetables.OrderByDescending(t => t.Month);
                    break;
                case SortState.TimetableYearAsc:
                    timetables = timetables.OrderBy(t => t.Year);
                    break;
                case SortState.TimetableYearDesc:
                    timetables = timetables.OrderByDescending(t => t.Year);
                    break;
                case SortState.ShowNameAsc:
                    timetables = timetables.OrderBy(t => t.Show.Name);
                    break;
                case SortState.ShowNameDesc:
                    timetables = timetables.OrderByDescending(t => t.Show.Name);
                    break;
                case SortState.TimetableStartTimeAsc:
                    timetables = timetables.OrderBy(t => t.StartTime);
                    break;
                case SortState.TimetableStartTimeDesc:
                    timetables = timetables.OrderByDescending(t => t.StartTime);
                    break;
                case SortState.TimetablEndTimeAsc:
                    timetables = timetables.OrderBy(t => t.EndTime);
                    break;
                case SortState.TimetablEndTimeDesc:
                    timetables = timetables.OrderByDescending(t => t.EndTime);
                    break;
                case SortState.StaffFullNameAsc:
                    timetables = timetables.OrderBy(t => t.Staff.FullName);
                    break;
                case SortState.StaffFullNameDesc:
                    timetables = timetables.OrderByDescending(t => t.Staff.FullName);
                    break;
            }

            if (filterModel.DayOfWeek != 0)
                timetables = timetables.Where(t => t.DayOfWeek == filterModel.DayOfWeek).AsQueryable();
            if (filterModel.Month != 0)
                timetables = timetables.Where(t => t.Month == filterModel.Month).AsQueryable();
            if (filterModel.Year != 0)
                timetables = timetables.Where(t => t.Year == filterModel.Year).AsQueryable();
            if (!string.IsNullOrEmpty(filterModel.StaffName))
            {
                timetables = timetables.Where(t => t.Staff.FullName == filterModel.StaffName).AsQueryable();
            }
            if (!string.IsNullOrEmpty(filterModel.ShowName))
            {
                timetables = timetables.Where(t => t.Show.Name == filterModel.ShowName).AsQueryable();
            }

            return timetables;
        }
    }
}
