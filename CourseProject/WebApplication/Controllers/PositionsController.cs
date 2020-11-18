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
    public class PositionsController : Controller
    {
        private readonly TvChannelContext db;
        private readonly CacheProvider cache;

        private const string filterKey = "positions";

        public PositionsController(TvChannelContext context, CacheProvider cacheProvider)
        {
            db = context;
            cache = cacheProvider;
        }

        public IActionResult Index(SortState sortState = SortState.PositionsNameAsc, int page = 1)
        {
            PositionsFilterViewModel filter = HttpContext.Session.Get<PositionsFilterViewModel>(filterKey);
            if (filter == null)
            {
                filter = new PositionsFilterViewModel { PositionName = string.Empty };
                HttpContext.Session.Set(filterKey, filter);
            }

            string modelKey = $"{typeof(Position).Name}-{page}-{sortState}-{filter.PositionName}";
            if (!cache.TryGetValue(modelKey, out PositionsViewModel model))
            {
                model = new PositionsViewModel();

                IQueryable<Position> positions = GetSortedEntities(sortState, filter.PositionName);

                int count = positions.Count();
                int pageSize = 10;
                model.PageViewModel = new PageViewModel(page, count, pageSize);

                model.Entities = count == 0 ? new List<Position>() : positions.Skip((model.PageViewModel.CurrentPage - 1) * pageSize).Take(pageSize).ToList();
                model.SortViewModel = new SortViewModel(sortState);
                model.PositionsFilterViewModel = filter;

                cache.Set(modelKey, model);
            }

            return View(model);
        }

        [HttpPost]
        public IActionResult Index(PositionsFilterViewModel filterModel, int page)
        {
            PositionsFilterViewModel filter = HttpContext.Session.Get<PositionsFilterViewModel>(filterKey);
            if (filter != null)
            {
                filter.PositionName = filterModel.PositionName;

                HttpContext.Session.Remove(filterKey);
                HttpContext.Session.Set(filterKey, filter);
            }

            return RedirectToAction("Index", new { page });
        }

        public IActionResult Create(int page)
        {
            PositionsViewModel model = new PositionsViewModel
            {
                PageViewModel = new PageViewModel { CurrentPage = page }
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(PositionsViewModel model)
        {
            if (ModelState.IsValid & CheckUniqueValues(model.Entity))
            {
                await db.Positions.AddAsync(model.Entity);
                await db.SaveChangesAsync();

                cache.Clean();

                return RedirectToAction("Index", "Positions");
            }

            return View(model);
        }

        public async Task<IActionResult> Edit(int id, int page)
        {
            Position position = await db.Positions.FindAsync(id);
            if (position != null)
            {
                PositionsViewModel model = new PositionsViewModel();
                model.PageViewModel = new PageViewModel { CurrentPage = page };
                model.Entity = position;

                return View(model);
            }

            return NotFound();
        }

        [HttpPost]
        public async Task<IActionResult> Edit(PositionsViewModel model)
        {
            if (ModelState.IsValid & CheckUniqueValues(model.Entity))
            {
                Position position = db.Positions.Find(model.Entity.PositionId);
                if (position != null)
                {
                    position.Name = model.Entity.Name;

                    db.Positions.Update(position);
                    await db.SaveChangesAsync();

                    cache.Clean();

                    return RedirectToAction("Index", "Positions", new { page = model.PageViewModel.CurrentPage });
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
            Position position = await db.Positions.FindAsync(id);
            if (position == null)
                return NotFound();

            bool deleteFlag = false;
            string message = "Do you want to delete this entity";

            if (db.Staff.Any(s => s.PositionId == position.PositionId))
                message = "This entity has entities, which dependents from this. Do you want to delete this entity and other, which dependents from this?";

            PositionsViewModel model = new PositionsViewModel();
            model.Entity = position;
            model.PageViewModel = new PageViewModel { CurrentPage = page };
            model.DeleteViewModel = new DeleteViewModel { Message = message, IsDeleted = deleteFlag };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(PositionsViewModel model)
        {
            Position position = await db.Positions.FindAsync(model.Entity.PositionId);
            if (position == null)
                return NotFound();

            db.Positions.Remove(position);
            await db.SaveChangesAsync();

            cache.Clean();

            model.DeleteViewModel = new DeleteViewModel { Message = "The entity was successfully deleted.", IsDeleted = true };

            return View(model);
        }



        private bool CheckUniqueValues(Position position)
        {
            bool firstFlag = true;

            Position tempPosition = db.Positions.FirstOrDefault(g => g.Name == position.Name);
            if (tempPosition != null)
            {
                if (tempPosition.PositionId != position.PositionId)
                {
                    ModelState.AddModelError(string.Empty, "Another entity have this name. Please replace this to another.");
                    firstFlag = false;
                }
            }

            if (firstFlag)
                return true;
            else
                return false;
        }

        private IQueryable<Position> GetSortedEntities(SortState sortState, string positionName)
        {
            IQueryable<Position> positions = db.Positions.AsQueryable();

            switch (sortState)
            {
                case SortState.PositionsNameAsc:
                    positions = positions.OrderBy(p => p.Name);
                    break;
                case SortState.PositionsNameDesc:
                    positions = positions.OrderByDescending(p => p.Name);
                    break;
            }

            if (!string.IsNullOrEmpty(positionName))
                positions = positions.Where(p => p.Name.Contains(positionName)).AsQueryable();

            return positions;
        }
    }
}
