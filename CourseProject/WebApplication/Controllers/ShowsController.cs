using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
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
                filter = new ShowsFilterViewModel { Name = string.Empty, GenreName = string.Empty };
                HttpContext.Session.Set(filterKey, filter);
            }

            string modelKey = $"{typeof(Show).Name}-{page}-{sortState}-{filter.Name}-{filter.GenreName}";
            if (!cache.TryGetValue(modelKey, out ShowsViewModel model))
            {
                model = new ShowsViewModel();

                IQueryable<Show> shows = GetSortedEntities(sortState, filter.Name, filter.GenreName);

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
        public IActionResult Index(ShowsFilterViewModel filterModel, int page)
        {
            ShowsFilterViewModel filter = HttpContext.Session.Get<ShowsFilterViewModel>(filterKey);
            if (filter != null)
            {
                filter.Name = filterModel.Name;
                filter.GenreName = filterModel.GenreName;

                HttpContext.Session.Remove(filterKey);
                HttpContext.Session.Set(filterKey, filter);
            }

            return RedirectToAction("Index", new { page });
        }

        public IActionResult Create(int page)
        {
            ShowsViewModel model = new ShowsViewModel();
            model.PageViewModel = new PageViewModel { CurrentPage = page };
            model.SelectList = db.Genres.ToList();

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(ShowsViewModel model)
        {
            model.SelectList = db.Genres.ToList();

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

        public async Task<IActionResult> Edit(int id, int page)
        {
            Show show = await db.Shows.Include(s => s.Genre).FirstOrDefaultAsync(s => s.ShowId == id);
            if (show != null)
            {
                ShowsViewModel model = new ShowsViewModel();
                model.PageViewModel = new PageViewModel { CurrentPage = page };
                model.Entity = show;
                model.SelectList = db.Genres.ToList();
                model.GenreName = model.Entity.Genre.GenreName;

                return View(model);
            }

            return NotFound();
        }

        [HttpPost]
        public async Task<IActionResult> Edit(ShowsViewModel model)
        {
            model.SelectList = db.Genres.ToList();

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

        public async Task<IActionResult> Delete(int id, int page)
        {
            Show show = await db.Shows.FindAsync(id);
            if (show == null)
                return NotFound();

            bool deleteFlag = false;
            string message = "Do you want to delete this entity";

            if (db.Timetables.Any(s => s.ShowId == show.ShowId))
                message = "This entity has entities, which dependents from this. Do you want to delete this entity and other, which dependents from this?";

            ShowsViewModel model = new ShowsViewModel();
            model.Entity = show;
            model.PageViewModel = new PageViewModel { CurrentPage = page };
            model.DeleteViewModel = new DeleteViewModel { Message = message, IsDeleted = deleteFlag };

            return View(model);
        }

        [HttpPost]
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

        private IQueryable<Show> GetSortedEntities(SortState sortState, string name, string genreName)
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

            if (!string.IsNullOrEmpty(name))
                shows = shows.Where(s => s.Name.Contains(name)).AsQueryable();
            if (!string.IsNullOrEmpty(genreName))
                shows = shows.Where(s => s.Genre.GenreName.Contains(genreName)).AsQueryable();

            return shows;
        }
    }
}
