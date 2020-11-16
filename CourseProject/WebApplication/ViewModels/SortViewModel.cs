using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication.ViewModels
{
    public enum SortState
    {
        No, 
        //Genres
        GenreNameAsc,
        GenreNameDesc,
        GenreDescriptionAsc,
        GenreDescriptionDesc,
        //Shows
        ShowNameAsc,
        ShowNameDesc,
        ShowDescriptionAsc,
        ShowDescriptionDesc,
        // Timetables
        TimetableDayOfWeekAsc,
        TimetableDayOfWeekDesc,
        TimetableMonthAsc,
        TimetableMonthDesc,
        TimetableYearAsc,
        TimetableYearDesc,
        TimetableStartTimeAsc,
        TimetableStartTimeDesc,
        TimetablEndTimeAsc,
        TimetablEndTimeDesc
    }

    public class SortViewModel
    {
        //Genres
        public SortState GenreNameSort { get; set; }
        public SortState GenreDescriptionSort { get; set; }

        //Shows
        public SortState ShowNameSort { get; set; }
        public SortState ShowDescriptionSort { get; set; }

        //Timtetables
        public SortState TimetableDayOfWeekSort { get; set; }
        public SortState TimetableMonthSort { get; set; }
        public SortState TimetableYearSort { get; set; }
        public SortState TimetableStartTimeSort { get; set; }
        public SortState TimetableEndTimeSort { get; set; }


        public SortState CurrentState { get; set; }
        public SortViewModel(SortState state)
        {
            //Genres
            GenreNameSort = state == SortState.GenreNameAsc ? SortState.GenreNameDesc : SortState.GenreNameAsc;
            GenreDescriptionSort = state == SortState.GenreDescriptionAsc ? SortState.GenreDescriptionDesc : SortState.GenreDescriptionAsc;
            //Shows
            ShowNameSort = state == SortState.ShowNameAsc ? SortState.ShowNameDesc : SortState.ShowNameAsc;
            ShowDescriptionSort = state == SortState.ShowDescriptionAsc ? SortState.ShowDescriptionDesc : SortState.ShowDescriptionAsc;

            //Timetables
            TimetableDayOfWeekSort = state == SortState.TimetableDayOfWeekAsc ? SortState.TimetableDayOfWeekDesc : SortState.TimetableDayOfWeekAsc;
            TimetableMonthSort = state == SortState.TimetableMonthAsc ? SortState.TimetableMonthDesc : SortState.TimetableMonthAsc;
            TimetableYearSort = state == SortState.TimetableYearAsc ? SortState.TimetableYearDesc : SortState.TimetableYearAsc;
            TimetableStartTimeSort = state == SortState.TimetableStartTimeAsc ? SortState.TimetableStartTimeDesc : SortState.TimetableStartTimeAsc;
            TimetableEndTimeSort = state == SortState.TimetablEndTimeAsc ? SortState.TimetablEndTimeDesc : SortState.TimetablEndTimeAsc;

            CurrentState = state;
        }
    }
}
