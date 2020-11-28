using Microsoft.EntityFrameworkCore.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;
using WebApplication.Models;

namespace WebApplication.Data
{
    public static class DbInitializer
    {
        private static char[] letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz".ToCharArray();
        private static Random random = new Random();

        public static void Initialize(TvChannelContext db)
        {
            db.Database.EnsureCreated();

            int rowCount;
            int rowIndex;

            int minStringLength;
            int maxStringLength;

            if (!db.Genres.Any())
            {
                string genreName;
                string genreDescription;

                rowCount = 500;
                rowIndex = 0;
                while (rowIndex < rowCount)
                {
                    minStringLength = 8;

                    maxStringLength = 16;
                    genreName = GetString(minStringLength, maxStringLength);

                    maxStringLength = 32;
                    genreDescription = GetString(minStringLength, maxStringLength);

                    db.Genres.Add(new Genre { GenreName = genreName, GenreDescription = genreDescription });

                    rowIndex++;
                }

                db.SaveChanges();
            }


            if (!db.Positions.Any())
            {
                string name;

                rowCount = 500;
                rowIndex = 0;
                while (rowIndex < rowCount)
                {
                    minStringLength = 4;
                    maxStringLength = 16;
                    name = GetString(minStringLength, maxStringLength);

                    db.Positions.Add(new Position { Name = name });

                    rowIndex++;
                }

                db.SaveChanges();
            }

            if (!db.Staff.Any())
            {
                string fullName;
                int positionId;

                rowCount = 20000;
                rowIndex = 0;
                while (rowIndex < rowCount)
                {
                    fullName = GetString(8, 64);
                    positionId = random.Next(1, 501);

                    db.Staff.Add(new Staff
                    {
                        FullName = fullName,
                        PositionId = positionId
                    });

                    rowIndex++;
                }

                db.SaveChanges();
            }

            if (!db.Shows.Any())
            {
                string showName;
                DateTime releaseDate;
                TimeSpan duration;
                int mark;
                int markMonth;
                int markYear;
                int genreId;
                string description;

                rowCount = 20000;
                rowIndex = 0;
                while (rowIndex < rowCount)
                {
                    minStringLength = 8;

                    maxStringLength = 32;
                    showName = GetString(minStringLength, maxStringLength);

                    duration = GetTimeSpan();
                    mark = random.Next(1, 11);
                    markMonth = random.Next(1, 13);

                    do
                    {
                        releaseDate = GetDateTime();
                        markYear = random.Next(2000, 2022);
                    }
                    while (releaseDate.Year < markYear);

                    genreId = random.Next(1, 501);

                    maxStringLength = 32;
                    description = GetString(minStringLength, maxStringLength);

                    db.Shows.Add(new Show
                    {
                        Name = showName,
                        ReleaseDate = releaseDate,
                        Duration = duration,
                        Mark = mark,
                        MarkMonth = markMonth,
                        MarkYear = markYear,
                        GenreId = genreId,
                        Description = description
                    });

                    rowIndex++;
                }

                db.SaveChanges();
            }

            if (!db.Timetables.Any())
            {
                int dayOfWeek;
                int month;
                int year;
                int showId;
                TimeSpan startTime;
                TimeSpan endTime;
                int staffId;

                rowCount = 20000;
                rowIndex = 0;
                while (rowIndex < rowCount)
                {
                    dayOfWeek = random.Next(1, 8);
                    month = random.Next(1, 13);
                    year = random.Next(2010, 2022);

                    showId = random.Next(1, 20001);

                    startTime = GetTimeSpan();
                    endTime = startTime + db.Shows.FirstOrDefault(s => s.ShowId == showId).Duration;

                    staffId = random.Next(1, 20001);

                    db.Timetables.Add(new Timetable
                    {
                        DayOfWeek = dayOfWeek,
                        Month = month,
                        Year = year,
                        ShowId = showId,
                        StartTime = startTime,
                        EndTime = endTime,
                        StaffId = staffId
                    });

                    rowIndex++;
                }

                db.SaveChanges();
            }

            if (!db.Appeals.Any())
            {
                string fullName;
                string organization;
                int showId;
                string goalRequest;

                rowCount = 20000;
                rowIndex = 0;
                while (rowIndex < rowCount)
                {
                    fullName = GetString(8, 64);
                    organization = GetString(8, 64);
                    showId = random.Next(1, 20001);
                    goalRequest = GetString(8, 128);

                    db.Appeals.Add(new Appeal
                    {
                        FullName = fullName,
                        Organization = organization,
                        ShowId = showId,
                        GoalRequest = goalRequest
                    });

                    rowIndex++;
                }

                db.SaveChanges();
            }
        }

        private static string GetString(int minStringLength, int maxStringLength)
        {
            string result = "";
            
            int stringLimit = minStringLength + random.Next(maxStringLength - minStringLength);
           
            int stringPosition;
            for (int i = 0; i < stringLimit; i++)
            {
                stringPosition = random.Next(letters.Length);

                result += letters[stringPosition];
            }

            return result;
        }

        private static DateTime GetDateTime()
        {
            DateTime start = new DateTime(1995, 1, 1);
            int range = (DateTime.Today - start).Days;

            return start.AddDays(random.Next(range));
        }
        
        private static TimeSpan GetTimeSpan()
        {
            TimeSpan start = TimeSpan.FromHours(0);
            TimeSpan end = TimeSpan.FromHours(3);

            int maxMinutes = (int)((end - start).TotalMinutes);
            int minutes = random.Next(maxMinutes);

            return start.Add(TimeSpan.FromMinutes(minutes));
        }
    }
}
