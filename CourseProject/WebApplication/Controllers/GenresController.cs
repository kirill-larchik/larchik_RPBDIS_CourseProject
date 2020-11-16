using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
    public class GenresController : Controller
    {
        private readonly TvChannelContext db;
        private readonly CacheProvider cache;

        private const string filterKey = "genres";

        public GenresController(TvChannelContext context, CacheProvider cacheProvider)
        {
            db = context;
            cache = cacheProvider;
        }

        public IActionResult Index(SortState sortState = SortState.GenreNameAsc, int page = 1)
        {
            GenresFilterViewModel filter = HttpContext.Session.Get<GenresFilterViewModel>(filterKey);
            if (filter == null)
            {
                filter = new GenresFilterViewModel { GenreName = string.Empty, GenreDescription = string.Empty };
                HttpContext.Session.Set(filterKey, filter);
            }

            string modelKey = $"{typeof(Genre).Name}-{page}-{sortState}-{filter.GenreName}-{filter.GenreDescription}";
            if (!cache.TryGetValue(modelKey, out GenresViewModel model))
            {
                model = new GenresViewModel();

                IQueryable<Genre> genres = GetSortedEntities(sortState, filter.GenreName, filter.GenreDescription);

                int count = genres.Count();
                int pageSize = 10;
                model.PageViewModel = new PageViewModel(page, count, pageSize);

                model.Entities = count == 0 ? new List<Genre>() : genres.Skip((model.PageViewModel.CurrentPage - 1) * pageSize).Take(pageSize).ToList();
                model.SortViewModel = new SortViewModel(sortState);
                model.GenresFilterViewModel = filter;

                cache.Set(modelKey, model);
            }

            return View(model);
        }

        [HttpPost]
        public IActionResult Index(GenresFilterViewModel filterModel, int page)
        {
            GenresFilterViewModel filter = HttpContext.Session.Get<GenresFilterViewModel>(filterKey);
            if (filter != null)
            {
                filter.GenreName = filterModel.GenreName;
                filter.GenreDescription = filterModel.GenreDescription;

                HttpContext.Session.Remove(filterKey);
                HttpContext.Session.Set(filterKey, filter);
            }

            return RedirectToAction("Index", new { page });
        }

        public IActionResult Create(int page)
        {
            GenresViewModel model = new GenresViewModel
            {
                PageViewModel = new PageViewModel { CurrentPage = page }
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(GenresViewModel model)
        {
            if (ModelState.IsValid & CheckUniqueValues(model.Entity))
            {
                await db.Genres.AddAsync(model.Entity);
                await db.SaveChangesAsync();

                cache.Clean();

                return RedirectToAction("Index", "Genres");
            }

            return View(model);
        }

        public async Task<IActionResult> Edit(int id, int page)
        {
            Genre genre = await db.Genres.FindAsync(id);
            if (genre != null)
            {
                GenresViewModel model = new GenresViewModel();
                model.PageViewModel = new PageViewModel { CurrentPage = page };
                model.Entity = genre;

                return View(model);
            }

            return NotFound();
        }

        [HttpPost]
        public async Task<IActionResult> Edit(GenresViewModel model)
        {
            if (ModelState.IsValid & CheckUniqueValues(model.Entity))
            {
                Genre genre = db.Genres.Find(model.Entity.GenreId);
                if (genre != null)
                {
                    genre.GenreName = model.Entity.GenreName;
                    genre.GenreDescription = model.Entity.GenreDescription;

                    db.Genres.Update(genre);
                    await db.SaveChangesAsync();

                    cache.Clean();

                    return RedirectToAction("Index", "Genres", new { page = model.PageViewModel.CurrentPage });
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
            Genre genre = await db.Genres.FindAsync(id);
            if (genre == null)
                return NotFound();

            bool deleteFlag = false;
            string message = "Do you want to delete this entity";

            if (db.Shows.Any(s => s.GenreId == genre.GenreId))
                message = "This entity has entities, which dependents from this. Do you want to delete this entity and other, which dependents from this?";

            GenresViewModel model = new GenresViewModel();
            model.Entity = genre;
            model.PageViewModel = new PageViewModel { CurrentPage = page };
            model.DeleteViewModel = new DeleteViewModel { Message = message, IsDeleted = deleteFlag };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(GenresViewModel model)
        {
            Genre genre = await db.Genres.FindAsync(model.Entity.GenreId);
            if (genre == null)
                return NotFound();

            db.Genres.Remove(genre);
            await db.SaveChangesAsync();

            cache.Clean();

            model.DeleteViewModel = new DeleteViewModel { Message = "The entity was successfully deleted.", IsDeleted = true };

            return View(model);
        }



        private bool CheckUniqueValues(Genre genre)
        {
            bool firstFlag = true;
            bool secondFlag = true;

            Genre tempGenre = db.Genres.FirstOrDefault(g => g.GenreName == genre.GenreName);
            if (tempGenre != null)
            {
                if (tempGenre.GenreId != genre.GenreId)
                {
                    ModelState.AddModelError(string.Empty, "Another entity have this name. Please replace this to another.");
                    firstFlag = false;
                }
            }

            tempGenre = db.Genres.FirstOrDefault(g => g.GenreDescription == genre.GenreDescription);
            if (tempGenre != null)
            {
                if (tempGenre.GenreId != genre.GenreId)
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

        private IQueryable<Genre> GetSortedEntities(SortState sortState, string genreName, string genreDescription)
        {
            IQueryable<Genre> genres = db.Genres.AsQueryable();

            switch (sortState)
            {
                case SortState.GenreNameAsc:
                    genres = genres.OrderBy(g => g.GenreName);
                    break;
                case SortState.GenreNameDesc:
                    genres = genres.OrderByDescending(g => g.GenreName);
                    break;
                case SortState.GenreDescriptionAsc:
                    genres = genres.OrderBy(g => g.GenreDescription);
                    break;
                case SortState.GenreDescriptionDesc:
                    genres = genres.OrderByDescending(g => g.GenreDescription);
                    break;
            }

            if (!string.IsNullOrEmpty(genreName))
                genres = genres.Where(g => g.GenreName.Contains(genreName)).AsQueryable();
            if (!string.IsNullOrEmpty(genreDescription))
                genres = genres.Where(g => g.GenreDescription.Contains(genreDescription)).AsQueryable();

            return genres;
        }
    }
}
