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
    [Authorize(Roles = "admin")]
    public class StaffController : Controller
    {
        private readonly TvChannelContext db;
        private readonly CacheProvider cache;

        private const string filterKey = "staff";

        public StaffController(TvChannelContext context, CacheProvider cacheProvider)
        {
            db = context;
            cache = cacheProvider;
        }

        public IActionResult Index(SortState sortState = SortState.StaffFullNameAsc, int page = 1)
        {
            StaffFilterViewModel filter = HttpContext.Session.Get<StaffFilterViewModel>(filterKey);
            if (filter == null)
            {
                filter = new StaffFilterViewModel { FullName = string.Empty, PositionName = string.Empty };
                HttpContext.Session.Set(filterKey, filter);
            }

            string modelKey = $"{typeof(Staff).Name}-{page}-{sortState}-{filter.FullName}-{filter.PositionName}";
            if (!cache.TryGetValue(modelKey, out StaffViewModel model))
            {
                model = new StaffViewModel();

                IQueryable<Staff> Staff = GetSortedEntities(sortState, filter.FullName, filter.PositionName);

                int count = Staff.Count();
                int pageSize = 10;
                model.PageViewModel = new PageViewModel(page, count, pageSize);

                model.Entities = count == 0 ? new List<Staff>() : Staff.Skip((model.PageViewModel.CurrentPage - 1) * pageSize).Take(pageSize).ToList();
                model.SortViewModel = new SortViewModel(sortState);
                model.StaffFilterViewModel = filter;

                cache.Set(modelKey, model);
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(StaffFilterViewModel filterModel, int page)
        {
            StaffFilterViewModel filter = HttpContext.Session.Get<StaffFilterViewModel>(filterKey);
            if (filter != null)
            {
                filter.FullName = filterModel.FullName;
                filter.PositionName = filterModel.PositionName;

                HttpContext.Session.Remove(filterKey);
                HttpContext.Session.Set(filterKey, filter);
            }

            return RedirectToAction("Index", new { page });
        }

        public IActionResult Create(int page)
        {
            StaffViewModel model = new StaffViewModel();
            model.PageViewModel = new PageViewModel { CurrentPage = page };
            model.SelectList = db.Positions.Select(p => p.Name).ToList();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(StaffViewModel model)
        {
            model.SelectList = db.Positions.Select(p => p.Name).ToList();

            var position = db.Positions.FirstOrDefault(g => g.Name == model.PositionName);
            if (position == null)
            {
                ModelState.AddModelError(string.Empty, "Please select position from list.");
                return View(model);
            }

            if (ModelState.IsValid & CheckUniqueValues(model.Entity))
            {
                model.Entity.PositionId = position.PositionId;

                await db.Staff.AddAsync(model.Entity);
                await db.SaveChangesAsync();

                cache.Clean();

                return RedirectToAction("Index");
            }
            
            return View(model);
        }

        public async Task<IActionResult> Edit(int id, int page)
        {
            Staff staff = await db.Staff.Include(s => s.Position).FirstOrDefaultAsync(s => s.StaffId == id);
            if (staff != null)
            {
                StaffViewModel model = new StaffViewModel();
                model.PageViewModel = new PageViewModel { CurrentPage = page };
                model.Entity = staff;
                model.SelectList = db.Positions.Select(p => p.Name).ToList();
                model.PositionName = model.Entity.Position.Name;

                return View(model);
            }

            return NotFound();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(StaffViewModel model)
        {
            model.SelectList = db.Positions.Select(p => p.Name).ToList();

            var position = db.Positions.FirstOrDefault(g => g.Name == model.PositionName);
            if (position == null)
            {
                ModelState.AddModelError(string.Empty, "Please select position from list.");
                return View(model);
            }

            if (ModelState.IsValid & CheckUniqueValues(model.Entity))
            {
                Staff staff = await db.Staff.FindAsync(model.Entity.StaffId);
                if (staff != null)
                {
                    staff.FullName = model.Entity.FullName;
                    staff.PositionId = position.PositionId;

                    db.Staff.Update(staff);
                    await db.SaveChangesAsync();

                    cache.Clean();

                    return RedirectToAction("Index", "Staff", new { page = model.PageViewModel.CurrentPage });
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
            Staff staff = await db.Staff.FindAsync(id);
            if (staff == null)
                return NotFound();

            bool deleteFlag = false;
            string message = "Do you want to delete this entity";

            if (db.Timetables.Any(s => s.StaffId == staff.StaffId))
                message = "This entity has entities, which dependents from this. Do you want to delete this entity and other, which dependents from this?";

            StaffViewModel model = new StaffViewModel();
            model.Entity = staff;
            model.PageViewModel = new PageViewModel { CurrentPage = page };
            model.DeleteViewModel = new DeleteViewModel { Message = message, IsDeleted = deleteFlag };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(StaffViewModel model)
        {
            Staff staff = await db.Staff.FindAsync(model.Entity.StaffId);
            if (staff == null)
                return NotFound();

            db.Staff.Remove(staff);
            await db.SaveChangesAsync();

            cache.Clean();

            model.DeleteViewModel = new DeleteViewModel { Message = "The entity was successfully deleted.", IsDeleted = true };

            return View(model);
        }

        private bool CheckUniqueValues(Staff staff)
        {
            bool firstFlag = true;

            Staff tempStaff = db.Staff.FirstOrDefault(s => s.FullName == staff.FullName);
            if (tempStaff != null)
            {
                if (tempStaff.StaffId != staff.StaffId)
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

        private IQueryable<Staff> GetSortedEntities(SortState sortState, string fullName, string positionName)
        {
            IQueryable<Staff> Staff = db.Staff.Include(s => s.Position).AsQueryable();
            switch (sortState)
            {
                case SortState.StaffFullNameAsc:
                    Staff = Staff.OrderBy(s => s.FullName);
                    break;
                case SortState.StaffFullNameDesc:
                    Staff = Staff.OrderByDescending(s => s.FullName);
                    break;
                case SortState.PositionsNameAsc:
                    Staff = Staff.OrderBy(s => s.Position.Name);
                    break;
                case SortState.PositionsNameDesc:
                    Staff = Staff.OrderByDescending(s => s.Position.Name);
                    break;
            }

            if (!string.IsNullOrEmpty(fullName))
                Staff = Staff.Where(s => s.FullName.Contains(fullName)).AsQueryable();
            if (!string.IsNullOrEmpty(positionName))
                Staff = Staff.Where(s => s.Position.Name.Contains(positionName)).AsQueryable();

            return Staff;
        }
    }
}
