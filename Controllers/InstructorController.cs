using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using ContosoUniversity.DAL;
using ContosoUniversity.Models;
using ContosoUniversity.ViewModels;
using System.Data.Entity.Infrastructure;
using ContosoUniversity.Services;

namespace ContosoUniversity.Controllers
{
    public class InstructorController : Controller
    {
        private SchoolContext db = new SchoolContext();
        private ServiceInstructors serv = new ServiceInstructors();

        // GET: Instructor
        public ActionResult Index(int? id, int? courseID)
        {
            var viewModel = new InstructorIndexData();

            viewModel.Instructors = db.Instructors
                .Include(i => i.OfficeAssignment)
                .Include(i => i.Courses.Select(c => c.Department))
                .OrderBy(i => i.LastName);

            if (id != null)
            {
                ViewBag.InstructorID = id.Value;
                viewModel.Courses = viewModel.Instructors.Where(
                    i => i.ID == id.Value).Single().Courses;
            }

            if (courseID != null)
            {
                ViewBag.CourseID = courseID.Value;
                // Lazy loading
                //viewModel.Enrollments = viewModel.Courses.Where(
                //    x => x.CourseID == courseID).Single().Enrollments;
                // Explicit loading
                var selectedCourse = viewModel.Courses.Where(x => x.CourseID == courseID).Single();
                db.Entry(selectedCourse).Collection(x => x.Enrollments).Load();
                foreach (Enrollment enrollment in selectedCourse.Enrollments)
                {
                    db.Entry(enrollment).Reference(x => x.Student).Load();
                }

                viewModel.Enrollments = selectedCourse.Enrollments;
            }

            return View(viewModel);
        }


        // GET: Instructor/Details/5
        public ActionResult Details(int? id)
        {
            return serv.Service_Details(id);
        }

        public ActionResult Create()
        {
            return serv.Service_Create();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "LastName,FirstMidName,HireDate,OfficeAssignment")]Instructor instructor, string[] selectedCourses)
        {
            return serv.Service_Create(instructor,selectedCourses);
        }


        // GET: Instructor/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Instructor instructor = db.Instructors
                .Include(i => i.OfficeAssignment)
                .Include(i => i.Courses)
                .Where(i => i.ID == id)
                .Single();
            serv.PopulateAssignedCourseData(instructor);
            if (instructor == null)
            {
                return HttpNotFound();
            }
            return View(instructor);
        }

       
        // POST: Instructor/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int? id, string[] selectedCourses)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var instructorToUpdate = db.Instructors
               .Include(i => i.OfficeAssignment)
               .Include(i => i.Courses)
               .Where(i => i.ID == id)
               .Single();

            if (TryUpdateModel(instructorToUpdate, "",
               new string[] { "LastName", "FirstMidName", "HireDate", "OfficeAssignment" }))
            {
                try
                {
                    if (String.IsNullOrWhiteSpace(instructorToUpdate.OfficeAssignment.Location))
                    {
                        instructorToUpdate.OfficeAssignment = null;
                    }

                    UpdateInstructorCourses(selectedCourses, instructorToUpdate);

                    db.SaveChanges();

                    return RedirectToAction("Index");
                }
                catch (RetryLimitExceededException /* dex */)
                {
                    //Log the error (uncomment dex variable name and add a line here to write a log.
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
                }
            }
           serv.PopulateAssignedCourseData(instructorToUpdate);
            return View(instructorToUpdate);
        }
       
        private void UpdateInstructorCourses(string[] selectedCourses, Instructor instructorToUpdate)
        {
            serv.Service_UpdateInstructorCourses(selectedCourses,instructorToUpdate);
        }



        // GET: Instructor/Delete/5
        public ActionResult Delete(int? id)
        {
            return serv.Service_Delete(id);
        }

        // POST: Instructor/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Instructor instructor = db.Instructors
              .Include(i => i.OfficeAssignment)
              .Where(i => i.ID == id)
              .Single();

            instructor.OfficeAssignment = null;
            db.Instructors.Remove(instructor);

            var department = db.Departments
                .Where(d => d.InstructorID == id)
                .SingleOrDefault();
            if (department != null)
            {
                department.InstructorID = null;
            }

            db.SaveChanges();
            return RedirectToAction("Index");
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
