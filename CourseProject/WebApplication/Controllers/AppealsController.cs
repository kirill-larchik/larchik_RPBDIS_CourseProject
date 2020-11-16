using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
    public class AppealsController : Controller
    {
        private readonly TvChannelContext db;
        private readonly CacheProvider cache;

        private const string filterKey = "appeals";

        public AppealsController(TvChannelContext context, CacheProvider cacheProvider)
        {
            db = context;
            cache = cacheProvider;
        }

        public IActionResult Index(SortState sortState = SortState.AppealFullNameAsc, int page = 1)
        {
            AppealsFilterViewModel filter = HttpContext.Session.Get<AppealsFilterViewModel>(filterKey);
            if (filter == null)
            {
                filter = new AppealsFilterViewModel { FullName = string.Empty, Organization = string.Empty, ShowName = string.Empty, GoalRequest = string.Empty };
                HttpContext.Session.Set(filterKey, filter);
            }

            string modelKey = $"{typeof(Appeal).Name}-{page}-{sortState}-{filter.FullName}-{filter.Organization}-{filter.ShowName}-{filter.GoalRequest}";
            if (!cache.TryGetValue(modelKey, out AppealsViewModel model))
            {
                model = new AppealsViewModel();

                IQueryable<Appeal> appeals = GetSortedEntities(sortState, filter.FullName, filter.Organization, filter.ShowName, filter.GoalRequest);

                int count = appeals.Count();
                int pageSize = 10;
                model.PageViewModel = new PageViewModel(page, count, pageSize);

                model.Entities = count == 0 ? new List<Appeal>() : appeals.Skip((model.PageViewModel.CurrentPage - 1) * pageSize).Take(pageSize).ToList();
                model.SortViewModel = new SortViewModel(sortState);
                model.AppealsFilterViewModel = filter;

                cache.Set(modelKey, model);
            }

            return View(model);
        }

        [HttpPost]
        public IActionResult Index(AppealsFilterViewModel filterModel, int page)
        {
            AppealsFilterViewModel filter = HttpContext.Session.Get<AppealsFilterViewModel>(filterKey);
            if (filter != null)
            {
                filter.FullName = filterModel.FullName;
                filter.Organization = filterModel.Organization;
                filter.ShowName = filterModel.ShowName;
                filter.GoalRequest = filterModel.GoalRequest;

                HttpContext.Session.Remove(filterKey);
                HttpContext.Session.Set(filterKey, filter);
            }

            return RedirectToAction("Index", new { page });
        }

        public IActionResult Create(int page)
        {
            AppealsViewModel model = new AppealsViewModel();
            model.PageViewModel = new PageViewModel { CurrentPage = page };
            model.SelectList = db.Shows.ToList();

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(AppealsViewModel model)
        {
            model.SelectList = db.Shows.ToList();

            var show = db.Shows.FirstOrDefault(s => s.Name == model.ShowName);
            if (show == null)
            {
                ModelState.AddModelError(string.Empty, "Please select show from list.");
                return View(model);
            }

            model.Entity.Show = new Show { Name = model.ShowName };
            if (ModelState.IsValid & CheckUniqueValues(model.Entity))
            {
                model.Entity.Show = null;
                model.Entity.ShowId = show.ShowId;

                await db.Appeals.AddAsync(model.Entity);
                await db.SaveChangesAsync();

                cache.Clean();

                return RedirectToAction("Index");

            }

            return View(model);
        }

        public async Task<IActionResult> Edit(int id, int page)
        {
            Appeal appeal = await db.Appeals.Include(a => a.Show).FirstOrDefaultAsync(a => a.AppealId == id);
            if (appeal != null)
            {
                AppealsViewModel model = new AppealsViewModel();
                model.PageViewModel = new PageViewModel { CurrentPage = page };
                model.Entity = appeal;
                model.SelectList = db.Shows.ToList();
                model.ShowName = model.Entity.Show.Name;

                return View(model);
            }

            return NotFound();
        }

        [HttpPost]
        public async Task<IActionResult> Edit(AppealsViewModel model)
        {
            model.SelectList = db.Shows.ToList();

            var show = db.Shows.FirstOrDefault(s => s.Name == model.ShowName);
            if (show == null)
            {
                ModelState.AddModelError(string.Empty, "Please select show from list.");
                return View(model);
            }

            model.Entity.Show = new Show { Name = model.ShowName };
            if (ModelState.IsValid & CheckUniqueValues(model.Entity))
            {
                model.Entity.Show = null;

                Appeal appeal = await db.Appeals.FindAsync(model.Entity.AppealId);
                if (appeal != null)
                {

                    appeal.FullName = model.Entity.FullName;
                    appeal.Organization = model.Entity.Organization;
                    appeal.ShowId = show.ShowId;
                    appeal.GoalRequest = model.Entity.GoalRequest;

                    db.Appeals.Update(appeal);
                    await db.SaveChangesAsync();

                    cache.Clean();

                    return RedirectToAction("Index", "Appeals", new { page = model.PageViewModel.CurrentPage });
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
            Appeal appeal = await db.Appeals.FindAsync(id);
            if (appeal == null)
                return NotFound();

            bool deleteFlag = false;
            string message = "Do you want to delete this entity";

            if (db.Shows.Any(s => s.ShowId == appeal.ShowId))
                message = "This entity has entities, which dependents from this. Do you want to delete this entity and other, which dependents from this?";

            AppealsViewModel model = new AppealsViewModel();
            model.Entity = appeal;
            model.PageViewModel = new PageViewModel { CurrentPage = page };
            model.DeleteViewModel = new DeleteViewModel { Message = message, IsDeleted = deleteFlag };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(AppealsViewModel model)
        {
            Appeal appeal = await db.Appeals.FindAsync(model.Entity.AppealId);
            if (appeal == null)
                return NotFound();

            db.Appeals.Remove(appeal);
            await db.SaveChangesAsync();

            cache.Clean();

            model.DeleteViewModel = new DeleteViewModel { Message = "The entity was successfully deleted.", IsDeleted = true };

            return View(model);
        }

        private bool CheckUniqueValues(Appeal appeal)
        {
            bool firstFlag = true;

            Appeal tempAppeal = db.Appeals.Include(a => a.Show).FirstOrDefault(a => a.FullName == appeal.FullName && a.Show.Name == appeal.Show.Name);
            if (tempAppeal != null)
            {
                if (tempAppeal.AppealId != appeal.AppealId && tempAppeal.GoalRequest == appeal.GoalRequest)
                {
                    ModelState.AddModelError(string.Empty, "Another entity have this request from this organization. Please replace this to another.");
                    firstFlag = false;
                }
            }

            if (firstFlag)
                return true;
            else
                return false;
        }

        private IQueryable<Appeal> GetSortedEntities(SortState sortState, string fullName, string organization, string showName, string goalRequest)
        {
            IQueryable<Appeal> appeals = db.Appeals.Include(a => a.Show).AsQueryable();
            switch (sortState)
            {
                case SortState.AppealFullNameAsc:
                    appeals = appeals.OrderBy(a => a.FullName);
                    break;
                case SortState.AppealFullNameDesc:
                    appeals = appeals.OrderByDescending(a => a.FullName);
                    break;
                case SortState.AppealOrganizationAsc:
                    appeals = appeals.OrderBy(a => a.Organization);
                    break;
                case SortState.AppealOrganizationDesc:
                    appeals = appeals.OrderByDescending(a => a.Organization);
                    break;
                case SortState.ShowNameAsc:
                    appeals = appeals.OrderBy(a => a.Show.Name);
                    break;
                case SortState.ShowNameDesc:
                    appeals = appeals.OrderByDescending(a => a.Show.Name);
                    break;
                case SortState.AppealGoalRequestAsc:
                    appeals = appeals.OrderBy(a => a.GoalRequest);
                    break;
                case SortState.AppealGoalRequestDesc:
                    appeals = appeals.OrderByDescending(a => a.GoalRequest);
                    break;
            }

            if (!string.IsNullOrEmpty(fullName))
                appeals = appeals.Where(a => a.FullName.Contains(fullName)).AsQueryable();
            if (!string.IsNullOrEmpty(organization))
                appeals = appeals.Where(a => a.Organization.Contains(organization)).AsQueryable();
            if (!string.IsNullOrEmpty(showName))
                appeals = appeals.Where(a => a.Show.Name.Contains(showName)).AsQueryable();
            if (!string.IsNullOrEmpty(goalRequest))
                appeals = appeals.Where(a => a.GoalRequest.Contains(goalRequest)).AsQueryable();

            return appeals;
        }
    }
}
