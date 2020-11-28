using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
    public class ShowsController : Controller
    {
        private readonly TvChannelContext db;
        private readonly CacheProvider cache;

        private const string filterKey = "shows";

        public ShowsController(TvChannelContext context, CacheProvider cacheProvider)
        {
            db = context;
            cache = cacheProvider;
        }

        public IActionResult Index(SortState sortState = SortState.ShowNameAsc, int page = 1)
        {
            ShowsFilterViewModel filter = HttpContext.Session.Get<ShowsFilterViewModel>(filterKey);
            if (filter == null)
            {
                filter = new ShowsFilterViewModel { Name = string.Empty, GenreName = string.Empty, StartDate = default, EndDate = default, Duration = default, ReleaseDate = default };
                HttpContext.Session.Set(filterKey, filter);
            }

            string modelKey = $"{typeof(Show).Name}-{page}-{sortState}-{filter.Name}-{filter.GenreName}-{filter.StaffName}-{filter.StartDate}-{filter.EndDate}-{filter.DurationString}-{filter.ReleaseDate}";
            if (!cache.TryGetValue(modelKey, out ShowsViewModel model))
            {
                model = new ShowsViewModel();

                if (!string.IsNullOrEmpty(filter.DurationString))
                    filter.Duration = TimeSpan.Parse(filter.DurationString);

                IQueryable<Show> shows = GetSortedEntities(sortState, filter);

                int count = shows.Count();

                int pageSize = 10;

                model.PageViewModel = new PageViewModel(page, count, pageSize);

                model.Entities = count == 0 ? new List<Show>() : shows.Skip((model.PageViewModel.CurrentPage - 1) * pageSize).Take(pageSize).ToList();
                model.SortViewModel = new SortViewModel(sortState);
                model.ShowsFilterViewModel = filter;

                cache.Set(modelKey, model);
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(ShowsViewModel filterModel, string staffName = null, string genreName = null)
        {
            ShowsFilterViewModel filter;
            if (!string.IsNullOrEmpty(staffName))
            {
                filter = new ShowsFilterViewModel { StaffName = staffName };
                HttpContext.Session.Set(filterKey, filter);

                return RedirectToAction("Index", new { page = 1 });
            }

            if (!string.IsNullOrEmpty(genreName))
            {
                filter = new ShowsFilterViewModel { GenreName = genreName };
                HttpContext.Session.Set(filterKey, filter);

                return RedirectToAction("Index", new { page = 1 });
            }

            filter = HttpContext.Session.Get<ShowsFilterViewModel>(filterKey);
            if (filter != null)
            {
                filter.Name = filterModel.ShowsFilterViewModel.Name;
                filter.GenreName = filterModel.ShowsFilterViewModel.GenreName;
                filter.StaffName = filterModel.ShowsFilterViewModel.StaffName;
                filter.StartDate = filterModel.ShowsFilterViewModel.StartDate;
                filter.EndDate = filterModel.ShowsFilterViewModel.EndDate;
                filter.DurationString = filterModel.ShowsFilterViewModel.Duration.ToString();
                filter.ReleaseDate = filterModel.ShowsFilterViewModel.ReleaseDate;

                HttpContext.Session.Remove(filterKey);
                HttpContext.Session.Set(filterKey, filter);
            }

            return RedirectToAction("Index", new { page = 1});
        }

        public IActionResult ClearFilter()
        {
            HttpContext.Session.Remove(filterKey);
            return RedirectToAction("Index", new { page = 1 });
        }

        [Authorize(Roles = "admin")]
        public IActionResult Create(int page)
        {
            ShowsViewModel model = new ShowsViewModel();
            model.PageViewModel = new PageViewModel { CurrentPage = page };
            model.SelectList = db.Genres.Select(g => g.GenreName).ToList();

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ShowsViewModel model)
        {
            model.SelectList = db.Genres.Select(g => g.GenreName).ToList();

            var genre = db.Genres.FirstOrDefault(g => g.GenreName == model.GenreName);
            if (genre == null)
            {
                ModelState.AddModelError(string.Empty, "Please select genre from list.");
                return View(model);
            }

            if (ModelState.IsValid & CheckUniqueValues(model.Entity))
            {
                if (model.Entity.MarkYear > model.Entity.ReleaseDate.Year ||
                    (model.Entity.MarkYear == model.Entity.ReleaseDate.Year && model.Entity.MarkMonth >= model.Entity.ReleaseDate.Month))
                {
                    model.Entity.GenreId = genre.GenreId;

                    await db.Shows.AddAsync(model.Entity);
                    await db.SaveChangesAsync();

                    cache.Clean();

                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Mark year(month) must be more then release date.");
                }
            }

            return View(model);
        }

        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Edit(int id, int page)
        {
            Show show = await db.Shows.Include(s => s.Genre).FirstOrDefaultAsync(s => s.ShowId == id);
            if (show != null)
            {
                ShowsViewModel model = new ShowsViewModel();
                model.PageViewModel = new PageViewModel { CurrentPage = page };
                model.Entity = show;
                model.SelectList = db.Genres.Select(g => g.GenreName).ToList();
                model.GenreName = model.Entity.Genre.GenreName;

                return View(model);
            }

            return NotFound();
        }

        [HttpPost]
        [Authorize(Roles = "admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ShowsViewModel model)
        {
            model.SelectList = db.Genres.Select(g => g.GenreName).ToList();

            var genre = db.Genres.FirstOrDefault(g => g.GenreName == model.GenreName);
            if (genre == null)
            {
                ModelState.AddModelError(string.Empty, "Please select genre from list.");
                return View(model);
            }

            if (ModelState.IsValid & CheckUniqueValues(model.Entity))
            {
                Show show = await db.Shows.FindAsync(model.Entity.ShowId);
                if (show != null)
                {
                    if (model.Entity.MarkYear > model.Entity.ReleaseDate.Year ||
                        (model.Entity.MarkYear == model.Entity.ReleaseDate.Year && model.Entity.MarkMonth >= model.Entity.ReleaseDate.Month))
                    {
                        show.Name = model.Entity.Name;
                        show.ReleaseDate = model.Entity.ReleaseDate;
                        show.Duration = model.Entity.Duration;
                        show.Mark = model.Entity.Mark;
                        show.MarkMonth = model.Entity.MarkMonth;
                        show.MarkYear = model.Entity.MarkYear;

                        show.GenreId = genre.GenreId;

                        show.Description = model.Entity.Description;

                        db.Shows.Update(show);
                        await db.SaveChangesAsync();

                        cache.Clean();

                        return RedirectToAction("Index", "Shows", new { page = model.PageViewModel.CurrentPage });
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "Mark year(month) must be more then release date.");
                    }
                }
                else
                {
                    return NotFound();
                }
            }

            return View(model);
        }

        public async Task<IActionResult> Details(int id, int page)
        {
            Show show = await db.Shows.Include(s => s.Genre).FirstOrDefaultAsync(s => s.ShowId == id);
            if (show == null)
                return NotFound();

            ShowsViewModel model = new ShowsViewModel();
            model.Entity = show;
            model.PageViewModel = new PageViewModel { CurrentPage = page };

            return View(model);
        }

        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Delete(int id, int page)
        {
            Show show = await db.Shows.FindAsync(id);
            if (show == null)
                return NotFound();

            bool deleteFlag = false;
            string message = "Do you want to delete this entity";

            if (db.Timetables.Any(s => s.ShowId == show.ShowId) || db.Appeals.Any(s => s.ShowId == show.ShowId))
                message = "This entity has entities, which dependents from this. Do you want to delete this entity and other, which dependents from this?";

            ShowsViewModel model = new ShowsViewModel();
            model.Entity = show;
            model.PageViewModel = new PageViewModel { CurrentPage = page };
            model.DeleteViewModel = new DeleteViewModel { Message = message, IsDeleted = deleteFlag };

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(ShowsViewModel model)
        {
            Show show = await db.Shows.FindAsync(model.Entity.ShowId);
            if (show == null)
                return NotFound();

            db.Shows.Remove(show);
            await db.SaveChangesAsync();

            cache.Clean();

            model.DeleteViewModel = new DeleteViewModel { Message = "The entity was successfully deleted.", IsDeleted = true };

            return View(model);
        }

        private bool CheckUniqueValues(Show show)
        {
            bool firstFlag = true;
            bool secondFlag = true;

            Show tempShow = db.Shows.FirstOrDefault(s => s.Name == show.Name);
            if (tempShow != null)
            {
                if (tempShow.ShowId != show.ShowId)
                {
                    ModelState.AddModelError(string.Empty, "Another entity have this name. Please replace this to another.");
                    firstFlag = false;
                }
            }

            tempShow = db.Shows.FirstOrDefault(s => s.Description == show.Description);
            if (tempShow != null)
            {
                if (tempShow.ShowId != show.ShowId)
                {
                    ModelState.AddModelError(string.Empty, "Another entity have this name. Please replace this to another.");
                    firstFlag = false;
                }
            }

            if (firstFlag && secondFlag)
                return true;
            else
                return false;
        }

        private IQueryable<Show> GetSortedEntities(SortState sortState, ShowsFilterViewModel filterModel)
        {
            IQueryable<Show> shows = db.Shows.Include(s => s.Genre).AsQueryable();
            switch (sortState)
            {
                case SortState.ShowNameAsc:
                    shows = shows.OrderBy(s => s.Name);
                    break;
                case SortState.ShowNameDesc:
                    shows = shows.OrderByDescending(s => s.Name);
                    break;
                case SortState.ShowDescriptionAsc:
                    shows = shows.OrderBy(s => s.Description);
                    break;
                case SortState.ShowDescriptionDesc:
                    shows = shows.OrderByDescending(s => s.Description);
                    break;
                case SortState.GenreNameAsc:
                    shows = shows.OrderBy(s => s.Genre.GenreName);
                    break;
                case SortState.GenreNameDesc:
                    shows = shows.OrderByDescending(s => s.Genre.GenreName);
                    break;
            }

            if (!string.IsNullOrEmpty(filterModel.Name))
                shows = shows.Where(s => s.Name.Contains(filterModel.Name)).AsQueryable();

            if (!string.IsNullOrEmpty(filterModel.GenreName))
                shows = shows.Where(s => s.Genre.GenreName.Contains(filterModel.GenreName)).AsQueryable();

            if (!string.IsNullOrEmpty(filterModel.StaffName))
            {
                shows = shows
                    .Join(db.Timetables, s => s.ShowId, t => t.ShowId, (s, t) => new { s, t })
                    .Join(db.Staff, st => st.t.StaffId, s => s.StaffId, (st, s) => new { st, s })
                    .Where(sts => sts.s.FullName == filterModel.StaffName)
                    .Select(sts => sts.st.s)
                    .AsQueryable();
            }

            // Improve?
            if (filterModel.StartDate != default || filterModel.EndDate != default)
            {
                DateTime endDate = new DateTime(filterModel.EndDate.Year, filterModel.EndDate.Month, DateTime.DaysInMonth(filterModel.EndDate.Year, filterModel.EndDate.Month));

                shows = shows
                .Join(db.Timetables, s => s.ShowId, t => t.ShowId, (s, t) => new { s, t })
                .AsEnumerable()
                .Select((q, d) => new { q, d = new DateTime(q.t.Year, q.t.Month, DateTime.DaysInMonth(q.t.Year, q.t.Month)) })
                .Where(q => q.d.Date >= filterModel.StartDate.Date && q.d.Date <= endDate)
                .Select(q => q.q.s)
                .AsQueryable();
            }

            if (filterModel.Duration != default)
            {
                shows = shows.Where(s => s.Duration <= filterModel.Duration).AsQueryable();
            }

            if (filterModel.ReleaseDate != default)
            {
                shows = shows.Where(s => s.ReleaseDate == filterModel.ReleaseDate).AsQueryable();
            }

            return shows;
        }
    }
}
